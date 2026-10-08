using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Appointments;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Jobs;

/// <summary>
/// Clears rows whose life has ended: booking holds and idempotency keys.
/// <para>
/// Neither is load-bearing, which is why this is safe to run on any instance at any time. A hold
/// is dead the moment it expires (<c>SlotHold</c> checks the timestamp, never a sweeper), and an
/// idempotency key promises a replay for 24 hours (<c>expires_at</c>). Both are deleted a further
/// day after that, so a request in flight at the boundary still finds what it expects. Holds that
/// became appointments are kept: the appointment names its hold for tracing a disputed booking.
/// </para>
/// </summary>
public sealed class HousekeepingJob(HappyPawsDbContext db, IClock clock, ILogger<HousekeepingJob> logger) : IScheduledJob
{
    public static readonly TimeSpan Grace = TimeSpan.FromDays(1);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = clock.UtcNow - Grace;

        var holds = await db.Set<SlotHold>()
            .Where(hold => hold.ConsumedAt == null && hold.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var keys = await db.Set<IdempotencyRecord>()
            .Where(record => record.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (holds > 0 || keys > 0)
        {
            logger.LogInformation("Housekeeping removed {Holds} expired hold(s) and {Keys} expired idempotency key(s)", holds, keys);
        }
    }
}
