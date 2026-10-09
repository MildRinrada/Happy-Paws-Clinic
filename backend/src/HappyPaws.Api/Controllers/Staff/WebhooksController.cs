using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Integrations;
using HappyPaws.Application.Integrations.CreateWebhookSubscription;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Outbound webhooks.</summary>
[ApiController]
[Route("staff/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/webhooks</c> — creates an outbound webhook. <c>201</c> with the webhook and its
    /// signing <c>secret</c>, which is shown this once. The contract names no scope; this route
    /// takes <c>integration:manage</c> (<c>docs/assumptions.md § 2</c>). The URL must be https and
    /// not a private address; <c>422</c> otherwise.
    /// </summary>
    /// <param name="request">The URL, the events and a description.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<CreatedWebhookDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWebhookRequest request,
        [FromServices] CreateWebhookSubscriptionHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(request.ToCommand(), cancellationToken);

        // The secret is in this body; no cache anywhere may keep it.
        Response.Headers.CacheControl = "no-store";

        return StatusCode(StatusCodes.Status201Created, created);
    }
}
