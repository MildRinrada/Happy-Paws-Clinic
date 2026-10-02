using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff;

/// <summary>A staff membership together with the account it belongs to — one row of the staff list.</summary>
public sealed record StaffMemberRow(StaffMembership Membership, UserAccount Account);

/// <summary>What <c>GET /staff/members</c> can narrow by — "กรองตามสาขา ตำแหน่ง สถานะ".</summary>
/// <param name="BranchId">Members assigned to this branch.</param>
/// <param name="StaffType">Members with this position.</param>
/// <param name="IsActive">True for working members, false for deactivated ones, null for both.</param>
/// <param name="VisibleBranchIds">
/// The caller's own branches, or null when the caller is not branch-bound. Applied in the query,
/// not afterwards, so a page is never short and a cursor never points past the caller's reach.
/// </param>
/// <param name="Department">Members in this department (extension, § 10).</param>
/// <param name="RoleId">Members holding this role (extension, § 10).</param>
public sealed record StaffMemberFilter(
    string? BranchId = null,
    string? StaffType = null,
    bool? IsActive = null,
    IReadOnlyCollection<string>? VisibleBranchIds = null,
    string? Department = null,
    string? RoleId = null);

/// <summary>Persistence port for staff memberships.</summary>
public interface IStaffRepository
{
    void Add(StaffMembership membership);

    /// <summary>Tracked, for the handlers that change a member.</summary>
    Task<StaffMembership?> FindTrackedByIdAsync(string staffId, CancellationToken cancellationToken = default);

    Task<StaffMemberRow?> FindRowAsync(string staffId, CancellationToken cancellationToken = default);

    /// <summary>Several members with their accounts, in no particular order. Ids that match nothing are absent.</summary>
    Task<IReadOnlyList<StaffMemberRow>> FindRowsAsync(
        IReadOnlyCollection<string> staffIds,
        CancellationToken cancellationToken = default);

    Task<CursorPage<StaffMemberRow>> ListAsync(
        StaffMemberFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>How many active members hold each staff type, for <c>GET /staff/staff-types</c>.</summary>
    Task<IReadOnlyDictionary<string, int>> StaffTypeCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>How many active members are in each department, for <c>GET /staff/departments</c>.</summary>
    Task<IReadOnlyDictionary<string, int>> DepartmentCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Active members who agreed to be listed on the website, optionally in one department. In no
    /// particular order; the public team page sorts them.
    /// </summary>
    Task<IReadOnlyList<StaffMemberRow>> ListOnWebsiteAsync(
        string? department,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The ids in <paramref name="staffIds"/> that are not an active member. Empty when all are —
    /// how a service checks the providers it was told may perform it.
    /// </summary>
    Task<IReadOnlyList<string>> MissingAsync(
        IReadOnlyCollection<string> staffIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when at least one active member, on an account that can still sign in, holds a role
    /// containing <c>*</c>. See <c>OwnerLockoutGuard</c>.
    /// </summary>
    Task<bool> AnyActiveUnrestrictedAsync(CancellationToken cancellationToken = default);

    /// <summary>True when some active member holds the given role — how bootstrap decides an owner already exists.</summary>
    Task<bool> AnyActiveWithRoleAsync(string roleId, CancellationToken cancellationToken = default);
}

/// <summary>Persistence port for staff invitations.</summary>
public interface IStaffInvitationRepository
{
    void Add(StaffInvitation invitation);

    /// <summary>Tracked: accepting and re-issuing both change the row.</summary>
    Task<StaffInvitation?> FindTrackedByIdAsync(string invitationId, CancellationToken cancellationToken = default);

    /// <summary>The not-yet-accepted invitation for an address, tracked, or null.</summary>
    Task<StaffInvitation?> FindOpenByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Invitations nobody has accepted yet, newest first — the "invited" view of the staff list.</summary>
    Task<CursorPage<StaffInvitation>> ListOpenAsync(
        StaffMemberFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);
}
