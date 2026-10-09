using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.Webhooks;

namespace HappyPaws.Application.Integrations.ListWebhookDeliveries;

/// <summary>
/// <c>GET /staff/webhooks/{id}/deliveries</c> — every event sent to one webhook, newest first, with
/// the status code, a short error and the start of the receiver's answer from the latest attempt.
/// An unknown webhook is <c>404</c>.
/// </summary>
public sealed class ListWebhookDeliveriesHandler(IWebhookRepository webhooks)
{
    public async Task<CursorPage<WebhookDeliveryDto>> HandleAsync(
        string subscriptionId,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        _ = await webhooks.FindAsync(subscriptionId, cancellationToken)
            ?? throw new NotFoundException("webhook", subscriptionId);

        var deliveries = await webhooks.ListDeliveriesAsync(subscriptionId, page, cancellationToken);

        return new CursorPage<WebhookDeliveryDto>(
            [.. deliveries.Data.Select(WebhookDeliveryDto.From)],
            deliveries.NextCursor);
    }
}
