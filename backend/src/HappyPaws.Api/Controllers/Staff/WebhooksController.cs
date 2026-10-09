using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Integrations;
using HappyPaws.Api.Http;
using HappyPaws.Application.Integrations.CreateWebhookSubscription;
using HappyPaws.Application.Integrations.DeleteWebhookSubscription;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.ListWebhooks;
using HappyPaws.Application.Integrations.RotateWebhookSecret;
using HappyPaws.Application.Integrations.UpdateWebhookSubscription;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Outbound webhooks.
/// <para>
/// The contract's create, and the list, edit, delete and secret rotation it does not have
/// (<c>docs/contract-extensions.md § 22</c>): without them a webhook could not be found again,
/// switched off, pointed elsewhere or given a new secret once made.
/// </para>
/// </summary>
[ApiController]
[Route("staff/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/webhooks</c> — every webhook that is not deleted, newest first, with its
    /// <c>version</c>. Never a secret. Not in the API reference; see
    /// <c>docs/contract-extensions.md § 22</c>. <c>integration:manage</c>.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<ListResponse<WebhookDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ListResponse<WebhookDto>> List(
        [FromServices] ListWebhooksHandler handler,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(cancellationToken));

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

    /// <summary>
    /// <c>PATCH /staff/webhooks/{id}</c> — change the URL (checked as on create), the events or the
    /// description, or switch it on or off. Switching off stops what is still queued for it. Not
    /// in the API reference; see <c>docs/contract-extensions.md § 22</c>. <c>If-Match</c> is honoured.
    /// </summary>
    /// <param name="id">The webhook, <c>whk_…</c>.</param>
    /// <param name="request">The fields to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}")]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<WebhookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<WebhookDto>> Update(
        string id,
        [FromBody] UpdateWebhookRequest request,
        [FromServices] UpdateWebhookSubscriptionHandler handler,
        CancellationToken cancellationToken)
    {
        var webhook = await handler.HandleAsync(request.ToCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(webhook.Version);

        return Ok(webhook);
    }

    /// <summary>
    /// <c>DELETE /staff/webhooks/{id}</c> — the webhook stops, leaves the list and loses its secret;
    /// its delivery log stays readable. Not in the API reference; see
    /// <c>docs/contract-extensions.md § 22</c>. <c>If-Match</c> is honoured.
    /// </summary>
    /// <param name="id">The webhook, <c>whk_…</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("{id}")]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        string id,
        [FromServices] DeleteWebhookSubscriptionHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteWebhookSubscriptionCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// <c>POST /staff/webhooks/{id}/rotate-secret</c> — a new signing secret, shown this once; the
    /// old one stops signing at once. Not in the API reference; see
    /// <c>docs/contract-extensions.md § 22</c>. <c>If-Match</c> is honoured.
    /// </summary>
    /// <param name="id">The webhook, <c>whk_…</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/rotate-secret")]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<RotatedWebhookSecretDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RotatedWebhookSecretDto>> RotateSecret(
        string id,
        [FromServices] RotateWebhookSecretHandler handler,
        CancellationToken cancellationToken)
    {
        var rotated = await handler.HandleAsync(new RotateWebhookSecretCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        Response.Headers.CacheControl = "no-store";
        Response.Headers.ETag = IfMatch.ToETag(rotated.Version);

        return Ok(rotated);
    }
}
