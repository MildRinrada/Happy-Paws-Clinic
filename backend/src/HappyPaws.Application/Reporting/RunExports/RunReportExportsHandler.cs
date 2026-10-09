using System.Globalization;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Reporting.Files;
using HappyPaws.Application.Reporting.Tables;
using HappyPaws.Domain.Reporting;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Reporting.RunExports;

/// <summary>
/// Builds queued export files — what the scheduler's export job calls.
/// <para>
/// <b>Claiming.</b> Each job is taken in its own short transaction that locks the oldest queued row
/// with <c>FOR UPDATE SKIP LOCKED</c> and marks it running before committing, so concurrent
/// workers on several API instances each get a different job, or none. The file is then built
/// with no lock held, stored, and the outcome written in a second short transaction.
/// </para>
/// <para>
/// <b>Recovery.</b> A job still running after <see cref="StaleAfter"/> belonged to a worker that
/// stopped; it goes back to the queue, and fails for good after
/// <see cref="ReportExport.MaxAttempts"/> tries so a report that crashes its worker cannot loop.
/// </para>
/// <para>
/// <b>Formats.</b> The same table goes to the writer of the format the job stored
/// (<see cref="IReportFileWriter"/>): CSV, XLSX or PDF. Only the bytes, the extension and the media
/// type differ; claiming, retries and storage are the same for all three.
/// </para>
/// </summary>
public sealed class RunReportExportsHandler(
    IReportExportRepository exports,
    ReportTableBuilder tables,
    IEnumerable<IReportFileWriter> writers,
    IClinicSettingsStore settings,
    IBranchRepository branches,
    IFileStorage storage,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RunReportExportsHandler> logger)
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

    /// <summary>The most files one tick builds, so one instance does not hog a burst of requests.</summary>
    public const int MaxPerRun = 5;

    private const int RecoverBatch = 20;

    /// <summary>Recovers stale jobs, then builds up to <see cref="MaxPerRun"/> queued ones. Returns how many it finished.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        await RecoverStaleAsync(cancellationToken);

        var finished = 0;

        while (finished < MaxPerRun && await ClaimAsync(cancellationToken) is { } export)
        {
            await BuildAsync(export, cancellationToken);
            finished++;
        }

        return finished;
    }

    /// <summary>Takes the oldest queued job, or null. Public so tests can race two workers for one job.</summary>
    public async Task<ReportExport?> ClaimAsync(CancellationToken cancellationToken = default)
    {
        ReportExport? claimed = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            claimed = await exports.ClaimNextQueuedAsync(token);
            claimed?.Start(clock.UtcNow);
        }, cancellationToken);

        return claimed;
    }

    private async Task BuildAsync(ReportExport export, CancellationToken cancellationToken)
    {
        string? fileKey = null;
        string? fileName = null;
        long size = 0;
        var rows = 0;
        string? error = null;

        try
        {
            var writer = writers.FirstOrDefault(candidate => candidate.Format == export.Format)
                ?? throw new InvalidOperationException($"no writer for {export.Format}");

            var table = await tables.BuildAsync(export, cancellationToken);
            var bytes = writer.Write(table, await HeadingAsync(export, cancellationToken));

            fileKey = $"exports/{export.Id}.{writer.Extension}";
            fileName = FileNameOf(export, writer.Extension);
            rows = table.RowCount;

            await using var content = new MemoryStream(bytes, writable: false);
            size = await storage.SaveAsync(fileKey, writer.ContentType, content, fileName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Report export {ExportId} ({Report}) could not be built", export.Id, export.Report);
            error = "export_failed";
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var record = await exports.LoadForUpdateAsync(export.Id, token);

            if (record is not { Status: ReportExportStatus.Running })
            {
                return;
            }

            if (error is null)
            {
                record.Succeed(fileKey!, fileName!, size, rows, clock.UtcNow);
            }
            else
            {
                record.Fail(error, clock.UtcNow);
            }
        }, cancellationToken);
    }

    private async Task RecoverStaleAsync(CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            foreach (var stale in await exports.ClaimStaleRunningAsync(clock.UtcNow - StaleAfter, RecoverBatch, token))
            {
                logger.LogWarning("Report export {ExportId} was left running; returning it to the queue", stale.Id);
                stale.Abandon(clock.UtcNow);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// What the file says about itself. Branch names are read now, as the file is built; a branch
    /// closed since is named by its id rather than left out, because the rows still count it.
    /// </summary>
    private async Task<ReportFileHeading> HeadingAsync(ReportExport export, CancellationToken cancellationToken)
    {
        var profile = await settings.ProfileAsync(cancellationToken);
        var covered = export.BranchIds;
        var names = covered.Count == 0
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : (await branches.ListActiveAsync(cancellationToken)).ToDictionary(branch => branch.Id, branch => branch.Name, StringComparer.Ordinal);

        var label = export.AllBranches
            ? "ทุกสาขา"
            : string.Join(", ", covered.Select(id => names.GetValueOrDefault(id) ?? id));

        return new ReportFileHeading(
            ReportCatalogue.Of(export.Report).DisplayName,
            profile.Name,
            label,
            export.FromDate,
            export.ToDate,
            TimeZoneInfo.ConvertTime(clock.UtcNow, clock.ClinicTimeZone));
    }

    /// <summary><c>revenue_2026-09-01_2026-09-30.csv</c>, or <c>.xlsx</c> or <c>.pdf</c>.</summary>
    private static string FileNameOf(ReportExport export, string extension) => string.Create(
        CultureInfo.InvariantCulture,
        $"{ReportCatalogue.Of(export.Report).Title}_{export.FromDate:yyyy-MM-dd}_{export.ToDate:yyyy-MM-dd}.{extension}");
}
