using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Queueing;
using HappyPaws.Api.Http;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Application.Queueing.GetQueueBoard;
using HappyPaws.Application.Queueing.IssueWalkInTicket;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The counter's view of the queue — "คิวรีเซ็ตรายวันต่อสาขา".
/// </summary>
[ApiController]
[Route("staff/queue")]
public sealed class QueueController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/queue?branch=&amp;room=</c> — "คิวทั้งหมดวันนี้พร้อมสถานะแต่ละใบ".
    /// <para>
    /// Not paginated, unlike every other list here: a day at one branch is tens of tickets, the
    /// board is read whole by a waiting-room screen, and paging would make the position counts
    /// meaningless.
    /// </para>
    /// </summary>
    /// <param name="branch">Branch id; may be omitted at a single-site clinic.</param>
    /// <param name="room">Narrows the board to one consulting room.</param>
    /// <param name="date">Clinic-local day. Defaults to today in clinic time.</param>
    /// <param name="includeFinished">Adds served and departed tickets to the day's view.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Queue.Read)]
    [ProducesResponseType<QueueBoardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<QueueBoardDto>> Board(
        [FromQuery] string? branch,
        [FromQuery] string? room,
        [FromQuery] DateOnly? date,
        [FromQuery(Name = "include_finished")] bool includeFinished,
        [FromServices] GetQueueBoardHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(
            new GetQueueBoardQuery(branch, room, date, includeFinished), cancellationToken));

    /// <summary>
    /// <c>POST /staff/queue/tickets</c> — "ออกบัตรคิวให้ walk-in ที่ยังไม่มีนัด".
    /// Idempotent, because the contract requires the header on every POST that creates a queue.
    /// </summary>
    /// <param name="request">
    /// The pet, the service, and optionally the branch, the vet and a priority. <c>priority</c> is
    /// a contract extension — an emergency arriving at the counter has to be expressible at the
    /// moment the ticket is printed, not only afterwards. See
    /// <c>docs/contract-extensions.md § 1</c>.
    /// </param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("tickets")]
    [Idempotent]
    [RequireScope(Scopes.Queue.Write)]
    [ProducesResponseType<QueueTicketDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<QueueTicketDto>> IssueTicket(
        [FromBody] IssueWalkInTicketRequest request,
        [FromServices] IssueWalkInTicketHandler handler,
        CancellationToken cancellationToken)
    {
        var ticket = await handler.HandleAsync(
            new IssueWalkInTicketCommand(
                request.PetId,
                request.ServiceId,
                request.BranchId,
                request.StaffId,
                request.Priority,
                request.PriorityReason),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(ticket.Version);

        return Created($"/staff/queue/{ticket.Id}", ticket);
    }
}
