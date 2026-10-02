using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Auditing;

/// <summary>
/// EF Core implementation of the audit port.
/// <para>
/// Newest first, keyset-paged on <c>(occurred_at, id)</c>. An audit log is appended to constantly
/// while it is being read, which is the case offset paging handles worst: an investigator paging
/// through an incident would see rows shift under them as new ones land at the top.
/// </para>
/// </summary>
internal sealed class AuditEntryRepository(HappyPawsDbContext db) : IAuditEntryRepository
{
    public void Add(AuditEntry entry) => db.Set<AuditEntry>().Add(entry);

    public async Task<CursorPage<AuditEntry>> ListAsync(
        AuditLogFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<AuditEntry>().AsNoTracking().AsQueryable();

        if (filter.VisibleBranchIds is { Count: > 0 } visible)
        {
            // Rows with no branch belong to no site — a sign-in, a consent change — and stay
            // visible. Excluding them would hide account-level events from every branch auditor
            // at once.
            query = query.Where(entry => entry.BranchId == null || visible.Contains(entry.BranchId));
        }

        if (filter.AccessOnly)
        {
            var access = AuditActions.AccessActions;
            query = query.Where(entry => access.Contains(entry.Action));
        }

        if (filter.Action is { Length: > 0 } action)
        {
            query = query.Where(entry => entry.Action == action);
        }

        if (filter.ResourceType is { Length: > 0 } resourceType)
        {
            query = query.Where(entry => entry.ResourceType == resourceType);
        }

        if (filter.ResourceId is { Length: > 0 } resourceId)
        {
            query = query.Where(entry => entry.ResourceId == resourceId);
        }

        if (filter.PetId is { Length: > 0 } petId)
        {
            query = query.Where(entry => entry.PetId == petId);
        }

        if (filter.ActorUserId is { Length: > 0 } actorUserId)
        {
            query = query.Where(entry => entry.ActorUserId == actorUserId);
        }

        if (filter.From is { } from)
        {
            query = query.Where(entry => entry.OccurredAt >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(entry => entry.OccurredAt < to);
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            query = query.Where(entry => entry.OccurredAt < sortKey
                || (entry.OccurredAt == sortKey && string.Compare(entry.Id, id) < 0));
        }

        var rows = await query
            .OrderByDescending(entry => entry.OccurredAt).ThenByDescending(entry => entry.Id)
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        if (rows.Count <= page.SafeLimit)
        {
            return new CursorPage<AuditEntry>(rows, null);
        }

        var data = rows.Take(page.SafeLimit).ToList();
        var last = data[^1];

        return new CursorPage<AuditEntry>(data, Cursor.Encode(last.OccurredAt, last.Id));
    }
}
