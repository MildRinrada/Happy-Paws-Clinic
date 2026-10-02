using HappyPaws.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Idempotency;

/// <summary>
/// PostgreSQL-backed idempotency store.
/// <para>
/// The claim is an <c>INSERT</c> that either succeeds or violates the primary key. That is the
/// whole concurrency design: two simultaneous retries of the same booking race to insert, one
/// wins and proceeds, the other reads the existing row and either replays the stored response or
/// backs off because the first is still running. No distributed lock, no read-then-write window.
/// </para>
/// </summary>
public sealed class EfIdempotencyStore(HappyPawsDbContext db, IClock clock) : IIdempotencyStore
{
    /// <summary>Retry window from the contract.</summary>
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    public async Task<IdempotentOutcome?> TryClaimAsync(
        IdempotencyKey key,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindAsync(key, cancellationToken);

        if (existing is not null)
        {
            return Describe(existing, requestHash);
        }

        var now = clock.UtcNow;

        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = key.Value,
            Endpoint = key.Endpoint,
            Subject = key.Subject,
            RequestHash = requestHash,
            CreatedAt = now,
            ExpiresAt = now.Add(Retention),
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException)
        {
            // Lost the insert race: another attempt claimed the key between our read and write.
            db.ChangeTracker.Clear();

            var winner = await FindAsync(key, cancellationToken);
            return winner is null ? null : Describe(winner, requestHash);
        }
    }

    public async Task CompleteAsync(
        IdempotencyKey key,
        int statusCode,
        string? responseBody,
        CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(key, cancellationToken);
        if (record is null)
        {
            return;
        }

        record.StatusCode = statusCode;
        record.ResponseBody = responseBody;
        record.CompletedAt = clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
    {
        // A faulted request must not leave a claim behind, or the client's legitimate retry would
        // be refused as "already in flight" until the record expires.
        await db.IdempotencyRecords
            .Where(record => record.Key == key.Value
                && record.Endpoint == key.Endpoint
                && record.Subject == key.Subject
                && record.CompletedAt == null)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Task<IdempotencyRecord?> FindAsync(IdempotencyKey key, CancellationToken cancellationToken) =>
        db.IdempotencyRecords.FirstOrDefaultAsync(
            record => record.Key == key.Value
                && record.Endpoint == key.Endpoint
                && record.Subject == key.Subject,
            cancellationToken);

    private static IdempotentOutcome Describe(IdempotencyRecord record, string requestHash) =>
        new(
            IsInFlight: record.IsInFlight,
            RequestHashMatches: string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal),
            StatusCode: record.StatusCode ?? 0,
            ResponseBody: record.ResponseBody);
}
