using HappyPaws.Application.Common;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.Application.Reporting.Dtos;

/// <summary>An export job as <c>POST</c> and <c>GET /staff/reports/exports</c> return it.</summary>
/// <param name="Id"><c>rex_...</c>, the job id.</param>
/// <param name="Report">Which report.</param>
/// <param name="Format"><c>csv</c>.</param>
/// <param name="Status"><c>queued</c>, <c>running</c>, <c>succeeded</c> or <c>failed</c>.</param>
/// <param name="From">First clinic day covered.</param>
/// <param name="To">Last clinic day covered.</param>
/// <param name="BranchId">The branch asked for, when one was.</param>
/// <param name="BranchIds">The branches covered; absent when it is every branch.</param>
/// <param name="CreatedAt">When it was asked for.</param>
/// <param name="StartedAt">When a worker took it.</param>
/// <param name="CompletedAt">When it finished.</param>
/// <param name="FileName">The file's name, once built.</param>
/// <param name="SizeBytes">Its size.</param>
/// <param name="RowCount">Data rows, header excluded.</param>
/// <param name="DownloadUrl">"ลิงก์ดาวน์โหลดอายุ 1 ชั่วโมง": signed afresh on every read of a finished job.</param>
/// <param name="DownloadExpiresAt">When that link stops working.</param>
/// <param name="ErrorCode">Why it failed.</param>
public sealed record ReportExportDto(
    string Id,
    string Report,
    string Format,
    string Status,
    DateOnly From,
    DateOnly To,
    string? BranchId,
    IReadOnlyList<string>? BranchIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? FileName,
    long? SizeBytes,
    int? RowCount,
    string? DownloadUrl,
    DateTimeOffset? DownloadExpiresAt,
    string? ErrorCode)
{
    public static ReportExportDto Of(ReportExport export, string? downloadUrl = null, DateTimeOffset? downloadExpiresAt = null) => new(
        export.Id,
        WireEnum.Of(export.Report),
        WireEnum.Of(export.Format),
        WireEnum.Of(export.Status),
        export.FromDate,
        export.ToDate,
        export.BranchId,
        export.AllBranches ? null : export.BranchIds,
        export.CreatedAt,
        export.StartedAt,
        export.CompletedAt,
        export.FileName,
        export.SizeBytes,
        export.RowCount,
        downloadUrl,
        downloadExpiresAt,
        export.ErrorCode);
}
