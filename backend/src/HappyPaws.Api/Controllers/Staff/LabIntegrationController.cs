using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Integrations;
using HappyPaws.Api.Http;
using HappyPaws.Application.Integrations.ConfigureLabProvider;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The outside lab settings. Stored only: there is no lab connector.</summary>
[ApiController]
[Route("staff/integrations/lab")]
public sealed class LabIntegrationController : ControllerBase
{
    /// <summary>
    /// <c>PUT /staff/integrations/lab</c> — lab, account id, API address and the write-only API key.
    /// Same rules as the payment settings: partial, empty body reads back, the key never returned,
    /// <c>If-Match</c> honoured. <c>integration:manage</c>.
    /// </summary>
    /// <param name="request">The fields to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<LabSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LabSettingsDto>> Configure(
        [FromBody] ConfigureLabRequest request,
        [FromServices] ConfigureLabProviderHandler handler,
        CancellationToken cancellationToken)
    {
        var settings = await handler.HandleAsync(request.ToCommand(IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(settings.Version);

        return Ok(settings);
    }
}
