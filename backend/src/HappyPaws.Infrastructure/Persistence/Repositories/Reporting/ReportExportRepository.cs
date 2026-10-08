using HappyPaws.Application.Reporting;
using HappyPaws.Domain.Reporting;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Reporting;

/// <summary>
/// The export jobs, and the queue the workers take them from.
/// <para>
/// <c>FOR UPDATE SKIP LOCKED</c> is what lets several API instances share one queue with no
/// coordinator: a worker locks the oldest queued row it can, and a row another worker has
/// already locked is skipped rather than waited for. The SQL is not composed further by EF (no
/// <c>LIMIT</c> wrapped around it), because a locking clause inside a subquery is not the same
/// statement.
/// </para>
/// </summary>
internal sealed class ReportExportRepository(HappyPawsDbContext db) : IReportExportRepository
{
    public void Add(ReportExport export) => db.Set<ReportExport>().Add(export);

    public Task<ReportExport?> FindAsync(string id, CancellationToken cancellationToken = default) =>
        db.Set<ReportExport>().AsNoTracking().FirstOrDefaultAsync(export => export.Id == id, cancellationToken);

    public Task<ReportExport?> LoadForUpdateAsync(string id, CancellationToken cancellationToken = default) =>
        db.Set<ReportExport>().FirstOrDefaultAsync(export => export.Id == id, cancellationToken);

    public async Task<ReportExport?> ClaimNextQueuedAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Set<ReportExport>()
            .FromSqlRaw(
                """
                SELECT r.*, r.xmin FROM report_exports r
                WHERE status = 'Queued'
                ORDER BY created_at, id
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        return rows.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ReportExport>> ClaimStaleRunningAsync(
        DateTimeOffset startedBefore,
        int limit,
        CancellationToken cancellationToken = default) =>
        await db.Set<ReportExport>()
            .FromSqlInterpolated(
                $"""
                SELECT r.*, r.xmin FROM report_exports r
                WHERE status = 'Running' AND started_at < {startedBefore.ToUniversalTime()}
                ORDER BY started_at, id
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
}
