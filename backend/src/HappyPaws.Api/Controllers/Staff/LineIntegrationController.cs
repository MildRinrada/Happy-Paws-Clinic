using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Notifications;
using HappyPaws.Api.Http;
using HappyPaws.Application.Integrations.ConfigureLineChannel;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The clinic's LINE Official Account settings.</summary>
[ApiController]
[Route("staff/integrations/line")]
public sealed class LineIntegrationController : ControllerBase
{
    /// <summary>
    /// <c>PUT /staff/integrations/line</c> — "channel id/secret · เปิดปิดชนิดการแจ้งเตือน".
    /// <para>
    /// Absent fields are left alone and an empty body reads the settings back, because the contract
    /// has no <c>GET</c>. The answer never carries the secret or the token, only whether each is set
    /// and where it comes from. <c>If-Match</c> is honoured when sent.
    /// </para>
    /// </summary>
    /// <param name="request">The fields to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPut]
    [RequireScope(Scopes.Integration.Manage)]
    [ProducesResponseType<LineSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LineSettingsDto>> Configure(
        [FromBody] ConfigureLineRequest request,
        [FromServices] ConfigureLineChannelHandler handler,
        CancellationToken cancellationToken)
    {
        var settings = await handler.HandleAsync(
            new ConfigureLineChannelCommand(
                request.ChannelId,
                request.ChannelSecret,
                request.ChannelAccessToken,
                request.Enabled,
                request.NotificationTypes,
                request.BroadcastMonthlyLimit,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(settings.Version);

        return Ok(settings);
    }
}
