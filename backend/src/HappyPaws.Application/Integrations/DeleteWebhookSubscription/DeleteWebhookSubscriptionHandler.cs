using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.DeleteWebhookSubscription;

/// <summary>
/// <c>DELETE /staff/webhooks/{id}</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 22</c>.
/// </summary>
/// <param name="SubscriptionId">The webhook.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record DeleteWebhookSubscriptionCommand(string SubscriptionId, uint? IfMatchVersion = null);

/// <summary>
/// Deletes a webhook: it leaves the list, nothing more is sent to it, and its secret is destroyed.
/// <para>
/// <b>The delivery log stays.</b> Deliveries are deleted with their webhook by a cascade, so
/// removing the row would take the answer to "did they ever get event X" with it. The row is kept
/// instead, switched off, with an empty secret (the marker, <see cref="WebhookSubscription.IsDeleted"/>)
/// and the query string cut from its URL. Its log stays readable by id until housekeeping removes
/// the events after 30 days. Pending deliveries stop as <c>failed</c> with <c>webhook_deleted</c>.
/// Deleting it again is <c>404</c>.
/// </para>
/// </summary>
public sealed class DeleteWebhookSubscriptionHandler(
    IWebhookRepository webhooks,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task HandleAsync(DeleteWebhookSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var subscription = await webhooks.LoadLiveAsync(command.SubscriptionId, token);

            ConcurrencyGuard.Ensure(subscription, command.IfMatchVersion);

            var wasActive = subscription.Active;
            subscription.Delete();

            await webhooks.HaltPendingAsync(subscription.Id, WebhookDelivery.HaltedDeleted, clock.UtcNow, token);

            audit.Record(new AuditEvent(
                AuditActions.WebhookDeleted,
                AuditResources.Webhook,
                subscription.Id,
                Details: new Dictionary<string, object?>
                {
                    ["destination"] = WebhookLookup.Destination(subscription.Url),
                    ["events"] = subscription.Events.ToArray(),
                    ["was_active"] = wasActive,
                }));
        }, cancellationToken);
    }
}
