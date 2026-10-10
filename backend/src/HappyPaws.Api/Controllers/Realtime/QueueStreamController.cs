using System.Text.Json;
using HappyPaws.Api.Authorization;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Queueing;
using HappyPaws.Application.Queueing.Displays;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Realtime;

/// <summary>
/// <c>SSE /queue/stream?branch=</c> — "event: ticket.called · ticket.updated · board.refresh".
/// <para>
/// The only non-REST surface in the contract, and available to both audiences. Like the shared
/// <c>/appointments/{id}</c> routes it carries no <c>[RequireScope]</c>: staff are admitted by
/// <c>queue:read</c> within their branch, and a customer — who holds only <c>portal</c> — is
/// admitted to a filtered view of their own tickets.
/// </para>
/// <para>
/// A third audience is a contract extension (<c>docs/contract-extensions.md § 30</c>): a
/// waiting-room screen's display token, for its own branch only. It gets every event of the branch
/// with the <c>ticket_id</c> left out, since the display board it re-reads carries no ticket ids,
/// and it is re-checked at every heartbeat, so a revoked screen's stream closes within 20 seconds
/// rather than when the connection happens to drop.
/// </para>
/// <para>
/// <b>Authentication is an ordinary bearer token.</b> A browser's <c>EventSource</c> cannot set
/// headers, so the web client connects to its own Next.js server, which holds the session cookie
/// and attaches the token — the same path every other authenticated call already takes. The
/// alternative, a token in the query string, would write credentials into every access log and
/// proxy cache along the way. See <c>docs/assumptions.md § 11</c>.
/// </para>
/// </summary>
[ApiController]
[Route("queue/stream")]
public sealed class QueueStreamController : ControllerBase
{
    /// <summary>Keeps idle connections alive through proxies that cut silent sockets.</summary>
    private static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(20);

    /// <summary>Opens the stream. Runs until the client disconnects.</summary>
    /// <param name="branch">Branch to watch. Required — a stream is one site's board.</param>
    /// <param name="stream">The event source.</param>
    /// <param name="currentUser">The authenticated caller.</param>
    /// <param name="branchGuard">Branch restriction for staff callers.</param>
    /// <param name="displays">Re-checks a display token while its stream is open.</param>
    /// <param name="clock">The time those re-checks are made at.</param>
    /// <param name="cancellationToken">Fires when the client goes away.</param>
    [HttpGet]
    [AcceptsDisplayToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task Stream(
        [FromQuery] string branch,
        [FromServices] IQueueEventStream stream,
        [FromServices] ICurrentUser currentUser,
        [FromServices] BranchGuard branchGuard,
        [FromServices] IQueueDisplayRepository displays,
        [FromServices] IClock clock,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(branch))
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["branch"] = ["ต้องระบุสาขา"] });
        }

        var audience = ResolveAudience(currentUser, branchGuard, branch);

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        // nginx buffers proxied responses by default, which would hold events until the buffer
        // filled — for a queue board that means the screen updates in bursts, minutes late.
        Response.Headers["X-Accel-Buffering"] = "no";

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        // Sent immediately so a reconnecting client re-reads the board rather than assuming the
        // state it had when the connection dropped is still current.
        await WriteEventAsync(Domain.Queueing.QueueEventTypes.BoardRefresh, new { branch_id = branch }, cancellationToken);

        using var heartbeat = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, heartbeat.Token);

        Func<CancellationToken, Task<bool>>? stillAllowed = audience.DisplayId is { } displayId
            ? token => displays.IsUsableAsync(displayId, clock.UtcNow, token)
            : null;

        var beating = SendHeartbeatsAsync(stillAllowed, heartbeat, linked.Token);

        try
        {
            await foreach (var queueEvent in stream.SubscribeAsync(branch, linked.Token))
            {
                if (audience.CustomerId is { } customerId && queueEvent.CustomerId != customerId)
                {
                    // A customer watching their own ticket must not learn that someone else's was
                    // just called — the event id alone would leak the clinic's throughput.
                    continue;
                }

                object payload = audience.DisplayId is not null
                    ? new { branch_id = queueEvent.BranchId }
                    : new { ticket_id = queueEvent.TicketId, branch_id = queueEvent.BranchId };

                await WriteEventAsync(queueEvent.Type, payload, linked.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // The client hung up, or a display was revoked. Not an error.
        }
        finally
        {
            await heartbeat.CancelAsync();
            await beating;
        }
    }

    /// <summary>Who is listening: staff (whole branch), one customer, or one waiting-room screen.</summary>
    private sealed record Audience(string? CustomerId = null, string? DisplayId = null);

    /// <summary>Decides the audience, or throws for anyone who is none of the three.</summary>
    private static Audience ResolveAudience(ICurrentUser currentUser, BranchGuard branchGuard, string branchId)
    {
        if (currentUser.DisplayId is { } displayId)
        {
            // The token is bound to one branch; any other is refused, not quietly swapped.
            return currentUser.BranchIds.Count == 1 && currentUser.BranchIds.Contains(branchId)
                ? new Audience(DisplayId: displayId)
                : throw new ForbiddenException($"display is not bound to branch {branchId}");
        }

        if (currentUser.StaffId is { Length: > 0 } && currentUser.Scopes.Allows(Scopes.Queue.Read))
        {
            branchGuard.EnsureCanAccess(branchId);
            return new Audience();
        }

        return currentUser.CustomerId is { Length: > 0 } customerId
            ? new Audience(CustomerId: customerId)
            : throw new ForbiddenException("caller may not watch the queue stream");
    }

    /// <summary>
    /// A comment every 20 seconds. For a display, the token is re-read first; once it no longer
    /// works the stream is ended by cancelling <paramref name="end"/>.
    /// </summary>
    private async Task SendHeartbeatsAsync(
        Func<CancellationToken, Task<bool>>? stillAllowed,
        CancellationTokenSource end,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(Heartbeat);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (stillAllowed is not null && !await stillAllowed(cancellationToken))
                {
                    await end.CancelAsync();
                    return;
                }

                // An SSE comment: ignored by every client, but it is traffic, which is what keeps
                // the socket from being reaped.
                await Response.WriteAsync(": ping\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task WriteEventAsync(string type, object payload, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {type}\n", cancellationToken);
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
