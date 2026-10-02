using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Scheduling;

/// <summary>
/// One concrete working block for one staff member at one branch.
/// <para>
/// The contract makes the roster the source of bookable time: "ตารางเวรเป็นตัวกำหนด slot ที่
/// /booking/availability คำนวณ". Availability therefore starts from shifts and subtracts, rather
/// than starting from opening hours — a branch being open means nothing if no vet is in.
/// </para>
/// <para>
/// This is the <b>expanded</b> form. <c>POST /staff/schedules/shifts</c> accepts an RRULE for a
/// whole week; expanding that recurrence, and applying time-off, belongs to the Scheduling module.
/// Availability reads only concrete blocks, so it never has to understand recurrence rules.
/// </para>
/// </summary>
public sealed class StaffShift : Entity
{
    private StaffShift()
    {
    }

    public string StaffId { get; private set; } = string.Empty;

    public string BranchId { get; private set; } = string.Empty;

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Set when time off is approved over this block; the shift stays for the audit trail.</summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    public bool IsActive => CancelledAt is null;

    public static StaffShift Create(string staffId, string branchId, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainException("invalid_shift", "เวลาสิ้นสุดเวรต้องอยู่หลังเวลาเริ่ม");
        }

        return new StaffShift
        {
            Id = EntityId.New(EntityId.Shift),
            StaffId = staffId,
            BranchId = branchId,
            StartsAt = startsAt,
            EndsAt = endsAt,
        };
    }

    /// <summary>True when the whole window fits inside this shift.</summary>
    public bool Covers(DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        IsActive && startsAt >= StartsAt && endsAt <= EndsAt;

    /// <summary>True when this active shift shares any time with the window. Touching ends do not count.</summary>
    public bool Overlaps(DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        IsActive && StartsAt < endsAt && startsAt < EndsAt;

    public void Cancel(DateTimeOffset at) => CancelledAt ??= at;

    /// <summary>
    /// Takes a window out of this shift: time off over part of a day. This shift is cancelled, not
    /// shortened, so the roster as it was planned stays on record, and the parts still worked come
    /// back as new shifts (none, one either side, or both).
    /// <para>
    /// Availability reads only active shifts, so leave reaches booking without booking knowing
    /// leave exists.
    /// </para>
    /// </summary>
    /// <param name="startsAt">Start of the time removed.</param>
    /// <param name="endsAt">End of the time removed.</param>
    /// <param name="at">When the change is made.</param>
    /// <returns>The remaining pieces, to be added; empty when the window covers the whole shift.</returns>
    public IReadOnlyList<StaffShift> CarveOut(DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset at)
    {
        if (!Overlaps(startsAt, endsAt))
        {
            return [];
        }

        Cancel(at);

        var pieces = new List<StaffShift>();

        if (StartsAt < startsAt)
        {
            pieces.Add(Create(StaffId, BranchId, StartsAt, startsAt));
        }

        if (endsAt < EndsAt)
        {
            pieces.Add(Create(StaffId, BranchId, endsAt, EndsAt));
        }

        return pieces;
    }
}
