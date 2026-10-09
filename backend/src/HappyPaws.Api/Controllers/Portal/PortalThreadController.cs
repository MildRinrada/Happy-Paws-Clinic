using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Messaging;
using HappyPaws.Api.Extensions;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Messaging.Dtos;
using HappyPaws.Application.Messaging.Portal;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// The customer's own conversation with the clinic, from the portal. Not in the API reference: see
/// <c>docs/contract-extensions.md § 24</c>. The same thread staff read at <c>/staff/threads</c>;
/// the customer id comes from the token only.
/// </summary>
[ApiController]
[Route("portal/thread")]
[RequireScope(Scopes.Portal)]
public sealed class PortalThreadController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/thread</c> — the caller's thread and a page of its messages, newest first:
    /// <c>{ "id", "status", "data": [...], "page": { "next_cursor" } }</c>. No thread yet answers
    /// with no <c>id</c> and an empty <c>data</c>; nothing is created by reading.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page: older messages.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Get(
        [FromServices] GetMyThreadHandler handler,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var thread = await handler.HandleAsync(
            new GetMyThreadQuery(limit is 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        // The shared list envelope, with the thread's id and state beside the page.
        return Ok(new
        {
            id = thread.Id,
            status = thread.Status,
            data = thread.Data,
            page = new { next_cursor = thread.NextCursor },
        });
    }

    /// <summary>
    /// <c>POST /portal/thread/messages</c> — the customer writes to the clinic, on the channel
    /// <c>portal</c>. The first message opens the thread. <c>201</c> with the stored message. Under
    /// the contract's messaging rate limit, like the staff reply.
    /// </summary>
    /// <param name="request">The words.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("messages")]
    [EnableRateLimiting(RateLimitingExtensions.MessagingPolicy)]
    [ProducesResponseType<PortalThreadMessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Write(
        [FromBody] WriteToClinicRequest request,
        [FromServices] WriteToClinicHandler handler,
        CancellationToken cancellationToken)
    {
        var message = await handler.HandleAsync(new WriteToClinicCommand(request.Text), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, message);
    }
}
