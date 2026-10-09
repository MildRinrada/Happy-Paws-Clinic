using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Integrations;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Integrations;

/// <summary>
/// Webhooks, the outbox and the deliveries. The claims use <c>FOR UPDATE SKIP LOCKED</c> as the
/// report export queue does: a worker locks the rows it can, and rows another worker already
/// holds are skipped rather than waited for. The locking SQL is not composed further by EF.
/// </summary>
internal sealed class WebhookRepository(HappyPawsDbContext db) : IWebhookRepository
{
    public void Add(WebhookSubscription subscription) => db.Set<WebhookSubscription>().Add(subscription);

    public void AddEvent(WebhookEvent webhookEvent) => db.Set<WebhookEvent>().Add(webhookEvent);

    public void AddDelivery(WebhookDelivery delivery) => db.Set<WebhookDelivery>().Add(delivery);

    public Task<WebhookSubscription?> FindAsync(string subscriptionId, CancellationToken cancellationToken = default) =>
        db.Set<WebhookSubscription>().AsNoTracking()
            .FirstOrDefaultAsync(subscription => subscription.Id == subscriptionId, cancellationToken);

    public async Task<IReadOnlyList<WebhookSubscription>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await db.Set<WebhookSubscription>().AsNoTracking()
            .Where(subscription => subscription.Active)
            .OrderBy(subscription => subscription.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WebhookEvent>> ClaimUndispatchedAsync(int limit, CancellationToken cancellationToken = default) =>
        await db.Set<WebhookEvent>()
            .FromSqlInterpolated(
                $"""
                SELECT e.*, e.xmin FROM webhook_events e
                WHERE dispatched_at IS NULL
                ORDER BY occurred_at, id
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

    public async Task<WebhookDelivery?> ClaimDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var rows = await db.Set<WebhookDelivery>()
            .FromSqlInterpolated(
                $"""
                SELECT d.*, d.xmin FROM webhook_deliveries d
                WHERE status = 'Pending' AND next_attempt_at <= {now.ToUniversalTime()}
                ORDER BY next_attempt_at, id
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        return rows.FirstOrDefault();
    }

    public Task<WebhookDelivery?> LoadDeliveryAsync(string deliveryId, CancellationToken cancellationToken = default) =>
        db.Set<WebhookDelivery>().FirstOrDefaultAsync(delivery => delivery.Id == deliveryId, cancellationToken);

    public Task<WebhookEvent?> FindEventAsync(string eventId, CancellationToken cancellationToken = default) =>
        db.Set<WebhookEvent>().AsNoTracking().FirstOrDefaultAsync(webhookEvent => webhookEvent.Id == eventId, cancellationToken);

    public async Task<CursorPage<WebhookDelivery>> ListDeliveriesAsync(
        string subscriptionId,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<WebhookDelivery>().AsNoTracking()
            .Where(delivery => delivery.SubscriptionId == subscriptionId);

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            query = query.Where(delivery => delivery.CreatedAt < sortKey
                || (delivery.CreatedAt == sortKey && string.Compare(delivery.Id, id) < 0));
        }

        var rows = await query
            .OrderByDescending(delivery => delivery.CreatedAt).ThenByDescending(delivery => delivery.Id)
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        if (rows.Count <= page.SafeLimit)
        {
            return new CursorPage<WebhookDelivery>(rows, null);
        }

        var data = rows.Take(page.SafeLimit).ToList();
        var last = data[^1];

        return new CursorPage<WebhookDelivery>(data, Cursor.Encode(last.CreatedAt, last.Id));
    }
}
