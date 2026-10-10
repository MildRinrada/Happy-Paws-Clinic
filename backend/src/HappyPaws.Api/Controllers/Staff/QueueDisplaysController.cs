using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts;
using HappyPaws.Api.Contracts.Queueing;
using HappyPaws.Application.Queueing.Displays;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Waiting-room screens: make one, see them all, stop one. <b>Not in the API reference</b>: see
/// <c>docs/contract-extensions.md § 30</c>.
/// <para>
/// Before these, the TV in the waiting room had to be signed in with a staff account, and anyone at
/// its keyboard had that account's console. A screen made here holds a token that opens the display
/// board and its branch's queue stream, and nothing else. All three routes are <c>queue:write</c>,
/// the scope that already organises the board, within the caller's branches.
/// </para>
/// </summary>
[ApiController]
[Route("staff/queue/displays")]
public sealed class QueueDisplaysController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/queue/displays?branch=</c> — the screens at the caller's branches, newest first,
    /// with <c>status</c> (<c>pending_setup</c>, <c>setup_expired</c>, <c>active</c>, <c>expired</c>,
    /// <c>revoked</c>) and when each was set up and last seen. Never a token or a code.
    /// </summary>
    /// <param name="branch">One branch; the caller's own branches when omitted.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Queue.Write)]
    [ProducesResponseType<ListResponse<QueueDisplayDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ListResponse<QueueDisplayDto>> List(
        [FromQuery] string? branch,
        [FromServices] ListQueueDisplaysHandler handler,
        CancellationToken cancellationToken) =>
        ListResponse.Of(await handler.HandleAsync(branch, cancellationToken));

    /// <summary>
    /// <c>POST /staff/queue/displays</c> — a screen for one branch. <c>201</c> with a
    /// <c>setup_code</c> (<c>ABCD-EFGH</c>) shown this once and good for 30 minutes and one use:
    /// typed on the TV, or followed as a link, it is traded for the screen's token at
    /// <c>POST /queue/displays/claim</c>. Only hashes are stored. <c>Cache-Control: no-store</c>.
    /// </summary>
    /// <param name="request">The name, the branch, and optionally an end.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Queue.Write)]
    [ProducesResponseType<CreatedQueueDisplayDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateQueueDisplayRequest request,
        [FromServices] CreateQueueDisplayHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(
            new CreateQueueDisplayCommand(request.Name ?? string.Empty, request.BranchId ?? string.Empty, request.ExpiresAt),
            cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>
    /// <c>DELETE /staff/queue/displays/{id}</c> — stops the screen: its next request is <c>401</c>
    /// and an open stream closes within 20 seconds. <c>204</c>, also when it was already revoked.
    /// </summary>
    /// <param name="id">The screen, <c>dsp_…</c>.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpDelete("{id}")]
    [RequireScope(Scopes.Queue.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        string id,
        [FromServices] RevokeQueueDisplayHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);

        return NoContent();
    }
}
