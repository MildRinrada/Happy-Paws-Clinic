using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Integrations;
using HappyPaws.Application.Integrations.CreateApiKey;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Keys for machine clients.</summary>
[ApiController]
[Route("staff/api-keys")]
public sealed class ApiKeysController : ControllerBase
{
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
