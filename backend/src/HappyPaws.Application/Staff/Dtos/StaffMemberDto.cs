using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Staff.Photos;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.Dtos;

/// <summary>
/// One person on the staff list — <c>GET /staff/members</c>.
/// <para>
/// <c>Roles</c> are names and <c>Branches</c> are ids, exactly as <c>GET /me</c> prints a staff
/// membership, so a client that already renders one can render the other.
/// </para>
/// <para>
/// <c>Status</c> is <c>active</c>, <c>deactivated</c> or <c>invited</c>. An invited row is an
/// invitation nobody has accepted yet: its <c>Id</c> is the invitation's (<c>sin_…</c>), it has no
/// <c>UserId</c>, and <c>InvitationExpiresAt</c> says how long the link has left.
/// </para>
/// <para>
/// <c>Department</c> and <c>PublicProfile</c> are extensions (§ 10). An invited row's department is
/// the one its staff type will start in, and it has no public profile yet.
/// </para>
/// </summary>
public sealed record StaffMemberDto(
    string Id,
    uint Version,
    string? UserId,
    string DisplayName,
    string? Email,
    string StaffType,
    string? LicenseNo,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Branches,
    string Status,
    bool TwoFactorEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? InvitationExpiresAt = null,
    string Department = Departments.Support,
    StaffPublicProfileDto? PublicProfile = null)
{
    public const string Active = "active";
    public const string Deactivated = "deactivated";
    public const string Invited = "invited";

    /// <param name="row">The member and their account.</param>
    /// <param name="roleNames">Role names by id.</param>
    /// <param name="storage">Mints the preview link of an uploaded photo (§ 32); without it there is no <c>photo_url</c>.</param>
    public static StaffMemberDto From(StaffMemberRow row, IReadOnlyDictionary<string, string> roleNames, IFileStorage? storage = null) =>
        new(
            row.Membership.Id,
            row.Membership.Version,
            row.Account.Id,
            row.Account.DisplayName,
            row.Account.Email,
            row.Membership.StaffType,
            row.Membership.LicenseNo,
            Names(row.Membership.RoleIds, roleNames),
            row.Membership.BranchIds,
            row.Membership.IsActive ? Active : Deactivated,
            row.Account.TwoFactorEnabled,
            row.Membership.CreatedAt,
            Department: row.Membership.Department,
            PublicProfile: StaffPublicProfileDto.From(row.Membership.PublicProfile, storage));

    public static StaffMemberDto From(StaffInvitation invitation, IReadOnlyDictionary<string, string> roleNames) =>
        new(
            invitation.Id,
            invitation.Version,
            UserId: null,
            invitation.DisplayName,
            invitation.Email,
            invitation.StaffType,
            invitation.LicenseNo,
            Names(invitation.RoleIds, roleNames),
            invitation.BranchIds,
            Invited,
            TwoFactorEnabled: false,
            invitation.CreatedAt,
            invitation.ExpiresAt,
            Department: Departments.ForStaffType(invitation.StaffType));

    /// <summary>A role id with no name was deleted by hand; it grants nothing, so it is not shown.</summary>
    private static IReadOnlyList<string> Names(
        IReadOnlyList<string> roleIds,
        IReadOnlyDictionary<string, string> roleNames) =>
        [.. roleIds.Where(roleNames.ContainsKey).Select(id => roleNames[id])];
}

/// <summary>A member's public profile as staff see and edit it. Extension, § 10.</summary>
/// <param name="JobTitle">Title the public reads.</param>
/// <param name="Specialty">Field of work.</param>
/// <param name="Bio">A few sentences.</param>
/// <param name="PhotoPath">
/// Site-relative photo, or an uploaded one as <c>storage:staff-photos/...</c> (§ 32), or null. Sent
/// back as it is on the next <c>PUT</c> to keep the photo.
/// </param>
/// <param name="CareerStartYear">Year (AD) they started in the profession.</param>
/// <param name="ShowOnWebsite">Whether <c>GET /team</c> lists them.</param>
/// <param name="PhotoUrl">A signed link to an uploaded photo, good for half an hour, for the preview. Null for a site path.</param>
public sealed record StaffPublicProfileDto(
    string? JobTitle,
    string? Specialty,
    string? Bio,
    string? PhotoPath,
    int? CareerStartYear,
    bool ShowOnWebsite,
    string? PhotoUrl = null)
{
    public static StaffPublicProfileDto From(StaffPublicProfile profile, IFileStorage? storage = null) =>
        new(
            profile.JobTitle,
            profile.Specialty,
            profile.Bio,
            profile.PhotoPath,
            profile.CareerStartYear,
            profile.ShowOnWebsite,
            storage is null ? null : StaffPhotoLinks.LinkOf(profile.PhotoPath, storage, StaffPhotoLinks.StaffLinkLifetime));
}

/// <summary>
/// One entry of <c>GET /staff/staff-types</c> — "ประเภทพนักงานที่คลินิกนิยามเอง".
/// </summary>
/// <param name="StaffType">The label, as it appears on a membership.</param>
/// <param name="Members">How many active members carry it. Zero for a suggestion nobody uses yet.</param>
public sealed record StaffTypeDto(string StaffType, int Members);

/// <summary>One entry of <c>GET /staff/departments</c>. Extension, § 10.</summary>
/// <param name="Department">The code, as it appears on a membership and in <c>?department=</c>.</param>
/// <param name="Name">The Thai name.</param>
/// <param name="Members">How many active members are in it.</param>
public sealed record DepartmentDto(string Department, string Name, int Members);
