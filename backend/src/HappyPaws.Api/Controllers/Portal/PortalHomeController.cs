using HappyPaws.Api.Authorization;
using HappyPaws.Application.Patients.Portal;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>The portal's first page.</summary>
[ApiController]
[Route("portal/home")]
[RequireScope(Scopes.Portal)]
public sealed class PortalHomeController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/home</c> — "สรุปหน้าแรก: สัตว์ นัดถัดไป คิว แจ้งเตือน", plus vaccines falling due.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<PortalHomeDto>(StatusCodes.Status200OK)]
    public Task<PortalHomeDto> Get([FromServices] GetPortalHomeHandler handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(cancellationToken);
}
