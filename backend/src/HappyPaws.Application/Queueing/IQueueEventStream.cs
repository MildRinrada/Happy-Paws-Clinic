namespace HappyPaws.Application.Queueing;

/// <summary>One message on <c>SSE /queue/stream?branch=</c>.</summary>
/// <param name="Type">One of <see cref="Domain.Queueing.QueueEventTypes"/>.</param>
/// <param name="BranchId">Subscribers are filtered by branch; a screen at one site sees one board.</param>
/// <param name="TicketId">The ticket this concerns, or null for a board-wide event.</param>
/// <param name="CustomerId">
/// Who the ticket belongs to. Carried so the stream can be filtered per audience: staff see the
/// whole board, a customer sees only their own ticket.
/// </param>
public sealed record QueueEvent(string Type, string BranchId, string? TicketId, string? CustomerId);

/// <summary>
/// Publishes and subscribes to the live queue.
/// <para>
/// The port is deliberately narrow, and the implementation is deliberately in-memory: one clinic is
/// one deployment, so a single process holds every subscriber and there is nothing to coordinate.
/// Reaching for Redis or a broker here would add an operational dependency to solve a problem this
/// architecture does not have — see <c>docs/architecture.md § 6</c>.
/// </para>
/// <para>
/// The consequence, stated so it is not discovered later: this does not survive running two API
/// instances. A second replica's subscribers would never see the first's events. Scaling out means
/// adding a backplane behind this same port, not rewriting the callers.
/// </para>
/// </summary>
public interface IQueueEventStream
{
    /// <summary>
    /// Announces a change. Never throws and never blocks the caller: a stalled or vanished
    /// subscriber must not be able to fail the clinical operation that produced the event.
    /// </summary>
    void Publish(QueueEvent queueEvent);

    /// <summary>
    /// Events for one branch until the caller disconnects. Completes when
    /// <paramref name="cancellationToken"/> fires.
    /// </summary>
    IAsyncEnumerable<QueueEvent> SubscribeAsync(string branchId, CancellationToken cancellationToken);
}
