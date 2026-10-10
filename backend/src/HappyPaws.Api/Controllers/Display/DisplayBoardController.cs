using HappyPaws.Api.Authorization;
using HappyPaws.Application.Queueing.Displays;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Display;

/// <summary>
/// What a waiting-room screen reads. <b>Not in the API reference</b>: see
/// <c>docs/contract-extensions.md § 30</c>.
/// </summary>
[ApiController]
[Route("queue/display-board")]
public sealed class DisplayBoardController : ControllerBase
{
    /// <summary>
    /// <c>GET /queue/display-board?branch=</c> — today's live board for the screen's branch, with
    /// ticket numbers, statuses (<c>waiting</c>, <c>called</c>, <c>in_room</c>), rooms, vet names and
    /// call times, and nothing about the pet, the owner, the service or the priority. A display token
    /// only (<c>Authorization: Bearer hpdsp_…</c>); any other caller is <c>403</c>. <c>branch</c>
    /// may be omitted; another branch is <c>403</c>.
    /// </summary>
    /// <param name="branch">The screen's own branch, or omitted.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [AcceptsDisplayToken]
    [ProducesResponseType<DisplayBoardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DisplayBoardDto>> Board(
        [FromQuery] string? branch,
        [FromServices] GetDisplayBoardHandler handler,
        CancellationToken cancellationToken)
    {
        var board = await handler.HandleAsync(branch, cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return Ok(board);
    }
}
