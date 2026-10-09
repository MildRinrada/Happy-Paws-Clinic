using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Crm;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Crm.CreateSegment;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Crm.GetSegment;
using HappyPaws.Application.Crm.ListSegments;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Saved customer groups. Only <c>POST</c> is in the contract; the two reads are
/// <c>docs/contract-extensions.md § 25</c>.
/// </summary>
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

    /// <summary>
    /// <c>GET /staff/segments?branch=&amp;limit=&amp;cursor=</c> — the saved groups, newest first, with
    /// the counts taken when each was saved (<c>counted_at</c>). Branch-bound staff see only groups
    /// limited to their branches. Not in the API reference (§ 25).
    /// </summary>
    /// <param name="branch">Only this branch's groups.</param>
    /// <param name="limit">Page size, 1 to 100.</param>
    /// <param name="cursor">From the previous page.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Crm.Read)]
    [ProducesResponseType<CursorPageResponse<SegmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CursorPageResponse<SegmentDto>>> List(
        [FromQuery] string? branch,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        [FromServices] ListSegmentsHandler handler,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListSegmentsQuery(branch, limit == 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        return Ok(CursorPageResponse<SegmentDto>.From(page));
    }

    /// <summary>
    /// <c>GET /staff/segments/{id}</c> — one group, counted again now with the SQL a broadcast sends
    /// with. Not in the API reference (§ 25).
    /// </summary>
    /// <param name="id">The group.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [RequireScope(Scopes.Crm.Read)]
    [ProducesResponseType<SegmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SegmentDto> Get(
        string id,
        [FromServices] GetSegmentHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(id, cancellationToken);
}
