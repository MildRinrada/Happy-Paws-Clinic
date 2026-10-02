using HappyPaws.Application.Appointments;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Application.Staff;

namespace HappyPaws.Application.Scheduling.GetStaffAvailability;

/// <summary><c>GET /staff/schedules/availability?date=&amp;branch=&amp;staff_id=</c>.</summary>
/// <param name="Date">The clinic day; today when omitted.</param>
/// <param name="BranchId">The branch; may be omitted in a one-branch clinic.</param>
/// <param name="StaffId">Only this person.</param>
public sealed record GetStaffAvailabilityQuery(DateOnly? Date = null, string? BranchId = null, string? StaffId = null);

/// <summary>
/// "ช่องว่างจริงหลังหักนัดและเวลาพัก": for each person rostered at a branch on a day, their shift
/// time with everything booked taken out.
/// <para>
/// The staff-side counterpart of the public <c>GET /booking/availability</c>, which offers slots
/// for one service. This answers a different question: who has free time, and when, regardless of
/// service. Breaks and time off are already gaps in the shifts themselves; what is taken out here
/// is appointments still holding their time and live ten-minute booking holds.
/// </para>
/// </summary>
public sealed class GetStaffAvailabilityHandler(
    IBookingCatalog catalog,
    IAppointmentRepository appointments,
    ISlotHoldRepository holds,
    IStaffRepository staff,
    BranchGuard branchGuard,
    ClinicDays days,
    IClock clock)
{
    public async Task<ScheduleAvailabilityDto> HandleAsync(
        GetStaffAvailabilityQuery query,
        CancellationToken cancellationToken = default)
    {
        var date = query.Date ?? days.Today;

        var branch = query.BranchId is { Length: > 0 } branchId
            ? await catalog.FindBranchAsync(branchId, cancellationToken) ?? throw new NotFoundException("branch", branchId)
            : await catalog.SoleActiveBranchAsync(cancellationToken)
              ?? throw new ValidationFailedException(
                  new Dictionary<string, string[]> { ["branch"] = ["คลินิกมีหลายสาขา ต้องระบุสาขา"] });

        branchGuard.EnsureCanAccess(branch.Id);

        if (await catalog.HolidayAsync(branch.Id, date, cancellationToken) is { } holiday)
        {
            return new ScheduleAvailabilityDto(branch.Id, date, Closed: true, holiday.Name, []);
        }

        var from = days.StartOf(date);
        var to = days.StartOf(date.AddDays(1));

        var shifts = await catalog.ShiftsAsync(branch.Id, from, to, query.StaffId, cancellationToken);
        var staffIds = shifts.Select(shift => shift.StaffId).Distinct(StringComparer.Ordinal).ToArray();

        var booked = await appointments.OccupyingSlotsAsync(branch.Id, staffIds, from, to, null, cancellationToken);
        var held = await holds.ActiveInWindowAsync(branch.Id, staffIds, from, to, clock.UtcNow, null, cancellationToken);
        var names = (await staff.FindRowsAsync(staffIds, cancellationToken))
            .ToDictionary(row => row.Membership.Id, row => row.Account.DisplayName, StringComparer.Ordinal);

        var people = staffIds.Select(staffId =>
        {
            // Clamped to the day asked about: a night shift's other half belongs to the other day.
            var working = shifts.Where(shift => shift.StaffId == staffId)
                .Select(shift => Clamp(shift.StartsAt, shift.EndsAt, from, to))
                .ToList();

            var taken = booked.Where(appointment => appointment.StaffId == staffId && appointment.OccupiesSlot)
                .Select(appointment => Clamp(appointment.StartsAt, appointment.EndsAt, from, to))
                .Concat(held.Where(hold => hold.StaffId == staffId).Select(hold => Clamp(hold.StartsAt, hold.EndsAt, from, to)))
                .Where(window => window.StartsAt < window.EndsAt)
                .OrderBy(window => window.StartsAt)
                .ToList();

            var free = Subtract(working, taken);

            return new StaffAvailabilityDto(
                staffId,
                names.GetValueOrDefault(staffId, staffId),
                working,
                taken,
                free,
                (int)free.Sum(window => (window.EndsAt - window.StartsAt).TotalMinutes));
        });

        return new ScheduleAvailabilityDto(
            branch.Id,
            date,
            Closed: false,
            Holiday: null,
            [.. people.OrderBy(person => person.DisplayName, StringComparer.Ordinal)]);
    }

    /// <summary>What is left of the working windows once the taken ones are removed.</summary>
    public static List<TimeWindowDto> Subtract(IEnumerable<TimeWindowDto> working, IReadOnlyList<TimeWindowDto> taken)
    {
        var free = new List<TimeWindowDto>();

        foreach (var window in working.OrderBy(window => window.StartsAt))
        {
            var cursor = window.StartsAt;

            foreach (var busy in taken.Where(busy => busy.StartsAt < window.EndsAt && busy.EndsAt > window.StartsAt))
            {
                if (busy.StartsAt > cursor)
                {
                    free.Add(new TimeWindowDto(cursor, busy.StartsAt));
                }

                if (busy.EndsAt > cursor)
                {
                    cursor = busy.EndsAt;
                }
            }

            if (cursor < window.EndsAt)
            {
                free.Add(new TimeWindowDto(cursor, window.EndsAt));
            }
        }

        return free;
    }

    private static TimeWindowDto Clamp(DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset from, DateTimeOffset to) =>
        new(startsAt < from ? from : startsAt, endsAt > to ? to : endsAt);
}
