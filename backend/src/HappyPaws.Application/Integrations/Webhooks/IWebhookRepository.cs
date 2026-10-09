using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.Webhooks;

/// <summary>
/// Subscriptions, the outbox and the deliveries.
/// <para>
/// The two <c>Claim</c> methods lock with <c>FOR UPDATE SKIP LOCKED</c> inside the caller's
/// transaction, so several API instances share the work without a coordinator, the same way the
/// report exports do.
/// </para>
/// </summary>
public interface IWebhookRepository
{
    void Add(WebhookSubscription subscription);

    void AddEvent(WebhookEvent webhookEvent);

    void AddDelivery(WebhookDelivery delivery);

    Task<WebhookSubscription?> FindAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>Subscriptions that are on, for fanning events out. A clinic has a handful.</summary>
    Task<IReadOnlyList<WebhookSubscription>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>The oldest outbox rows not yet turned into deliveries, locked.</summary>
    Task<IReadOnlyList<WebhookEvent>> ClaimUndispatchedAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>The pending delivery due longest ago, locked; null when none is due.</summary>
    Task<WebhookDelivery?> ClaimDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<WebhookDelivery?> LoadDeliveryAsync(string deliveryId, CancellationToken cancellationToken = default);

    Task<WebhookEvent?> FindEventAsync(string eventId, CancellationToken cancellationToken = default);

    /// <summary>One subscription's deliveries, newest first.</summary>
    Task<CursorPage<WebhookDelivery>> ListDeliveriesAsync(
        string subscriptionId,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);
}
