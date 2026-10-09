using HappyPaws.Api.Authorization;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.ListWebhookDeliveries;
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
}
