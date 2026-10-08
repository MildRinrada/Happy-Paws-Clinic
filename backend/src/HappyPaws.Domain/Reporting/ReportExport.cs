using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Reporting;

/// <summary>
/// An export job — <c>POST /staff/reports/exports</c>, "งานส่งออกแบบ async · คืน job id".
/// <para>
/// <b>The scope of the data is fixed when it is asked for.</b> The days and the branches are
/// resolved against the requester's token at request time and stored here; the worker that builds
/// the file has no caller to ask, and must never widen a branch-bound manager's export to the whole
/// clinic because nobody was signed in when it ran.
/// </para>
/// <para>
/// Workers claim a queued job with <c>FOR UPDATE SKIP LOCKED</c>, so two API instances never build
/// the same file. A job left <see cref="ReportExportStatus.Running"/> by a worker that died is put
/// back in the queue, up to <see cref="MaxAttempts"/> tries.
/// </para>
/// </summary>
public sealed class ReportExport : Entity
{
    public const int MaxAttempts = 3;

    private readonly List<string> _branchIds = [];

    private ReportExport()
    {
    }

    public ReportKind Report { get; private set; }

    public ReportFormat Format { get; private set; }

    /// <summary>First clinic day, included.</summary>
    public DateOnly FromDate { get; private set; }

    /// <summary>Last clinic day, included.</summary>
    public DateOnly ToDate { get; private set; }

    /// <summary>The one branch asked for, when one was.</summary>
    public string? BranchId { get; private set; }

    /// <summary>True when the requester could see every branch and did not narrow it.</summary>
    public bool AllBranches { get; private set; }

    /// <summary>The branches the file covers, when <see cref="AllBranches"/> is false.</summary>
    public IReadOnlyList<string> BranchIds => _branchIds;

    public string RequestedByStaffId { get; private set; } = string.Empty;

    public ReportExportStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Where the file is in the storage. Never shown; a download URL is signed from it.</summary>
    public string? FileKey { get; private set; }

    public string? FileName { get; private set; }

    public long? SizeBytes { get; private set; }

    public int? RowCount { get; private set; }

    public string? ErrorCode { get; private set; }

    public static ReportExport Request(
        ReportKind report,
        ReportFormat format,
        DateOnly fromDate,
        DateOnly toDate,
        string? branchId,
        IReadOnlyCollection<string>? branchIds,
        string requestedByStaffId)
    {
        if (toDate < fromDate)
        {
            throw new DomainException("invalid_export", "วันสุดท้ายต้องไม่อยู่ก่อนวันแรก");
        }

        var export = new ReportExport
        {
            Id = EntityId.New(EntityId.ReportExport),
            Report = report,
            Format = format,
            FromDate = fromDate,
            ToDate = toDate,
            BranchId = branchId,
            AllBranches = branchIds is null,
            RequestedByStaffId = requestedByStaffId,
            Status = ReportExportStatus.Queued,
        };

        if (branchIds is not null)
        {
            export._branchIds.AddRange(branchIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        }

        return export;
    }

    /// <summary>A worker took it. Counts the attempt, so a job that kills its worker stops being retried.</summary>
    public void Start(DateTimeOffset now)
    {
        if (Status != ReportExportStatus.Queued)
        {
            throw new StateConflictException("report_export", Status.ToString(), nameof(Start));
        }

        Status = ReportExportStatus.Running;
        Attempts++;
        StartedAt = now;
    }

    public void Succeed(string fileKey, string fileName, long sizeBytes, int rowCount, DateTimeOffset now)
    {
        EnsureRunning(nameof(Succeed));

        Status = ReportExportStatus.Succeeded;
        FileKey = fileKey;
        FileName = fileName;
        SizeBytes = sizeBytes;
        RowCount = rowCount;
        CompletedAt = now;
        ErrorCode = null;
    }

    public void Fail(string errorCode, DateTimeOffset now)
    {
        EnsureRunning(nameof(Fail));

        Status = ReportExportStatus.Failed;
        ErrorCode = errorCode;
        CompletedAt = now;
    }

    /// <summary>
    /// A running job whose worker stopped answering: back to the queue for another worker, or failed
    /// once it has used its attempts.
    /// </summary>
    public void Abandon(DateTimeOffset now)
    {
        EnsureRunning(nameof(Abandon));

        if (Attempts >= MaxAttempts)
        {
            Status = ReportExportStatus.Failed;
            ErrorCode = "export_timed_out";
            CompletedAt = now;
            return;
        }

        Status = ReportExportStatus.Queued;
        StartedAt = null;
    }

    private void EnsureRunning(string action)
    {
        if (Status != ReportExportStatus.Running)
        {
            throw new StateConflictException("report_export", Status.ToString(), action);
        }
    }
}
