using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Notifications;

/// <summary>
/// The delivery log.
/// <para>
/// Rows are written through a context of their own, for the same reason as the web inbox: a
/// message goes out after the use case committed, on the same request, and saving the request's
/// context here would also flush whatever else it still tracks.
/// </para>
/// </summary>
internal sealed class DeliveryLogRepository(HappyPawsDbContext db, DbContextOptions<HappyPawsDbContext> options) : IDeliveryLog
{
    public async Task RecordAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        await using var own = new HappyPawsDbContext(options);

        own.Set<NotificationDelivery>().Add(delivery);
        await own.SaveChangesAsync(cancellationToken);
    }

    public async Task<CursorPage<DeliveryLogRow>> ListAsync(
        DeliveryLogFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<NotificationDelivery>().AsNoTracking();

        if (filter.BranchIds is { } visible)
        {
            query = query.Where(item => item.BranchId == null || visible.Contains(item.BranchId));
        }

        if (filter.Status is { } status)
        {
            query = query.Where(item => item.Status == status);
        }

        if (filter.Channel is { } channel)
        {
            query = query.Where(item => item.Channel == channel);
        }

        if (filter.Source is { } source)
        {
            query = query.Where(item => item.Source == source);
        }

        if (filter.CustomerId is { } customerId)
        {
            query = query.Where(item => item.CustomerId == customerId);
        }

        if (filter.TemplateKey is { } templateKey)
        {
            query = query.Where(item => item.TemplateKey == templateKey);
        }

        if (filter.BatchId is { } batchId)
        {
            query = query.Where(item => item.BatchId == batchId);
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
        var deliveries = hasMore ? rows[..limit] : rows;

        // One query for the page's names rather than a join, so the paging query stays the plain
        // indexed scan the cursor is built for.
        var customerIds = deliveries.Select(item => item.CustomerId).Distinct().ToList();
        var names = await db.Set<Customer>().AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, customer => customer.DisplayName, cancellationToken);

        var data = deliveries.Select(item => new DeliveryLogRow(item, names.GetValueOrDefault(item.CustomerId))).ToList();

        return new CursorPage<DeliveryLogRow>(
            data, hasMore ? Cursor.Encode(data[^1].Delivery.CreatedAt, data[^1].Delivery.Id) : null);
    }

    public async Task<IReadOnlyList<NotificationDelivery>> ForBatchAsync(string batchId, CancellationToken cancellationToken = default) =>
        await db.Set<NotificationDelivery>().AsNoTracking()
            .Where(item => item.BatchId == batchId)
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public Task<int> CountSentSinceAsync(
        DeliverySource source,
        DeliveryChannel channel,
        DateTimeOffset since,
        CancellationToken cancellationToken = default) =>
        db.Set<NotificationDelivery>()
            .CountAsync(
                item => item.Source == source
                    && item.Channel == channel
                    && item.Status == DeliveryStatus.Sent
                    && item.CreatedAt >= since,
                cancellationToken);
}
