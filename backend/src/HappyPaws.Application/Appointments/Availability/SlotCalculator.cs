using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Appointments.Availability;

/// <summary>One bookable opening on one vet's calendar.</summary>
public sealed record AvailableSlot(DateTimeOffset StartsAt, DateTimeOffset EndsAt, string StaffId);

/// <summary>
/// Everything the calculation needs, gathered by the caller so the calculation itself touches no
/// database and no clock.
/// </summary>
/// <param name="Shifts">Active rostered blocks in the window.</param>
/// <param name="Occupied">Appointments that still hold their time.</param>
/// <param name="Holds">Live ten-minute booking holds.</param>
/// <param name="SlotMinutes">The grid step from the clinic's booking settings.</param>
/// <param name="ServiceDurationMinutes">How long the requested service takes.</param>
/// <param name="EarliestStart">Now plus lead time — already resolved by the caller.</param>
/// <param name="LatestStart">End of the window being offered.</param>
/// <param name="EligibleStaffIds">Staff who may perform the service; empty means anyone on shift.</param>
/// <param name="StartAllowed">
/// An extra test on a slot's start, or null for none. This is how a service's online booking
/// windows ("จำกัด…ช่วงเวลา") narrow the feed and the suggestions alike, without the calculation
/// needing to know what a service or a time zone is.
/// </param>
public sealed record SlotQuery(
    IReadOnlyList<StaffShift> Shifts,
    IReadOnlyList<Appointment> Occupied,
    IReadOnlyList<SlotHold> Holds,
    int SlotMinutes,
    int ServiceDurationMinutes,
    DateTimeOffset EarliestStart,
    DateTimeOffset LatestStart,
    IReadOnlyCollection<string> EligibleStaffIds,
    Func<DateTimeOffset, bool>? StartAllowed = null);

/// <summary>
/// Turns a shift roster into bookable openings, and answers whether one specific window is free.
/// <para>
/// Deliberately pure: no repository, no <c>IClock</c>, no EF. Slot arithmetic is the part of
/// booking most likely to be subtly wrong — off-by-one at a shift boundary, a back-to-back
/// appointment counted as an overlap, a lead time applied to the wrong end — and it is far cheaper
/// to pin all of that down in unit tests than through HTTP.
/// </para>
/// <para>
/// The grid is anchored at each shift's start rather than at midnight, so a vet starting at 09:10
/// offers 09:10, 09:40, 10:10 rather than losing the first twenty minutes to a clock-aligned grid.
/// </para>
/// </summary>
public static class SlotCalculator
{
    public static IReadOnlyList<AvailableSlot> Enumerate(SlotQuery query)
    {
        var duration = TimeSpan.FromMinutes(query.ServiceDurationMinutes);
        var step = TimeSpan.FromMinutes(query.SlotMinutes);
        var slots = new List<AvailableSlot>();

        foreach (var shift in query.Shifts)
        {
            if (!shift.IsActive || !IsEligible(query.EligibleStaffIds, shift.StaffId))
            {
                continue;
            }

            for (var start = shift.StartsAt; start + duration <= shift.EndsAt; start += step)
            {
                if (start < query.EarliestStart || start > query.LatestStart)
                {
                    continue;
                }

                if (query.StartAllowed is { } allowed && !allowed(start))
                {
                    continue;
                }

                if (IsFree(query.Occupied, query.Holds, shift.StaffId, start, start + duration))
                {
                    slots.Add(new AvailableSlot(start, start + duration, shift.StaffId));
                }
            }
        }

        return [.. slots.OrderBy(slot => slot.StartsAt).ThenBy(slot => slot.StaffId, StringComparer.Ordinal)];
    }

    /// <summary>True when some active shift covers the whole window for the given vet.</summary>
    public static bool IsCoveredByShift(
        IReadOnlyList<StaffShift> shifts,
        string staffId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt) =>
        shifts.Any(shift => shift.StaffId == staffId && shift.Covers(startsAt, endsAt));

    /// <summary>
    /// True when nothing already occupies the vet's calendar over the window.
    /// <para>
    /// Overlap is half-open: an appointment ending at 10:30 does not collide with one starting at
    /// 10:30. Treating touching windows as a conflict would silently halve a clinic's capacity.
    /// </para>
    /// </summary>
    public static bool IsFree(
        IReadOnlyList<Appointment> occupied,
        IReadOnlyList<SlotHold> holds,
        string staffId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        foreach (var appointment in occupied)
        {
            if (appointment.StaffId == staffId
                && appointment.OccupiesSlot
                && Overlaps(appointment.StartsAt, appointment.EndsAt, startsAt, endsAt))
            {
                return false;
            }
        }

        foreach (var hold in holds)
        {
            if (hold.StaffId == staffId && Overlaps(hold.StartsAt, hold.EndsAt, startsAt, endsAt))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The <c>details.suggested</c> list the contract shows on a <c>slot_unavailable</c> refusal —
    /// the next few distinct openings, as <c>HH:mm</c> in clinic-local time.
    /// </summary>
    public static IReadOnlyList<string> SuggestTimes(
        IReadOnlyList<AvailableSlot> slots,
        TimeZoneInfo clinicTimeZone,
        int count = 3) =>
        [.. slots
            .Select(slot => TimeZoneInfo.ConvertTime(slot.StartsAt, clinicTimeZone).ToString("HH:mm"))
            .Distinct(StringComparer.Ordinal)
            .Take(count)];

    private static bool IsEligible(IReadOnlyCollection<string> eligibleStaffIds, string staffId) =>
        eligibleStaffIds.Count == 0 || eligibleStaffIds.Contains(staffId);

    private static bool Overlaps(
        DateTimeOffset firstStart,
        DateTimeOffset firstEnd,
        DateTimeOffset secondStart,
        DateTimeOffset secondEnd) =>
        firstStart < secondEnd && secondStart < firstEnd;
}
