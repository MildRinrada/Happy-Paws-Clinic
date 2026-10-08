using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Application.Notifications.ListTemplates;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>The message templates.</summary>
[ApiController]
[Route("staff/notifications/templates")]
public sealed class NotificationTemplatesController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/notifications/templates</c> — "เทมเพลตพร้อมตัวแปร pet_name, vet_name, time".
    /// Short and fixed, so returned whole in <c>{ "data": [...] }</c>.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Notify.Manage)]
    [ProducesResponseType<ListResponse<TemplateDto>>(StatusCodes.Status200OK)]
    public async Task<ListResponse<TemplateDto>> List(
        [FromServices] ListTemplatesHandler handler,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(cancellationToken));
}
