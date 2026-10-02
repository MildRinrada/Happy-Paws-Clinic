using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Staff;

/// <summary>EF Core implementation of the staff port.</summary>
internal sealed class StaffRepository(HappyPawsDbContext db) : IStaffRepository
{
    public void Add(StaffMembership membership) => db.Set<StaffMembership>().Add(membership);

    public Task<StaffMembership?> FindTrackedByIdAsync(string staffId, CancellationToken cancellationToken = default) =>
        db.Set<StaffMembership>().FirstOrDefaultAsync(membership => membership.Id == staffId, cancellationToken);

    public async Task<StaffMemberRow?> FindRowAsync(string staffId, CancellationToken cancellationToken = default)
    {
        var row = await Rows()
            .Where(row => row.Membership.Id == staffId)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : new StaffMemberRow(row.Membership, row.Account);
    }

    public async Task<IReadOnlyList<StaffMemberRow>> FindRowsAsync(
        IReadOnlyCollection<string> staffIds,
        CancellationToken cancellationToken = default)
    {
        var ids = staffIds.ToArray();

        return await Rows()
            .Where(row => ids.Contains(row.Membership.Id))
            .Select(row => new StaffMemberRow(row.Membership, row.Account))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Newest first, keyset on <c>(created_at, id)</c>, like every other list here. Every filter —
    /// including the caller's own branches — is part of the query, so a page is never short and a
    /// cursor never points at a row the caller could not have been shown.
    /// </summary>
    public async Task<CursorPage<StaffMemberRow>> ListAsync(
        StaffMemberFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var rows = Rows();

        if (filter.IsActive is { } isActive)
        {
            rows = rows.Where(row => row.Membership.IsActive == isActive);
        }

        if (filter.StaffType is { Length: > 0 } staffType)
        {
            rows = rows.Where(row => row.Membership.StaffType == staffType);
        }

        if (filter.BranchId is { Length: > 0 } branchId)
        {
            rows = rows.Where(row => row.Membership.BranchIds.Contains(branchId));
        }

        if (filter.Department is { Length: > 0 } department)
        {
            rows = rows.Where(row => row.Membership.Department == department);
        }

        if (filter.RoleId is { Length: > 0 } roleId)
        {
            rows = rows.Where(row => row.Membership.RoleIds.Contains(roleId));
        }

        if (filter.VisibleBranchIds is { } visible)
        {
            var visibleIds = visible.ToArray();
            rows = rows.Where(row => row.Membership.BranchIds.Any(id => visibleIds.Contains(id)));
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);

            rows = rows.Where(row =>
                row.Membership.CreatedAt < sortKey
                || (row.Membership.CreatedAt == sortKey && string.Compare(row.Membership.Id, id) < 0));
        }

        var limit = page.SafeLimit;

        var fetched = await rows
            .OrderByDescending(row => row.Membership.CreatedAt).ThenByDescending(row => row.Membership.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = fetched.Count > limit;
        var data = (hasMore ? fetched[..limit] : fetched)
            .Select(row => new StaffMemberRow(row.Membership, row.Account))
            .ToList();

        return new CursorPage<StaffMemberRow>(
            data,
            hasMore ? Cursor.Encode(data[^1].Membership.CreatedAt, data[^1].Membership.Id) : null);
    }

    public async Task<IReadOnlyDictionary<string, int>> StaffTypeCountsAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<StaffMembership>().AsNoTracking()
            .Where(membership => membership.IsActive)
            .GroupBy(membership => membership.StaffType)
            .Select(group => new { StaffType = group.Key, Members = group.Count() })
            .ToDictionaryAsync(entry => entry.StaffType, entry => entry.Members, cancellationToken);

    public async Task<IReadOnlyDictionary<string, int>> DepartmentCountsAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<StaffMembership>().AsNoTracking()
            .Where(membership => membership.IsActive)
            .GroupBy(membership => membership.Department)
            .Select(group => new { Department = group.Key, Members = group.Count() })
            .ToDictionaryAsync(entry => entry.Department, entry => entry.Members, cancellationToken);

    /// <summary>
    /// Only accounts that can still sign in: a suspended account belongs to someone the clinic has
    /// stepped away from, and the website should not keep introducing them.
    /// </summary>
    public async Task<IReadOnlyList<StaffMemberRow>> ListOnWebsiteAsync(
        string? department,
        CancellationToken cancellationToken = default)
    {
        var rows = Rows().Where(row =>
            row.Membership.IsActive
            && row.Membership.ShowOnWebsite
            && row.Account.Status == UserStatus.Active);

        if (department is { Length: > 0 })
        {
            rows = rows.Where(row => row.Membership.Department == department);
        }

        return await rows
            .Select(row => new StaffMemberRow(row.Membership, row.Account))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> MissingAsync(
        IReadOnlyCollection<string> staffIds,
        CancellationToken cancellationToken = default)
    {
        var ids = staffIds.ToArray();

        var found = await db.Set<StaffMembership>().AsNoTracking()
            .Where(membership => ids.Contains(membership.Id) && membership.IsActive)
            .Select(membership => membership.Id)
            .ToListAsync(cancellationToken);

        return [.. ids.Except(found, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Answered in two small reads and a comparison in memory, rather than one query that overlaps
    /// two arrays. The staff of one clinic is tens of rows, and "does an owner remain" is the one
    /// question in this module that must never be wrong because a translation was clever.
    /// </summary>
    public async Task<bool> AnyActiveUnrestrictedAsync(CancellationToken cancellationToken = default)
    {
        const string everything = Scopes.Wildcard;

        var unrestrictedRoleIds = await db.Set<Role>().AsNoTracking()
            .Where(role => role.Scopes.Contains(everything))
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        if (unrestrictedRoleIds.Count == 0)
        {
            return false;
        }

        // An active membership on an account that cannot sign in is not an owner anybody can be.
        var roleSets = await Rows()
            .Where(row => row.Membership.IsActive && row.Account.Status == UserStatus.Active)
            .Select(row => row.Membership.RoleIds)
            .ToListAsync(cancellationToken);

        return roleSets.Any(roleIds => roleIds.Any(unrestrictedRoleIds.Contains));
    }

    public Task<bool> AnyActiveWithRoleAsync(string roleId, CancellationToken cancellationToken = default) =>
        db.Set<StaffMembership>()
            .AnyAsync(membership => membership.IsActive && membership.RoleIds.Contains(roleId), cancellationToken);

    private IQueryable<MembershipWithAccount> Rows() =>
        from membership in db.Set<StaffMembership>().AsNoTracking()
        join account in db.Set<UserAccount>().AsNoTracking() on membership.UserId equals account.Id
        select new MembershipWithAccount { Membership = membership, Account = account };

    /// <summary>Query-side pairing. A class with settable members, because that is what EF can compose over.</summary>
    private sealed class MembershipWithAccount
    {
        public required StaffMembership Membership { get; init; }

        public required UserAccount Account { get; init; }
    }
}

/// <summary>EF Core implementation of the staff-invitation port.</summary>
internal sealed class StaffInvitationRepository(HappyPawsDbContext db) : IStaffInvitationRepository
{
    public void Add(StaffInvitation invitation) => db.Set<StaffInvitation>().Add(invitation);

    public Task<StaffInvitation?> FindTrackedByIdAsync(
        string invitationId,
        CancellationToken cancellationToken = default) =>
        db.Set<StaffInvitation>()
            .FirstOrDefaultAsync(invitation => invitation.Id == invitationId, cancellationToken);

    public Task<StaffInvitation?> FindOpenByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        db.Set<StaffInvitation>()
            .FirstOrDefaultAsync(
                invitation => invitation.Email == email && invitation.AcceptedAt == null, cancellationToken);

    public async Task<CursorPage<StaffInvitation>> ListOpenAsync(
        StaffMemberFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var invitations = db.Set<StaffInvitation>().AsNoTracking()
            .Where(invitation => invitation.AcceptedAt == null);

        if (filter.StaffType is { Length: > 0 } staffType)
        {
            invitations = invitations.Where(invitation => invitation.StaffType == staffType);
        }

        if (filter.BranchId is { Length: > 0 } branchId)
        {
            invitations = invitations.Where(invitation => invitation.BranchIds.Contains(branchId));
        }

        // An invitation carries no department; it will start in the one its staff type defaults to.
        if (filter.Department is { Length: > 0 } department)
        {
            if (department == Departments.Support)
            {
                var mapped = Departments.StaffTypeDefaults.Keys.ToArray();
                invitations = invitations.Where(invitation => !mapped.Contains(invitation.StaffType));
            }
            else
            {
                var staffTypes = Departments.StaffTypeDefaults
                    .Where(entry => entry.Value == department)
                    .Select(entry => entry.Key)
                    .ToArray();
                invitations = invitations.Where(invitation => staffTypes.Contains(invitation.StaffType));
            }
        }

        if (filter.RoleId is { Length: > 0 } roleId)
        {
            invitations = invitations.Where(invitation => invitation.RoleIds.Contains(roleId));
        }

        if (filter.VisibleBranchIds is { } visible)
        {
            var visibleIds = visible.ToArray();
            invitations = invitations.Where(
                invitation => invitation.BranchIds.Any(id => visibleIds.Contains(id)));
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);

            invitations = invitations.Where(invitation =>
                invitation.CreatedAt < sortKey
                || (invitation.CreatedAt == sortKey && string.Compare(invitation.Id, id) < 0));
        }

        var limit = page.SafeLimit;

        var rows = await invitations
            .OrderByDescending(invitation => invitation.CreatedAt).ThenByDescending(invitation => invitation.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var data = hasMore ? rows[..limit] : rows;

        return new CursorPage<StaffInvitation>(
            data, hasMore ? Cursor.Encode(data[^1].CreatedAt, data[^1].Id) : null);
    }
}
