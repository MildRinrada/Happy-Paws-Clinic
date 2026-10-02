using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling.Dtos;

/// <summary>One working block.</summary>
public sealed record ShiftDto(string Id, string StaffId, string BranchId, DateTimeOffset StartsAt, DateTimeOffset EndsAt)
{
    public static ShiftDto From(StaffShift shift) =>
        new(shift.Id, shift.StaffId, shift.BranchId, shift.StartsAt, shift.EndsAt);
}

/// <summary>One stretch of leave.</summary>
public sealed record TimeOffDto(string Id, string StaffId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? Reason)
{
    public static TimeOffDto From(StaffTimeOff timeOff) =>
        new(timeOff.Id, timeOff.StaffId, timeOff.StartsAt, timeOff.EndsAt, timeOff.Reason);
}

/// <summary>A window of time, for free and booked intervals.</summary>
public sealed record TimeWindowDto(DateTimeOffset StartsAt, DateTimeOffset EndsAt);

/// <summary>One person's part of the calendar — <c>GET /staff/schedules</c>.</summary>
public sealed record StaffScheduleDto(
    string StaffId,
    string DisplayName,
    string Department,
    IReadOnlyList<ShiftDto> Shifts,
    IReadOnlyList<TimeOffDto> TimeOff);

/// <summary><c>GET /staff/schedules?from=&amp;to=</c>: everyone's roster for a span of clinic days.</summary>
/// <param name="From">First clinic day, inclusive.</param>
/// <param name="To">Last clinic day, inclusive.</param>
/// <param name="Staff">One entry per person with a shift or time off in the span.</param>
public sealed record ScheduleCalendarDto(DateOnly From, DateOnly To, IReadOnlyList<StaffScheduleDto> Staff);

/// <summary>An appointment in the way, with whoever could take it instead.</summary>
/// <param name="AppointmentId">The appointment inside the time off.</param>
/// <param name="BranchId">Where it is.</param>
/// <param name="ServiceId">What it is for.</param>
/// <param name="StartsAt">When it starts.</param>
/// <param name="EndsAt">When it ends.</param>
/// <param name="SuggestedStaffIds">Colleagues rostered, allowed to perform the service and free for the whole window, best first. Empty when nobody is.</param>
public sealed record TimeOffConflictDto(
    string AppointmentId,
    string BranchId,
    string ServiceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    IReadOnlyList<string> SuggestedStaffIds);

/// <summary>What recording time off did — "ระบบเตือนนัดที่ชนและเสนอหมอแทน".</summary>
/// <param name="TimeOff">The leave recorded.</param>
/// <param name="ShiftsCancelled">How many shifts it touched; each is kept on record as cancelled.</param>
/// <param name="ShiftsRemaining">The parts of those shifts still worked, now shifts of their own.</param>
/// <param name="Conflicts">Appointments already booked inside the leave. None are moved; a person decides.</param>
public sealed record TimeOffResultDto(
    TimeOffDto TimeOff,
    int ShiftsCancelled,
    IReadOnlyList<ShiftDto> ShiftsRemaining,
    IReadOnlyList<TimeOffConflictDto> Conflicts);

/// <summary>One person's real gaps on one day — <c>GET /staff/schedules/availability</c>.</summary>
/// <param name="StaffId">Who.</param>
/// <param name="DisplayName">Their name.</param>
/// <param name="Shifts">What they are rostered for that day at the branch.</param>
/// <param name="Booked">Appointments and live booking holds taking their time.</param>
/// <param name="Free">Shift time left after everything booked.</param>
/// <param name="FreeMinutes">Total of <paramref name="Free"/>.</param>
public sealed record StaffAvailabilityDto(
    string StaffId,
    string DisplayName,
    IReadOnlyList<TimeWindowDto> Shifts,
    IReadOnlyList<TimeWindowDto> Booked,
    IReadOnlyList<TimeWindowDto> Free,
    int FreeMinutes);

/// <summary><c>GET /staff/schedules/availability</c> for one branch and day.</summary>
/// <param name="BranchId">The branch.</param>
/// <param name="Date">The clinic day.</param>
/// <param name="Closed">True on a holiday, when nothing is bookable whatever the roster says.</param>
/// <param name="Holiday">The holiday's name when closed.</param>
/// <param name="Staff">One entry per person rostered that day.</param>
public sealed record ScheduleAvailabilityDto(
    string BranchId,
    DateOnly Date,
    bool Closed,
    string? Holiday,
    IReadOnlyList<StaffAvailabilityDto> Staff);

/// <summary>One assignment rule on the wire.</summary>
public sealed record AssignmentRuleDto(string? Species, string? ServiceId, IReadOnlyList<string> StaffIds)
{
    public static AssignmentRuleDto From(AssignmentRule rule) => new(rule.Species, rule.ServiceId, rule.StaffIds);
}

/// <summary>The whole rule set, as <c>PUT /staff/schedules/assignment-rules</c> stores it.</summary>
public sealed record AssignmentRulesDto(uint Version, IReadOnlyList<AssignmentRuleDto> Rules)
{
    public static AssignmentRulesDto From(AssignmentRules rules) =>
        new(rules.Version, [.. rules.Rules.Select(AssignmentRuleDto.From)]);
}
