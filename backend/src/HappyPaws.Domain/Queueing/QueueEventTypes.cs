namespace HappyPaws.Domain.Queueing;

/// <summary>
/// The event names carried on <c>SSE /queue/stream?branch=</c>, taken verbatim from the contract:
/// "event: ticket.called · ticket.updated · board.refresh".
/// <para>
/// Named here rather than typed as string literals at each publish site, because these are wire
/// values a browser switches on — a typo in one publisher would silently produce an event no
/// client listens for.
/// </para>
/// </summary>
public static class QueueEventTypes
{
    /// <summary>A ticket has just been called forward. The event a waiting-room screen reacts to.</summary>
    public const string TicketCalled = "ticket.called";

    /// <summary>Any other change to a ticket — issued, assigned, skipped, moved room.</summary>
    public const string TicketUpdated = "ticket.updated";

    /// <summary>
    /// The whole board should be re-fetched. Sent when a change is too broad to describe as one
    /// ticket, and as the reconnect hint so a client that missed events does not have to guess.
    /// </summary>
    public const string BoardRefresh = "board.refresh";
}
