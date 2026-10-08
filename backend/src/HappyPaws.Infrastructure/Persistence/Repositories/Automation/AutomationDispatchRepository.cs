using HappyPaws.Application.Automation;
using HappyPaws.Domain.Automation;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Automation;

/// <summary>
/// The "already sent" records.
/// <para>
/// The claim is one <c>INSERT ... ON CONFLICT DO NOTHING</c> against the unique reminder key, on a
/// context of its own so it commits by itself: the row count says whether this worker won. Two
/// instances that reach the same reminder in the same second both run the insert; PostgreSQL
/// serialises them on the unique index and exactly one sees a row inserted.
/// </para>
/// </summary>
internal sealed class AutomationDispatchRepository(HappyPawsDbContext db, DbContextOptions<HappyPawsDbContext> options)
    : IAutomationDispatchRepository
{
    public async Task<bool> TryClaimAsync(AutomationDispatch claim, CancellationToken cancellationToken = default)
    {
        await using var own = new HappyPawsDbContext(options);

        var inserted = await own.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO automation_dispatches
                (id, rule_id, subject_kind, subject_id, due_key, customer_id, pet_id, branch_id,
                 scheduled_for, claimed_at, claimed_by, status, created_at)
            VALUES
                ({claim.Id}, {claim.RuleId}, {claim.SubjectKind.ToString()}, {claim.SubjectId}, {claim.DueKey},
                 {claim.CustomerId}, {claim.PetId}, {claim.BranchId}, {claim.ScheduledFor.ToUniversalTime()},
                 {claim.ClaimedAt.ToUniversalTime()}, {claim.ClaimedBy}, {claim.Status.ToString()}, {claim.ClaimedAt.ToUniversalTime()})
            ON CONFLICT (rule_id, subject_kind, subject_id, due_key) DO NOTHING
            """,
            cancellationToken);

        return inserted == 1;
    }

    public async Task CompleteAsync(
        string dispatchId,
        DispatchStatus status,
        string? errorCode,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        await using var own = new HappyPawsDbContext(options);

        await own.Set<AutomationDispatch>()
            .Where(item => item.Id == dispatchId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Status, status)
                    .SetProperty(item => item.ErrorCode, errorCode)
                    .SetProperty(item => item.CompletedAt, completedAt)
                    .SetProperty(item => item.UpdatedAt, completedAt),
                cancellationToken);
    }

    public async Task<IReadOnlySet<string>> ClaimedKeysAsync(
        string ruleId,
        IReadOnlyCollection<string> subjectIds,
        CancellationToken cancellationToken = default)
    {
        var ids = subjectIds.ToList();

        var pairs = await db.Set<AutomationDispatch>().AsNoTracking()
            .Where(item => item.RuleId == ruleId && ids.Contains(item.SubjectId))
            .Select(item => new { item.SubjectId, item.DueKey })
            .ToListAsync(cancellationToken);

        return pairs.Select(pair => DispatchKey.Of(pair.SubjectId, pair.DueKey)).ToHashSet(StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<AutomationDispatch>> StaleClaimsAsync(
        DateTimeOffset claimedBefore,
        int limit,
        CancellationToken cancellationToken = default) =>
        await db.Set<AutomationDispatch>().AsNoTracking()
            .Where(item => item.Status == DispatchStatus.Claimed && item.ClaimedAt < claimedBefore)
            .OrderBy(item => item.ClaimedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, AutomationStats>> StatsAsync(
        IReadOnlyCollection<string> ruleIds,
        DateTimeOffset recentSince,
        CancellationToken cancellationToken = default)
    {
        var ids = ruleIds.ToList();

        var rows = await db.Set<AutomationDispatch>().AsNoTracking()
            .Where(item => ids.Contains(item.RuleId))
            .GroupBy(item => item.RuleId)
            .Select(group => new
            {
                RuleId = group.Key,
                Total = group.Count(),
                Sent = group.Count(item => item.Status == DispatchStatus.Sent),
                Skipped = group.Count(item => item.Status == DispatchStatus.Skipped),
                Failed = group.Count(item => item.Status == DispatchStatus.Failed),
                Lost = group.Count(item => item.Status == DispatchStatus.Lost),
                Recent = group.Count(item => item.ClaimedAt >= recentSince),
                Last = group.Max(item => (DateTimeOffset?)item.ClaimedAt),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.RuleId,
            row => new AutomationStats(row.Total, row.Sent, row.Skipped, row.Failed, row.Lost, row.Recent, row.Last),
            StringComparer.Ordinal);
    }
}
