using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Crm;
using HappyPaws.Application.Crm.CreateSegment;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>Saved customer groups.</summary>
[ApiController]
[Route("staff/segments")]
public sealed class SegmentsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/segments</c> — "กลุ่มลูกค้าจากเงื่อนไข เช่น ไม่มา 6 เดือน". <c>201</c> with the
    /// group and how many customers match it now, and how many of those a LINE broadcast could reach.
    /// The id is what <c>POST /staff/integrations/line/broadcast</c> takes as <c>segment_id</c>.
    /// </summary>
    /// <param name="request">The name and the conditions.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Crm.Manage)]
    [ProducesResponseType<SegmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSegmentRequest request,
        [FromServices] CreateSegmentHandler handler,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await handler.HandleAsync(request.ToCommand(), cancellationToken));
}
