using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Reporting.Dtos;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.Application.Reporting.GetExportStatus;

/// <summary>
/// <c>GET /staff/reports/exports/{id}</c> — "สถานะและลิงก์ดาวน์โหลดอายุ 1 ชั่วโมง".
/// <para>
/// <b>Only the person who asked for an export can read it.</b> The file was cut to their branches
/// and their scopes at the time; another member of staff holding <c>report:export</c> may see less,
/// so anyone else is answered <c>404</c>, as for a record that is not theirs.
/// </para>
/// <para>
/// A finished job carries a download link signed afresh on each read, good for one hour. The file
/// itself stays in storage; asking again gives a new link.
/// </para>
/// </summary>
public sealed class GetExportStatusHandler(
    IReportExportRepository exports,
    IFileStorage storage,
    ICurrentUser currentUser,
    IClock clock)
{
    public static readonly TimeSpan DownloadLifetime = TimeSpan.FromHours(1);

    public async Task<ReportExportDto> HandleAsync(string exportId, CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        var export = await exports.FindAsync(exportId, cancellationToken);

        if (export is null || export.RequestedByStaffId != staffId)
        {
            throw new NotFoundException("report_export", exportId);
        }

        if (export is { Status: ReportExportStatus.Succeeded, FileKey: { } key })
        {
            return ReportExportDto.Of(export, storage.CreateDownloadUrl(key, DownloadLifetime), clock.UtcNow + DownloadLifetime);
        }

        return ReportExportDto.Of(export);
    }
}
