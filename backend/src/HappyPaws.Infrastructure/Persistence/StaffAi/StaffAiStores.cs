using System.Text.Json;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.StaffAi;
using HappyPaws.Application.StaffAi.Ask;
using HappyPaws.Application.StaffAi.DailyBrief;
using HappyPaws.Application.StaffAi.PetSummary;
using HappyPaws.Infrastructure.Persistence.Repositories.Analytics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace HappyPaws.Infrastructure.Persistence.StaffAi;

/// <summary>
/// The monthly AI counter. The reservation is one statement: the month's total row is incremented
/// only while it is under the limit (<c>ON CONFLICT DO UPDATE ... WHERE used &lt; limit</c>), and the
/// feature's row only if the total was. PostgreSQL locks the total row for the update, so a second
/// request racing for the last use waits, re-reads the row, finds it full and gets nothing back.
/// </summary>
internal sealed class StaffAiUsageStore(HappyPawsDbContext db, IClock clock) : IStaffAiUsageStore
{
    private const string ReserveSql =
        """
        WITH total AS (
            INSERT INTO staff_ai_usage (month, feature, used, updated_at)
            VALUES (@month, '*', 1, @now)
            ON CONFLICT (month, feature) DO UPDATE
                SET used = staff_ai_usage.used + 1, updated_at = @now
                WHERE staff_ai_usage.used < @limit
            RETURNING used
        )
        INSERT INTO staff_ai_usage (month, feature, used, updated_at)
        SELECT @month, @feature, 1, @now FROM total
        ON CONFLICT (month, feature) DO UPDATE
            SET used = staff_ai_usage.used + 1, updated_at = @now
        RETURNING used
        """;

    private const string ReleaseSql =
        """
        UPDATE staff_ai_usage
        SET used = GREATEST(used - 1, 0), updated_at = @now
        WHERE month = @month AND feature IN ('*', @feature) AND used > 0
        RETURNING used
        """;

    public async Task<bool> TryReserveAsync(string month, string feature, int limit, CancellationToken cancellationToken = default)
    {
        var rows = await AnalyticsSql.QueryAsync(
            db,
            ReserveSql,
            [
                new NpgsqlParameter("month", NpgsqlDbType.Varchar) { Value = month },
                new NpgsqlParameter("feature", NpgsqlDbType.Varchar) { Value = feature },
                new NpgsqlParameter("limit", NpgsqlDbType.Integer) { Value = limit },
                new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = clock.UtcNow.ToUniversalTime() },
            ],
            reader => reader.GetInt32(0),
            cancellationToken);

        return rows.Count > 0;
    }

    public async Task ReleaseAsync(string month, string feature, CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            ReleaseSql,
            [
                new NpgsqlParameter("month", NpgsqlDbType.Varchar) { Value = month },
                new NpgsqlParameter("feature", NpgsqlDbType.Varchar) { Value = feature },
                new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = clock.UtcNow.ToUniversalTime() },
            ],
            reader => reader.GetInt32(0),
            cancellationToken);

    public async Task<StaffAiUsageCounts> ReadAsync(string month, CancellationToken cancellationToken = default)
    {
        var rows = await db.Set<StaffAiUsageRow>().AsNoTracking()
            .Where(row => row.Month == month)
            .ToListAsync(cancellationToken);

        return new StaffAiUsageCounts(
            rows.FirstOrDefault(row => row.Feature == StaffAiUsageRow.Total)?.Used ?? 0,
            rows.Where(row => row.Feature != StaffAiUsageRow.Total).ToDictionary(row => row.Feature, row => row.Used));
    }
}

/// <summary>One summary per pet, replaced when the data changes.</summary>
internal sealed class PetSummaryCache(HappyPawsDbContext db) : IPetSummaryCache
{
    public async Task<CachedPetSummary?> FindAsync(string petId, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<PetSummaryRow>().AsNoTracking().FirstOrDefaultAsync(found => found.PetId == petId, cancellationToken);

        return row is null
            ? null
            : new CachedPetSummary(
                row.PetId,
                row.Fingerprint,
                row.Text,
                JsonSerializer.Deserialize<List<PetSummaryCitationDto>>(row.Citations, StaffToolJson.Options) ?? [],
                row.GeneratedAt);
    }

