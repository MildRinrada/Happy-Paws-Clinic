using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling;

/// <summary>A block about to be written: a new shift, a copy, or an existing one at new times.</summary>
public sealed record PlannedShift(string StaffId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

/// <summary>
/// The two refusals every roster write shares: a block on top of another block of the same person
/// (at any branch), and a block inside their recorded time off. Creating, changing and copying
/// shifts all answer the same <c>409</c> with the same list, so a client shows every clash one way.
/// <para>
/// Each entry names the person (<c>staff_id</c>): one request may plan for many people
/// (<c>docs/contract-extensions.md § 49</c>).
/// </para>
/// </summary>
public static class ShiftClashes
{
    /// <exception cref="ConflictException"><c>shift_overlap</c>, with <c>details.conflicts</c>.</exception>
    public static void EnsureNoOverlap(IReadOnlyCollection<PlannedShift> planned, IEnumerable<StaffShift> existing)
    {
        var clashes = existing
            .Where(shift => planned.Any(block => block.StaffId == shift.StaffId && shift.Overlaps(block.StartsAt, block.EndsAt)))
            .Select(shift => new Dictionary<string, object?>
            {
                ["shift_id"] = shift.Id,
                ["staff_id"] = shift.StaffId,
                ["branch_id"] = shift.BranchId,
                ["starts_at"] = shift.StartsAt,
                ["ends_at"] = shift.EndsAt,
            })
            .ToList();

        if (clashes.Count > 0)
        {
            throw new ConflictException(ErrorCodes.ShiftOverlap, "เวรใหม่ทับเวรที่มีอยู่แล้ว")
            {
                MessageEn = "The new shifts overlap shifts this person already has.",
                Details = new Dictionary<string, object> { ["conflicts"] = clashes },
            };
        }
    }

    /// <exception cref="ConflictException"><c>staff_on_time_off</c>, with <c>details.time_off</c>.</exception>
    public static void EnsureNotOnTimeOff(IReadOnlyCollection<PlannedShift> planned, IEnumerable<StaffTimeOff> timeOff)
    {
        var clashes = timeOff
            .Where(leave => planned.Any(block => block.StaffId == leave.StaffId && leave.Overlaps(block.StartsAt, block.EndsAt)))
            .Select(leave => new Dictionary<string, object?>
            {
                ["time_off_id"] = leave.Id,
                ["staff_id"] = leave.StaffId,
                ["starts_at"] = leave.StartsAt,
                ["ends_at"] = leave.EndsAt,
            })
            .ToList();

        if (clashes.Count > 0)
        {
            throw new ConflictException(ErrorCodes.StaffOnTimeOff, "เวรใหม่ตรงกับช่วงที่พนักงานลา")
            {
                MessageEn = "The new shifts fall in this person's time off.",
                Details = new Dictionary<string, object> { ["time_off"] = clashes },
            };
        }
    }
}
