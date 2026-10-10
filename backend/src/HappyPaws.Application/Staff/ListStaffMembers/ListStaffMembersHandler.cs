using HappyPaws.Application.Access;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.ListStaffMembers;

/// <summary><c>GET /staff/members</c> — "กรองตามสาขา ตำแหน่ง สถานะ".</summary>
/// <param name="BranchId">Members assigned to this branch.</param>
/// <param name="StaffType">Members with this position.</param>
/// <param name="Status"><c>active</c>, <c>deactivated</c>, <c>invited</c>, or omitted for everyone employed.</param>
/// <param name="Limit">Page size.</param>
/// <param name="Cursor">Opaque cursor from the previous page.</param>
/// <param name="Department">Members in this department, a code from <see cref="Departments"/> (extension, § 10).</param>
/// <param name="Role">Members holding the role with this name (extension, § 10).</param>
public sealed record ListStaffMembersQuery(
    string? BranchId = null,
    string? StaffType = null,
    string? Status = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null,
    string? Department = null,
    string? Role = null);

/// <summary>
/// The staff list.
/// <para>
/// <c>?status=invited</c> reads a different table. The contract lets a clinic invite someone and
/// gives no endpoint that lists who has been invited, so an invitation nobody has accepted is
/// treated as what it looks like from the owner's chair: a member of staff whose status is
/// "invited". It is its own page rather than mixed into the others because the two sources cannot
/// share one cursor.
/// </para>
/// <para>
/// Branch-scoped like every other staff read: a member bound to branches sees colleagues who share
/// one. The filter is built into the query, so pages are never short.
/// </para>
/// <para>
/// <c>?department=</c> and <c>?role=</c> answer two different questions: which team someone is
/// in, and what they may do. A department outside the fixed list is a mistake and answers
/// <c>422</c>; a role name nobody uses is just a role with no members, like an unused staff type.
/// </para>
/// </summary>
public sealed class ListStaffMembersHandler(
    IStaffRepository staff,
    IStaffInvitationRepository invitations,
    IRoleRepository roles,
    IFileStorage storage,
    BranchGuard branchGuard)
{
    public async Task<CursorPage<StaffMemberDto>> HandleAsync(
        ListStaffMembersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.BranchId is { Length: > 0 } branchId)
        {
            branchGuard.EnsureCanAccess(branchId);
        }

        var status = query.Status?.Trim().ToLowerInvariant();

        if (status is not (null or "" or StaffMemberDto.Active or StaffMemberDto.Deactivated or StaffMemberDto.Invited))
        {
            throw Invalid("status", "สถานะต้องเป็น active, deactivated หรือ invited");
        }

        var department = query.Department?.Trim().ToLowerInvariant();

        if (department is { Length: > 0 } && !Departments.IsKnown(department))
        {
            throw Invalid(
                "department",
                $"แผนกต้องเป็นหนึ่งใน {string.Join(", ", Departments.All.Select(known => known.Code))}");
        }

        var allRoles = await roles.ListAsync(cancellationToken);
        var roleNames = allRoles.ToDictionary(role => role.Id, role => role.Name);

        string? roleId = null;

        if (query.Role is { Length: > 0 } roleName)
        {
            // A name that matches no role still filters: by an id nothing holds, so the page is empty.
            roleId = allRoles.FirstOrDefault(role => role.Name == roleName.Trim())?.Id ?? $"unknown:{roleName}";
        }

        var filter = new StaffMemberFilter(
            query.BranchId,
            query.StaffType,
            status switch
            {
                StaffMemberDto.Active => true,
                StaffMemberDto.Deactivated => false,
                _ => null,
            },
            branchGuard.VisibleBranchIds,
            department is { Length: > 0 } ? department : null,
            roleId);

        var page = new CursorPageRequest(query.Limit, query.Cursor);

        if (status == StaffMemberDto.Invited)
        {
            var invited = await invitations.ListOpenAsync(filter, page, cancellationToken);

            return new CursorPage<StaffMemberDto>(
                [.. invited.Data.Select(invitation => StaffMemberDto.From(invitation, roleNames))],
                invited.NextCursor);
        }

        var members = await staff.ListAsync(filter, page, cancellationToken);

        return new CursorPage<StaffMemberDto>(
            [.. members.Data.Select(row => StaffMemberDto.From(row, roleNames, storage))],
            members.NextCursor);
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
