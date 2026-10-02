using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Portal;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Notifications.ListMyNotifications;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>The customer's web inbox.</summary>
[ApiController]
[Route("portal/notifications")]
[RequireScope(Scopes.Portal)]
public sealed class PortalNotificationsController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/notifications</c> — "กล่องแจ้งเตือนในเว็บ". Newest first, with the unread count.
    /// Reading the list marks nothing; see <see cref="MarkRead"/>.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="unread">Only unread messages.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromServices] ListMyNotificationsHandler handler,
        [FromQuery] bool unread,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var page = await handler.HandleAsync(
            new ListMyNotificationsQuery(unread, limit is 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);

        // The shared list envelope, with the unread count beside the page.
        return Ok(new
        {
            data = page.Data,
            page = new { next_cursor = page.NextCursor },
            unread_count = page.UnreadCount,
        });
    }

    /// <summary>
    /// <c>POST /portal/notifications/read</c> — <b>contract extension</b>
    /// (<c>docs/contract-extensions.md § 13</c>), the "mark as read" of the contract's line. No ids
    /// marks everything read.
    /// </summary>
    /// <param name="request">The ids to mark, or none for all.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("read")]
    [ProducesResponseType<MarkedReadDto>(StatusCodes.Status200OK)]
    public Task<MarkedReadDto> MarkRead(
        [FromBody] MarkNotificationsReadRequest request,
        [FromServices] MarkNotificationsReadHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new MarkNotificationsReadCommand(request.Ids), cancellationToken);
}
