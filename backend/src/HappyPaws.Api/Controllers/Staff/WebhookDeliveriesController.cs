using HappyPaws.Api.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.ListWebhookDeliveries;
using HappyPaws.Application.Integrations.RedeliverWebhookDelivery;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The delivery log of one webhook.</summary>
[ApiController]
[Route("staff/webhooks/{id}/deliveries")]
public sealed class WebhookDeliveriesController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/webhooks/{id}/deliveries</c> — each event sent to this webhook, newest first,
    /// with its status (<c>pending</c>, <c>succeeded</c>, <c>failed</c>), attempts, and the status
    /// code, error and first 1,000 characters of the receiver's answer from the latest attempt.
    /// Cursor-paged. <c>integration:manage</c>, as for creating one.
    /// </summary>
    /// <param name="id">The webhook, <c>whk_…</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="limit">Page size, 1 to 100.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<CursorPage<WebhookDeliveryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CursorPage<WebhookDeliveryDto>> List(
        string id,
        [FromServices] ListWebhookDeliveriesHandler handler,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            id,
            new CursorPageRequest(limit is 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

    /// <summary>
    /// <c>POST /staff/webhooks/{id}/deliveries/{deliveryId}/redeliver</c> — queues a <c>failed</c>
    /// delivery to be sent again, now, with the same event id and body. <c>202</c> with the
    /// delivery, back to <c>pending</c>. Anything not <c>failed</c>, or a webhook that is switched
    /// off, is <c>409</c>. Not in the API reference; see <c>docs/contract-extensions.md § 22</c>.
    /// </summary>
    /// <param name="id">The webhook, <c>whk_…</c>.</param>
    /// <param name="deliveryId">The delivery, <c>whd_…</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{deliveryId}/redeliver")]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<WebhookDeliveryDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Redeliver(
        string id,
        string deliveryId,
        [FromServices] RedeliverWebhookDeliveryHandler handler,
        CancellationToken cancellationToken)
    {
        var delivery = await handler.HandleAsync(new RedeliverWebhookDeliveryCommand(id, deliveryId), cancellationToken);

        return StatusCode(StatusCodes.Status202Accepted, delivery);
    }
}
