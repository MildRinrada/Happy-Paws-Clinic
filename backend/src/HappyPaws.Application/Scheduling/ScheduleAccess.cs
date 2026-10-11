using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.Application.Scheduling;

/// <summary>
/// Whose roster a caller may touch. The same branch rule as the rest of the staff module: a
/// branch-bound scheduler plans the people who work at their branches, and anyone else answers
/// <c>404</c>, as if they did not exist.
/// </summary>
public sealed class ScheduleAccess(IStaffRepository staff, IScheduleRepository schedule, BranchGuard branchGuard)
{
    /// <summary>
    /// An active shift the caller may change or remove, tracked
    /// (<c>docs/contract-extensions.md § 47</c>). A shift at a branch the caller does not work at
    /// answers <c>404</c> like one that does not exist, and so does one already cancelled, by
    /// time off or by an earlier removal: there is nothing left of it to change.
    /// </summary>
    /// <exception cref="NotFoundException">No such shift, cancelled, or at none of the caller's branches.</exception>
    public async Task<StaffShift> ActiveShiftAsync(string shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await schedule.FindShiftForChangeAsync(shiftId, cancellationToken);

        if (shift is null || !shift.IsActive || !branchGuard.CanAccess(shift.BranchId))
        {
            throw new NotFoundException("shift", shiftId);
        }

        return shift;
    }

    /// <summary>An active member the caller may schedule.</summary>
    /// <exception cref="NotFoundException">No such member, deactivated, or at none of the caller's branches.</exception>
    public async Task<StaffMemberRow> ActiveMemberAsync(string staffId, CancellationToken cancellationToken = default)
    {
        var row = await staff.FindRowAsync(staffId, cancellationToken);

        if (row is null || !row.Membership.IsActive || !IsVisible(row))
        {
            throw new NotFoundException("staff_member", staffId);
        }

        return row;
    }

    /// <summary>True when the member works at one of the caller's branches, or either side is not branch-bound.</summary>
    public bool IsVisible(StaffMemberRow row) =>
        branchGuard.VisibleBranchIds is not { } visible
        || row.Membership.BranchIds.Count == 0
        || row.Membership.BranchIds.Any(visible.Contains);
}
