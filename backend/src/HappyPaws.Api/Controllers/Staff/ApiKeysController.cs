using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Integrations;
using HappyPaws.Application.Integrations.CreateApiKey;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.ListApiKeys;
using HappyPaws.Application.Integrations.RevokeApiKey;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Keys for machine clients: the contract's create, and the list and revoke it does not have
/// (<c>docs/contract-extensions.md § 22</c>), without which a leaked key stayed valid until it expired.
/// </summary>
[ApiController]
[Route("staff/api-keys")]
public sealed class ApiKeysController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/api-keys</c> — the keys the caller could have made, newest first: prefix,
    /// name, scopes, branches, <c>status</c> (<c>active</c>, <c>expired</c>, <c>revoked</c>) and
    /// when it was made, expires, was last used and was revoked. Never the key. Not in the API
    /// reference; see <c>docs/contract-extensions.md § 22</c>. <c>integration:manage</c>.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<ListResponse<ApiKeyDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ListResponse<ApiKeyDto>> List(
        [FromServices] ListApiKeysHandler handler,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(cancellationToken));

    /// <summary>
    /// <c>DELETE /staff/api-keys/{id}</c> — revokes the key; its next request is <c>401</c>.
    /// <c>204</c>, also when it was already revoked. Not in the API reference; see
    /// <c>docs/contract-extensions.md § 22</c>. <c>integration:manage</c>.
    /// </summary>
    /// <param name="id">The key, <c>key_…</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("{id}")]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        string id,
        [FromServices] RevokeApiKeyHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new RevokeApiKeyCommand(id), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// <c>POST /staff/api-keys</c> — issues an API key. <c>201</c> with the whole <c>key</c>, shown
    /// this once; only its hash is stored. The key is sent as <c>Authorization: Bearer hp_…</c> and
    /// acts with exactly its scopes, within its branches, until <c>expires_at</c> (90 days by
    /// default, 365 at most). Scopes must be ones the caller holds (<c>403 insufficient_scope</c>)
    /// and ones a key may carry (<c>422</c>: no 2FA-level scope, no <c>integration:manage</c> or
    /// <c>staff:manage</c>, no wildcard). <c>integration:manage</c> (<c>docs/assumptions.md § 2</c>).
    /// </summary>
    /// <param name="request">The name, the scopes, the branches and the expiry.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<CreatedApiKeyDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateApiKeyRequest request,
        [FromServices] CreateApiKeyHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(request.ToCommand(), cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return StatusCode(StatusCodes.Status201Created, created);
    }
}
