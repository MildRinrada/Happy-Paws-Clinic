using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Queueing;
using HappyPaws.Api.Http;
using HappyPaws.Application.Queueing.AssignTicket;
using HappyPaws.Application.Queueing.CallTicket;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Application.Queueing.LeaveQueue;
using HappyPaws.Application.Queueing.SetTicketPriority;
using HappyPaws.Application.Queueing.SkipTicket;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The actions on one ticket. Note the split the contract draws: assigning a vet or a room is
/// <c>queue:write</c>, while calling and skipping are <c>queue:call</c> — a receptionist organises
/// the board, and calling a patient forward is a separate permission a vet or an assistant holds.
/// <para>
/// The last two routes, <c>priority</c> and <c>leave</c>, are <b>not in the API reference</b>. Both
/// close gaps found by building against it: the contract's queue cannot express urgency, and its
/// state machine has a <c>left</c> state no endpoint can reach. Both are marked as extensions on
/// the actions themselves and are documented in <c>docs/contract-extensions.md</c>.
/// </para>
/// </summary>
[ApiController]
[Route("staff/queue/{id}")]
public sealed class QueueTicketActionsController : ControllerBase
{
    /// <summary><c>POST /staff/queue/{id}/assign</c> — "จ่ายหมอและห้องตรวจ · ย้ายคิวข้ามห้องได้".</summary>
    /// <param name="id">Queue ticket id.</param>
    /// <param name="request">The vet, the room, or both.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("assign")]
    [RequireScope(Scopes.Queue.Write)]
    [ProducesResponseType<QueueTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QueueTicketDto>> Assign(
        string id,
        [FromBody] AssignTicketRequest request,
        [FromServices] AssignTicketHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new AssignTicketCommand(id, request.StaffId, request.RoomId, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken));

    /// <summary><c>POST /staff/queue/{id}/call</c> — "เรียกคิว · ยิงแจ้งเตือน LINE ให้เจ้าของ".</summary>
    /// <param name="id">Queue ticket id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("call")]
    [RequireScope(Scopes.Queue.Call)]
    [ProducesResponseType<QueueTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QueueTicketDto>> Call(
        string id,
        [FromServices] CallTicketHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new CallTicketCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken));

    /// <summary><c>POST /staff/queue/{id}/skip</c> — "ข้ามคิวที่ยังไม่มา · เลื่อนไปท้ายแถว".</summary>
    /// <param name="id">Queue ticket id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("skip")]
    [RequireScope(Scopes.Queue.Call)]
    [ProducesResponseType<QueueTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QueueTicketDto>> Skip(
        string id,
        [FromServices] SkipTicketHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new SkipTicketCommand(id, IfMatch.Parse(Request.Headers.IfMatch)), cancellationToken));

    /// <summary>
    /// <c>POST /staff/queue/{id}/priority</c> — raise or lower a ticket's urgency.
    /// <para>
    /// <b>Not in the API reference.</b> The contract's queue is strictly first-come with a skip,
    /// which leaves an emergency no way past a routine visit. See
    /// <c>docs/contract-extensions.md § 1</c>. Scoped as <c>queue:write</c> — organising the board
    /// is the receptionist's job, the same permission that assigns a room.
    /// </para>
    /// </summary>
    /// <param name="id">Queue ticket id.</param>
    /// <param name="request">The new priority, and why.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("priority")]
    [RequireScope(Scopes.Queue.Write)]
    [ProducesResponseType<QueueTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QueueTicketDto>> Priority(
        string id,
        [FromBody] SetPriorityRequest request,
        [FromServices] SetTicketPriorityHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new SetTicketPriorityCommand(id, request.Priority, request.Reason, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken));

    /// <summary>
    /// <c>POST /staff/queue/{id}/leave</c> — the owner gave up waiting and went home.
    /// <para>
    /// <b>Not in the API reference.</b> <c>left</c> appears in the contract's queue state machine
    /// with no endpoint that reaches it. See <c>docs/contract-extensions.md § 2</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Queue ticket id.</param>
    /// <param name="request">Why they left, if the counter knows.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("leave")]
    [RequireScope(Scopes.Queue.Write)]
    [ProducesResponseType<QueueTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QueueTicketDto>> Leave(
        string id,
        [FromBody] LeaveQueueRequest request,
        [FromServices] LeaveQueueHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new LeaveQueueCommand(id, request.Reason, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken));

    private ActionResult<QueueTicketDto> WithETag(QueueTicketDto ticket)
    {
        Response.Headers.ETag = IfMatch.ToETag(ticket.Version);
        return Ok(ticket);
    }
}
