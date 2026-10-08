using HappyPaws.Domain.Reporting;

namespace HappyPaws.Application.Reporting;

/// <summary>The export jobs.</summary>
public interface IReportExportRepository
{
    void Add(ReportExport export);

    /// <summary>Read-only, for the status endpoint.</summary>
    Task<ReportExport?> FindAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Tracked, inside the caller's transaction, for recording an outcome.</summary>
    Task<ReportExport?> LoadForUpdateAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The oldest queued job, locked with <c>FOR UPDATE SKIP LOCKED</c> inside the caller's
    /// transaction, or null when there is none another worker has not already taken. Two workers
    /// asking at once get two different jobs, or one gets nothing; never the same one.
    /// </summary>
    Task<ReportExport?> ClaimNextQueuedAsync(CancellationToken cancellationToken = default);

    /// <summary>Running jobs started before <paramref name="startedBefore"/>, locked the same way.</summary>
    Task<IReadOnlyList<ReportExport>> ClaimStaleRunningAsync(
        DateTimeOffset startedBefore,
        int limit,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Wakes the export worker when a job is queued, so a file is not left waiting for the next
/// polling tick. A hint, never the guarantee: the worker polls anyway, and a nudge sent to an
/// instance whose scheduler is off changes nothing.
/// </summary>
public interface IReportExportSignal
{
    void ExportQueued();
}
