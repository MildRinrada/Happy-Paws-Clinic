using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments;

/// <summary>
/// Persistence port for the ten-minute booking holds behind <c>POST /booking/holds</c>.
/// </summary>
public interface ISlotHoldRepository
{
    Task<SlotHold?> FindByIdAsync(string holdId, CancellationToken cancellationToken = default);

    void Add(SlotHold hold);

    /// <summary>
    /// The live holds one holder has, tracked. Used to let go of a visitor's earlier guest hold
    /// when they pick another time (<c>docs/contract-extensions.md § 27</c>).
    /// </summary>
    Task<IReadOnlyList<SlotHold>> ActiveHeldByAsync(
        string holderId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Holds that still block time inside a window. <paramref name="now"/> is a parameter rather
    /// than read inside the repository so that expiry is evaluated against the same instant as the
    /// rest of the booking decision — a hold must not be live for the collision check and dead for
    /// the suggestion list.
    /// <para>
    /// <c>excludeHoldId</c> drops the caller's own hold, which must not block their own booking.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<SlotHold>> ActiveInWindowAsync(
        string branchId,
        IReadOnlyCollection<string> staffIds,
        DateTimeOffset from,
        DateTimeOffset to,
        DateTimeOffset now,
        string? excludeHoldId = null,
        CancellationToken cancellationToken = default);
}
