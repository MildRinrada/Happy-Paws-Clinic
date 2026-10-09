using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Integrations;
using HappyPaws.Api.Http;
using HappyPaws.Application.Integrations.ConfigurePaymentProvider;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The payment provider settings. Stored only: there is no gateway connector.</summary>
[ApiController]
[Route("staff/integrations/payment")]
public sealed class PaymentIntegrationController : ControllerBase
{
    /// <summary>
    /// <c>PUT /staff/integrations/payment</c> — provider, mode, public key and the write-only
    /// secret key and webhook secret. Absent fields are left alone; an empty body reads the settings
    /// back, because the contract has no <c>GET</c>. Secrets are never returned, only
    /// <c>has_secret_key</c> and <c>has_webhook_secret</c>. <c>If-Match</c> is honoured when sent.
    /// <c>integration:manage</c> (<c>docs/assumptions.md § 2</c>).
    /// </summary>
    /// <param name="request">The fields to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<PaymentSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PaymentSettingsDto>> Configure(
        [FromBody] ConfigurePaymentRequest request,
        [FromServices] ConfigurePaymentProviderHandler handler,
        CancellationToken cancellationToken)
    {
        var settings = await handler.HandleAsync(request.ToCommand(IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(settings.Version);

        return Ok(settings);
    }
}
