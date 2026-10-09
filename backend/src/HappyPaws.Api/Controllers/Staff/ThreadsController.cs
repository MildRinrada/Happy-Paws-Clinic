using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Messaging;
using HappyPaws.Api.Extensions;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Messaging.Dtos;
using HappyPaws.Application.Messaging.ListThreads;
using HappyPaws.Application.Messaging.ReplyToThread;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The customers' conversations with the clinic: "กล่องข้อความรวมทุกช่องทางต่อลูกค้าหนึ่งคน". A
/// thread starts when a customer writes (today: a LINE message from an address on their record);
/// staff answer it here. See <c>docs/features/messaging.md</c>.
/// </summary>
[ApiController]
[Route("staff/threads")]
public sealed class ThreadsController : ControllerBase
{
    /// <summary>
    /// <c>GET /staff/threads</c> — one entry per customer, those awaiting a reply first, then the most
    /// recently active. A branch-bound reader sees their branches' threads and those with no branch.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="status"><c>awaiting_reply</c> or <c>replied</c>.</param>
    /// <param name="customerId">One customer's thread.</param>
    /// <param name="threadId">One thread, to open it.</param>
    /// <param name="messages">How many of each thread's newest messages to include, 0 to 100. Default 20.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Message.Read)]
    [ProducesResponseType<CursorPage<ThreadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<CursorPage<ThreadDto>> List(
        [FromServices] ListThreadsHandler handler,
        [FromQuery] string? status,
        [FromQuery(Name = "customer_id")] string? customerId,
        [FromQuery(Name = "thread_id")] string? threadId,
        [FromQuery] int? messages,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListThreadsQuery(
                status,
                customerId,
                threadId,
                messages,
                new CursorPageRequest(limit is 0 ? CursorPaging.DefaultLimit : limit, cursor)),
            cancellationToken);

    /// <summary>
    /// <c>POST /staff/threads/{id}/messages</c> — "ตอบกลับ · ระบบเลือกช่องทางที่ลูกค้าใช้ล่าสุด".
    /// <para>
    /// <c>201</c> with the stored reply and what became of it (<c>delivery</c>): the channel the
    /// customer last wrote on is tried first, then LINE, SMS and email, through the consent gate.
    /// A reply that reached no one is still <c>201</c>, with <c>delivery.status</c> <c>skipped</c>
    /// or <c>failed</c>: the counter needs to see it, it is not an error. Under the contract's
    /// messaging rate limit.
    /// </para>
    /// </summary>
    /// <param name="id">Thread id.</param>
    /// <param name="request">The words.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/messages")]
    [RequireScope(Scopes.Message.Write)]
    [EnableRateLimiting(RateLimitingExtensions.MessagingPolicy)]
    [ProducesResponseType<ThreadMessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reply(
        string id,
        [FromBody] ReplyToThreadRequest request,
        [FromServices] ReplyToThreadHandler handler,
        CancellationToken cancellationToken)
    {
        var message = await handler.HandleAsync(new ReplyToThreadCommand(id, request.Text), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, message);
    }
}
