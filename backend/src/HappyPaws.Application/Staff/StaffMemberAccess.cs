using HappyPaws.Application.Access;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Identity;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff;

/// <summary>
/// Loads a staff member for a change and decides whether this caller may make it.
/// <para>
/// Two conditions beyond holding <c>staff:manage</c>, and both are about the record rather than
/// the token:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Branch.</b> A branch-bound manager manages the people at their branches. Someone they
///     share no site with answers <c>404</c>, the same as an id that does not exist.
///   </description></item>
///   <item><description>
///     <b>Rank.</b> A caller may not change or deactivate a member who holds scopes the caller
///     does not. Otherwise an admin could deactivate the owner and become the most privileged
///     account left — the same reasoning as <see cref="ScopeGrantPolicy"/>, from the other side.
///   </description></item>
/// </list>
/// </summary>
public sealed class StaffMemberAccess(
    IStaffRepository staff,
    IIdentityRepository identity,
    ScopeGrantPolicy grants,
    BranchGuard branchGuard)
{
    /// <summary>The member, tracked, together with the roles they currently hold.</summary>
    /// <exception cref="NotFoundException">No such member, or not one at the caller's branches.</exception>
    /// <exception cref="InsufficientScopeException">The member holds scopes the caller does not.</exception>
    public async Task<(StaffMembership Membership, IReadOnlyList<Role> Roles)> ForChangeAsync(
        string staffId,
        CancellationToken cancellationToken = default)
    {
        var membership = await staff.FindTrackedByIdAsync(staffId, cancellationToken)
            ?? throw new NotFoundException("staff_member", staffId);

        if (branchGuard.VisibleBranchIds is { } visible
            && !membership.BranchIds.Any(visible.Contains))
        {
            throw new NotFoundException("staff_member", staffId);
        }

        var roles = await identity.FindRolesAsync(membership.RoleIds, cancellationToken);

        grants.EnsureCallerHolds(roles.SelectMany(role => role.Scopes));

        return (membership, roles);
    }
}
