using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing;

/// <summary>
/// Persistence port for queue tickets.
/// <para>
/// The queue is deliberately <b>not</b> paginated. A day at one branch is tens of tickets, the
/// board is read as a whole by a waiting-room screen, and a cursor would make "how many ahead of
/// me" unanswerable without walking pages. Every other list in this system is cursor-paged; this
/// one is bounded by the clinic's own day.
/// </para>
/// </summary>
public interface IQueueTicketRepository
{
    void Add(QueueTicket ticket);

    Task<QueueTicket?> FindByIdAsync(string ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The ticket already issued for an appointment, if any. Check-in is idempotent at the HTTP
    /// layer, but a retry that outlived its idempotency record must still not issue a second
    /// number for the same visit.
    /// </summary>
    Task<QueueTicket?> FindByAppointmentAsync(string appointmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The same lookup, tracked for modification — opening a consultation moves the appointment's
    /// ticket into the room. Kept separate from <see cref="FindByAppointmentAsync"/> so the
    /// read-only callers stay read-only rather than silently acquiring a change-tracked entity.
    /// </summary>
    Task<QueueTicket?> FindTrackedByAppointmentAsync(
        string appointmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /staff/queue?branch=&amp;room=</c> — "คิวทั้งหมดวันนี้พร้อมสถานะแต่ละใบ", in call order.
    /// <para>
    /// <paramref name="includeFinished"/> false returns only tickets still on the board; the
    /// counter's default view. True includes served and departed tickets for the day's record.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<QueueTicket>> BoardAsync(
        string branchId,
        DateOnly serviceDate,
        string? roomId = null,
        bool includeFinished = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// How many tickets are still waiting to be called ahead of this one — the portal's
    /// "เหลืออีกกี่คิว".
    /// </summary>
    Task<int> CountAheadOfAsync(QueueTicket ticket, CancellationToken cancellationToken = default);

    /// <summary>A customer's tickets still on today's board, for the portal home.</summary>
    Task<IReadOnlyList<QueueTicket>> LiveForCustomerAsync(
        string customerId,
        DateOnly serviceDate,
        CancellationToken cancellationToken = default);
}
