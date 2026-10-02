using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Application.Staff;

namespace HappyPaws.Application.Scheduling.GetScheduleCalendar;

/// <summary><c>GET /staff/schedules?from=&amp;to=&amp;branch=&amp;staff_id=</c>.</summary>
/// <param name="From">First clinic day; today when omitted.</param>
/// <param name="To">Last clinic day, inclusive; six days after <paramref name="From"/> when omitted.</param>
/// <param name="BranchId">Only shifts at this branch.</param>
/// <param name="StaffId">Only this person.</param>
public sealed record GetScheduleCalendarQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    string? BranchId = null,
    string? StaffId = null);

/// <summary>
/// "ตารางเวรรวมทุกคนแบบปฏิทิน": everyone's shifts and time off for a span of days, grouped by
/// person, for drawing a calendar.
/// <para>
/// <c>from</c> and <c>to</c> are filters with defaults, not required (see
/// <c>docs/assumptions.md § 7</c>): with neither, the answer is the coming week. A span is at most
/// 31 days, which is a month view; a longer one is refused rather than slowly returned.
/// </para>
/// <para>
/// Branch-scoped like every staff read. A branch-bound caller sees shifts at their branches and
/// the leave of people who work there.
/// </para>
/// </summary>
public sealed class GetScheduleCalendarHandler(
    IScheduleRepository schedule,
    IStaffRepository staff,
    ScheduleAccess access,
    BranchGuard branchGuard,
    ClinicDays days)
{
    public const int LongestSpanDays = 31;

    public async Task<ScheduleCalendarDto> HandleAsync(
        GetScheduleCalendarQuery query,
        CancellationToken cancellationToken = default)
    {
        var first = query.From ?? days.Today;
        var last = query.To ?? first.AddDays(6);

        if (last < first)
        {
            throw Invalid("to", "วันสุดท้ายต้องไม่อยู่ก่อนวันแรก");
        }

        if (last.DayNumber - first.DayNumber + 1 > LongestSpanDays)
        {
            throw Invalid("to", $"ดูตารางได้ครั้งละไม่เกิน {LongestSpanDays} วัน");
        }

        if (query.BranchId is { Length: > 0 } branchId)
        {
            branchGuard.EnsureCanAccess(branchId);
        }

        var from = days.StartOf(first);
        var to = days.StartOf(last.AddDays(1));
        var branchFilter = query.BranchId is { Length: > 0 } one ? [one] : branchGuard.VisibleBranchIds;

        var shifts = await schedule.ActiveShiftsAsync(from, to, branchFilter, query.StaffId, cancellationToken);
        var timeOff = await schedule.TimeOffAsync(from, to, query.StaffId, cancellationToken);

        var staffIds = shifts.Select(shift => shift.StaffId)
            .Concat(timeOff.Select(leave => leave.StaffId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var members = (await staff.FindRowsAsync(staffIds, cancellationToken))
            .Where(access.IsVisible)
            .Where(row => query.BranchId is not { Length: > 0 } branch
                || row.Membership.BranchIds.Count == 0
                || row.Membership.BranchIds.Contains(branch)
                || shifts.Any(shift => shift.StaffId == row.Membership.Id))
            .ToList();

        return new ScheduleCalendarDto(
            first,
            last,
            [
                .. members
                    .OrderBy(row => row.Account.DisplayName, StringComparer.Ordinal)
                    .Select(row => new StaffScheduleDto(
                        row.Membership.Id,
                        row.Account.DisplayName,
                        row.Membership.Department,
                        [.. shifts.Where(shift => shift.StaffId == row.Membership.Id).Select(ShiftDto.From)],
                        [.. timeOff.Where(leave => leave.StaffId == row.Membership.Id).Select(TimeOffDto.From)])),
            ]);
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
