using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.Dtos;

/// <summary>
/// A queue ticket as the API returns it — <c>tkt_A014</c>.
/// <para>
/// Lives in the Queueing module rather than with the booking DTOs even though check-in is what
/// creates it: the ticket belongs to the queue, and the appointment result simply carries one.
/// </para>
/// <para>
/// The last five fields are a contract extension (<c>docs/contract-extensions.md § 31</c>): the
/// reasons and times the ticket already stores, for the counter's history. Staff responses only.
/// <see cref="ForCustomer"/> drops them on the one shared route that returns this shape to an owner
/// (checking in their own appointment), and the waiting-room screen never sees this shape. Absent
/// rather than null when unset, like every optional field in the API.
/// </para>
/// </summary>
public sealed record QueueTicketDto(
    string Id,
    uint Version,
    string Number,
    string BranchId,
    DateOnly ServiceDate,
    string Status,
    string Priority,
    string PetId,
    string CustomerId,
    string ServiceId,
    string? AppointmentId,
    string? StaffId,
    string? RoomId,
    int SkipCount,
    DateTimeOffset IssuedAt,
    DateTimeOffset? CalledAt,
    string? PriorityReason = null,
    DateTimeOffset? EnteredRoomAt = null,
    DateTimeOffset? ServedAt = null,
    DateTimeOffset? LeftAt = null,
    string? LeftReason = null)
{
    public static QueueTicketDto From(QueueTicket ticket) =>
        new(
            ticket.Id,
            ticket.Version,
            ticket.Number,
            ticket.BranchId,
            ticket.ServiceDate,
            ToWireStatus(ticket.Status),
            QueuePriorityParser.ToWire(ticket.Priority),
            ticket.PetId,
            ticket.CustomerId,
            ticket.ServiceId,
            ticket.AppointmentId,
            ticket.StaffId,
            ticket.RoomId,
            ticket.SkipCount,
            ticket.IssuedAt,
            ticket.CalledAt,
            ticket.PriorityReason,
            ticket.EnteredRoomAt,
            ticket.ServedAt,
            ticket.LeftAt,
            ticket.LeftReason);

    /// <summary>
    /// The same ticket for its owner: without the reasons and the room and service times, which
    /// are the counter's notes rather than the owner's (<c>docs/contract-extensions.md § 31</c>).
    /// </summary>
    public QueueTicketDto ForCustomer() =>
        this with { PriorityReason = null, EnteredRoomAt = null, ServedAt = null, LeftAt = null, LeftReason = null };

    /// <summary>
    /// <c>InRoom</c> → <c>in_room</c>. Spelled out rather than derived from the enum name, because
    /// <c>ToString().ToLower()</c> would silently produce <c>inroom</c> and the contract's state
    /// machine says <c>in_room</c>.
    /// </summary>
    public static string ToWireStatus(QueueTicketStatus status) => status switch
    {
        QueueTicketStatus.Waiting => "waiting",
        QueueTicketStatus.Called => "called",
        QueueTicketStatus.InRoom => "in_room",
        QueueTicketStatus.Served => "served",
        QueueTicketStatus.Skipped => "skipped",
        QueueTicketStatus.Left => "left",
        _ => status.ToString().ToLowerInvariant(),
    };
}

/// <summary>
/// <c>GET /staff/queue</c> — today's board for one branch, in the order tickets will be called.
/// </summary>
public sealed record QueueBoardDto(
    string BranchId,
    DateOnly ServiceDate,
    string? RoomId,
    int WaitingCount,
    IReadOnlyList<QueueTicketDto> Tickets);

/// <summary>
/// <c>GET /portal/queue/{id}</c> — "เลขคิวตัวเอง · กำลังเรียก · เหลืออีกกี่คิว".
/// <para>
/// Deliberately not the full ticket: an owner watching their place in line has no business seeing
/// which vet or room every other patient was assigned to.
/// </para>
/// <para>
/// <c>Ahead</c> is how many tickets are still waiting to be called before this one.
/// </para>
/// </summary>
public sealed record MyQueueTicketDto(
    string Id,
    string Number,
    string Status,
    string Priority,
    DateOnly ServiceDate,
    string BranchId,
    string? RoomId,
    int Ahead,
    DateTimeOffset? CalledAt)
{
    public static MyQueueTicketDto From(QueueTicket ticket, int ahead) =>
        new(
            ticket.Id,
            ticket.Number,
            QueueTicketDto.ToWireStatus(ticket.Status),
            QueuePriorityParser.ToWire(ticket.Priority),
            ticket.ServiceDate,
            ticket.BranchId,
            ticket.RoomId,
            ahead,
            ticket.CalledAt);
}
