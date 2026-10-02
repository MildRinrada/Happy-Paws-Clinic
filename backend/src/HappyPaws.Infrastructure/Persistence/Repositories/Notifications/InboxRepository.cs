using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Notifications;

/// <summary>
/// The web inbox.
/// <para>
/// Delivery writes through a context of its own. Messages are sent after a use case commits, on the
/// same request; saving the request's own context there would also flush anything it still tracks,
/// and a notification must never be what saves someone else's half-finished change.
/// </para>
/// </summary>
internal sealed class InboxRepository(HappyPawsDbContext db, DbContextOptions<HappyPawsDbContext> options) : IInboxRepository
{
    public async Task DeliverAsync(InboxNotification notification, CancellationToken cancellationToken = default)
    {
        await using var own = new HappyPawsDbContext(options);

        own.Set<InboxNotification>().Add(notification);
        await own.SaveChangesAsync(cancellationToken);
    }

    public async Task<CursorPage<InboxNotification>> ListAsync(
        string customerId,
        bool unreadOnly,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<InboxNotification>().AsNoTracking().Where(item => item.CustomerId == customerId);

        if (unreadOnly)
        {
            query = query.Where(item => item.ReadAt == null);
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            query = query.Where(item => item.CreatedAt < sortKey || (item.CreatedAt == sortKey && string.Compare(item.Id, id) < 0));
        }

        var limit = page.SafeLimit;
        var rows = await query
            .OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var data = hasMore ? rows[..limit] : rows;

        return new CursorPage<InboxNotification>(data, hasMore ? Cursor.Encode(data[^1].CreatedAt, data[^1].Id) : null);
    }

    public Task<int> UnreadCountAsync(string customerId, CancellationToken cancellationToken = default) =>
        db.Set<InboxNotification>().CountAsync(item => item.CustomerId == customerId && item.ReadAt == null, cancellationToken);

    public async Task<int> MarkReadAsync(
        string customerId,
        IReadOnlyCollection<string>? ids,
        DateTimeOffset at,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<InboxNotification>().Where(item => item.CustomerId == customerId && item.ReadAt == null);

        if (ids is { Count: > 0 })
        {
            query = query.Where(item => ids.Contains(item.Id));
        }

        // One UPDATE, scoped to the caller's own rows by the WHERE — someone else's id simply matches nothing.
        return await query.ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ReadAt, at), cancellationToken);
    }
}
