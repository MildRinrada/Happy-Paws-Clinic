using HappyPaws.Application.Access;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Access;

namespace HappyPaws.Application.Staff;

/// <summary>
/// Turns the roles and branches a request names into ones this clinic actually has, and refuses
/// the ones the caller has no business handing out.
/// <para>
/// Shared by inviting a member and editing one, because both answer the same two questions — "do
/// these exist" and "may <i>you</i> assign them" — and an invitation that was checked less
/// carefully than an edit would simply become the way to get around the edit's checks.
/// </para>
/// </summary>
public sealed class StaffAssignmentResolver(
    IRoleRepository roles,
    IBranchRepository branches,
    BranchGuard branchGuard)
{
    /// <summary>
    /// Resolves role names, as <c>GET /me</c> prints them, to roles.
    /// </summary>
    /// <exception cref="ValidationFailedException">A name matches no role, or names the portal role.</exception>
    public async Task<IReadOnlyList<Role>> RolesAsync(
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        var names = roleNames.Distinct(StringComparer.Ordinal).ToArray();
        var found = await roles.FindByNamesAsync(names, cancellationToken);

        var unknown = names.Except(found.Select(role => role.Name), StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
        {
            throw Invalid("roles", $"ไม่พบ role: {string.Join(", ", unknown)}");
        }

        if (found.Any(role => role.IsPortalRole))
        {
            // The customer role describes the portal. On a staff membership it would grant the
            // portal marker to someone with no customer record behind it.
            throw Invalid("roles", "role สำหรับลูกค้ากำหนดให้พนักงานไม่ได้");
        }

        return found;
    }

    /// <summary>
    /// Checks that every branch exists and that the caller works at it.
    /// <para>
    /// An empty list is refused from a branch-bound caller. A membership with no branches is how a
    /// single-site clinic is represented, and <see cref="BranchGuard"/> reads it as "not
    /// branch-bound" — so leaving the list empty would be a way to grant wider reach than the
    /// caller has.
    /// </para>
    /// </summary>
    /// <exception cref="ValidationFailedException">A branch does not exist, or the list is empty when it may not be.</exception>
    /// <exception cref="ForbiddenException">The caller is not assigned to one of the branches.</exception>
    public async Task<IReadOnlyList<string>> BranchesAsync(
        IReadOnlyList<string> branchIds,
        CancellationToken cancellationToken = default)
    {
        var ids = branchIds.Distinct(StringComparer.Ordinal).ToArray();

        if (ids.Length == 0)
        {
            return branchGuard.VisibleBranchIds is null
                ? ids
                : throw Invalid("branches", "ต้องระบุสาขาอย่างน้อยหนึ่งสาขา");
        }

        var missing = await branches.MissingAsync(ids, cancellationToken);
        if (missing.Count > 0)
        {
            throw Invalid("branches", $"ไม่พบสาขา: {string.Join(", ", missing)}");
        }

        foreach (var id in ids)
        {
            branchGuard.EnsureCanAccess(id);
        }

        return ids;
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