    public async Task SaveAsync(CachedPetSummary summary, CancellationToken cancellationToken = default)
    {
        var citations = JsonSerializer.Serialize(summary.Citations, StaffToolJson.Options);
        var row = await db.Set<PetSummaryRow>().FirstOrDefaultAsync(found => found.PetId == summary.PetId, cancellationToken);

        if (row is null)
        {
            db.Set<PetSummaryRow>().Add(new PetSummaryRow
            {
                PetId = summary.PetId,
                Fingerprint = summary.Fingerprint,
                Text = summary.Text,
                Citations = citations,
                GeneratedAt = summary.GeneratedAt,
            });
        }
        else
        {
            row.Fingerprint = summary.Fingerprint;
            row.Text = summary.Text;
            row.Citations = citations;
            row.GeneratedAt = summary.GeneratedAt;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two vets summarised the same pet at once and the other insert won. Either summary
            // is good; the next read compares fingerprints again.
            db.ChangeTracker.Clear();
        }
    }
}

/// <summary>The morning briefs. The claim is one statement on the (branch, day) key.</summary>
internal sealed class DailyBriefStore(HappyPawsDbContext db) : IDailyBriefStore
{
    private const string ClaimSql =
        """
        INSERT INTO staff_ai_daily_briefs (branch_id, brief_date, status, attempts, claimed_at, created_at)
        VALUES (@branch, @date, 'generating', 1, @now, @now)
        ON CONFLICT (branch_id, brief_date) DO UPDATE
            SET status = 'generating',
                attempts = staff_ai_daily_briefs.attempts + 1,
                claimed_at = @now,
                updated_at = @now
            WHERE (staff_ai_daily_briefs.status = 'failed'
                   AND staff_ai_daily_briefs.failure_reason IS DISTINCT FROM 'quota_exceeded'
                   AND staff_ai_daily_briefs.attempts < @max_attempts)
               OR (staff_ai_daily_briefs.status = 'generating'
                   AND staff_ai_daily_briefs.claimed_at < @stale_before)
        RETURNING attempts
        """;

    public async Task<bool> TryClaimAsync(
        string branchId,
        DateOnly date,
        DateTimeOffset now,
        DateTimeOffset staleBefore,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        var rows = await AnalyticsSql.QueryAsync(
            db,
            ClaimSql,
            [
                new NpgsqlParameter("branch", NpgsqlDbType.Varchar) { Value = branchId },
                new NpgsqlParameter("date", NpgsqlDbType.Date) { Value = date },
                new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now.ToUniversalTime() },
                new NpgsqlParameter("stale_before", NpgsqlDbType.TimestampTz) { Value = staleBefore.ToUniversalTime() },
                new NpgsqlParameter("max_attempts", NpgsqlDbType.Integer) { Value = maxAttempts },
            ],
            reader => reader.GetInt32(0),
            cancellationToken);

        return rows.Count > 0;
    }

    public async Task FinishAsync(
        string branchId,
        DateOnly date,
        string status,
        string? failureReason,
        DailyBriefNumbersDto? numbers,
        string? text,
        DateTimeOffset at,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<DailyBriefRow>()
            .FirstOrDefaultAsync(found => found.BranchId == branchId && found.BriefDate == date, cancellationToken);

        if (row is null)
        {
            return;
        }

        row.Status = status;
        row.FailureReason = failureReason;
        row.Numbers = numbers is null ? row.Numbers : JsonSerializer.Serialize(numbers, StaffToolJson.Options);
        row.Text = text;
        row.GeneratedAt = at;
        row.UpdatedAt = at;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredDailyBrief?> FindAsync(string branchId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<DailyBriefRow>().AsNoTracking()
            .FirstOrDefaultAsync(found => found.BranchId == branchId && found.BriefDate == date, cancellationToken);

        return row is null
            ? null
            : new StoredDailyBrief(
                row.BranchId,
                row.BriefDate,
                row.Status,
                row.FailureReason,
                row.Numbers is null ? null : JsonSerializer.Deserialize<DailyBriefNumbersDto>(row.Numbers, StaffToolJson.Options),
                row.Text,
                row.GeneratedAt);
    }
}
