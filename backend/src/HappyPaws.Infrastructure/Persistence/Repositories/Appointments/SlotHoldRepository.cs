using HappyPaws.Application.Appointments;
using HappyPaws.Domain.Appointments;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Appointments;

/// <summary>
/// EF Core implementation of the booking-hold port.
/// <para>
/// Expiry is filtered in SQL against the instant the caller passed in, not against
/// <c>DateTimeOffset.UtcNow</c> read here. One booking decision must see one clock: a hold that
/// counts as live for the collision check and dead for the suggestion list would produce a refusal
/// that offers the slot it just refused.
/// </para>
/// </summary>
internal sealed class SlotHoldRepository(HappyPawsDbContext db) : ISlotHoldRepository
{
    public Task<SlotHold?> FindByIdAsync(string holdId, CancellationToken cancellationToken = default) =>
        db.Set<SlotHold>().FirstOrDefaultAsync(hold => hold.Id == holdId, cancellationToken);

    public void Add(SlotHold hold) => db.Set<SlotHold>().Add(hold);

    public async Task<IReadOnlyList<SlotHold>> ActiveInWindowAsync(
        string branchId,
        IReadOnlyCollection<string> staffIds,
        DateTimeOffset from,
        DateTimeOffset to,
        DateTimeOffset now,
        string? excludeHoldId = null,
        CancellationToken cancellationToken = default)
    {
        if (staffIds.Count == 0)
        {
            return [];
        }

        var query = db.Set<SlotHold>()
            .AsNoTracking()
            .Where(hold => hold.BranchId == branchId
                && staffIds.Contains(hold.StaffId)
                && hold.ConsumedAt == null
                && hold.ReleasedAt == null
                && hold.ExpiresAt > now
                && hold.StartsAt < to
                && hold.EndsAt > from);

        if (excludeHoldId is { Length: > 0 })
        {
            query = query.Where(hold => hold.Id != excludeHoldId);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
