using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Staff;

namespace HappyPaws.Application.Scheduling;

/// <summary>
/// Whose roster a caller may touch. The same branch rule as the rest of the staff module: a
/// branch-bound scheduler plans the people who work at their branches, and anyone else answers
/// <c>404</c>, as if they did not exist.
/// </summary>
public sealed class ScheduleAccess(IStaffRepository staff, BranchGuard branchGuard)
{
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
