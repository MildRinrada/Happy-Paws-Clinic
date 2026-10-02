namespace HappyPaws.Domain.Queueing;

/// <summary>
/// The queue-ticket state machine, written once as data — the counterpart to
/// <see cref="Appointments.AppointmentTransitions"/>.
/// <para>
/// The contract draws it as <c>waiting → called → in_room → served</c> with <c>skipped</c> and
/// <c>left</c> as exits. One edge is not in that drawing but is required by an endpoint:
/// <c>POST /staff/queue/{id}/skip</c> is described as "ข้ามคิวที่ยังไม่มา · <b>เลื่อนไปท้ายแถว</b>" —
/// move it to the back of the line — which only means something if a skipped ticket can be called
/// again. So <c>skipped → called</c> exists here. See <c>docs/assumptions.md § 17</c>.
/// </para>
/// </summary>
public static class QueueTicketTransitions
{
    private static readonly Dictionary<QueueTicketStatus, QueueTicketStatus[]> Allowed = new()
    {
        [QueueTicketStatus.Waiting] =
        [
            QueueTicketStatus.Called,
            QueueTicketStatus.Skipped,
            QueueTicketStatus.Left,
        ],

        [QueueTicketStatus.Called] =
        [
            QueueTicketStatus.InRoom,
            // Called and did not come forward: back of the line rather than out of the system.
            QueueTicketStatus.Skipped,
            QueueTicketStatus.Left,
        ],

        // Skipped is a waiting state, not an exit — the owner is still in the building, just later
        // in the order. Note what is *absent*: skipped → skipped. A ticket already at the back
        // cannot be pushed back again without being called first, so a double-press answers 409
        // instead of quietly demoting the owner twice and inflating SkipCount, which the clinic
        // reads as "how many times did this person fail to come forward".
        [QueueTicketStatus.Skipped] = [QueueTicketStatus.Called, QueueTicketStatus.Left],

        [QueueTicketStatus.InRoom] = [QueueTicketStatus.Served],

        // Terminal.
        [QueueTicketStatus.Served] = [],
        [QueueTicketStatus.Left] = [],
    };

    /// <summary>
    /// The statuses in which a ticket is still part of today's queue.
    /// <para>
    /// Exposed as a list because the board query pushes this set into SQL. A repository with its
    /// own hard-coded copy would be one edit away from disagreeing with <see cref="IsLive"/>, and
    /// a disagreement there means a patient vanishing from the board while still waiting.
    /// </para>
    /// </summary>
    public static readonly IReadOnlyList<QueueTicketStatus> LiveStatuses =
    [
        QueueTicketStatus.Waiting,
        QueueTicketStatus.Called,
        QueueTicketStatus.Skipped,
        QueueTicketStatus.InRoom,
    ];

    /// <summary>The statuses in which a ticket is still waiting to be called at all.</summary>
    public static readonly IReadOnlyList<QueueTicketStatus> AwaitingCallStatuses =
    [
        QueueTicketStatus.Waiting,
        QueueTicketStatus.Skipped,
    ];

    private static readonly HashSet<QueueTicketStatus> Live = [.. LiveStatuses];

    public static bool IsAllowed(QueueTicketStatus from, QueueTicketStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    /// <summary>True while the ticket still belongs on today's board.</summary>
    public static bool IsLive(QueueTicketStatus status) => Live.Contains(status);

    public static IReadOnlyCollection<QueueTicketStatus> NextFrom(QueueTicketStatus status) =>
        Allowed.TryGetValue(status, out var next) ? next : [];
}
