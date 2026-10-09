using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.RedeliverWebhookDelivery;

/// <summary>
/// <c>POST /staff/webhooks/{id}/deliveries/{deliveryId}/redeliver</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 22</c>.
/// </summary>
public sealed record RedeliverWebhookDeliveryCommand(string SubscriptionId, string DeliveryId);

/// <summary>
/// Sends a failed delivery again: the same row goes back to <c>pending</c>, due now, and the
/// webhook job sends it on its next run with the same event id and body
/// (<see cref="WebhookDelivery.Redeliver"/>).
/// <para>
/// <b>Once.</b> Only a <c>failed</c> delivery can be sent again (<c>409 state_conflict</c>
/// otherwise), so a second press while the first is still queued, or two people pressing at once
/// (<c>409</c> on the row's version), never queues it twice. The webhook must exist and be on
/// (<c>404</c>, <c>409</c>): a switched-off webhook would stop it again before it left.
/// </para>
/// </summary>
public sealed class RedeliverWebhookDeliveryHandler(
    IWebhookRepository webhooks,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<WebhookDeliveryDto> HandleAsync(RedeliverWebhookDeliveryCommand command, CancellationToken cancellationToken = default)
    {
        WebhookDelivery? queued = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var subscription = await webhooks.FindAsync(command.SubscriptionId, token);

            if (subscription is not { IsDeleted: false })
            {
                throw new NotFoundException("webhook", command.SubscriptionId);
            }

            var delivery = await webhooks.LoadDeliveryAsync(command.DeliveryId, token);

            if (delivery is null || delivery.SubscriptionId != subscription.Id)
            {
                throw new NotFoundException("webhook_delivery", command.DeliveryId);
            }

            if (!subscription.Active)
            {
                throw new ConflictException(ErrorCodes.StateConflict, "webhook นี้ปิดอยู่ เปิดใช้งานก่อนแล้วจึงส่งซ้ำ");
            }

            var previousError = delivery.LastError;
            delivery.Redeliver(clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.WebhookRedelivered,
                AuditResources.Webhook,
                subscription.Id,
                Details: new Dictionary<string, object?>
                {
                    ["delivery_id"] = delivery.Id,
                    ["event_id"] = delivery.EventId,
                    ["event_type"] = delivery.EventType,
                    ["previous_error"] = previousError,
                }));

            queued = delivery;
        }, cancellationToken);

        return WebhookDeliveryDto.From(queued!);
    }
}
