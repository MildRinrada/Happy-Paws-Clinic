using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Queueing;

/// <summary>
/// A place in today's queue — <c>tkt_A014</c>. Issued when an appointment is checked in
/// ("เช็กอินที่เคาน์เตอร์หรือลูกค้าเช็กอินเอง · สร้างคิวทันที") or at the counter for a walk-in
/// with no appointment.
/// <para>
/// <see cref="ServiceDate"/> is stored as a plain date in clinic-local terms because the contract
/// resets numbering daily per branch ("คิวรีเซ็ตรายวันต่อสาขา"). Deriving the day from an instant at
/// read time would put a late-evening ticket in the next day's queue for anyone whose connection
/// reports a different offset; storing the day the counter actually meant removes the question.
/// </para>
/// <para>
/// <see cref="SkippedAt"/> doubles as the ordering key for "เลื่อนไปท้ายแถว". A skipped ticket sorts
/// after every unskipped one and, among skipped tickets, by when it was skipped — so being passed
/// over twice puts you behind someone passed over once, without a mutable position column that
/// every other row would have to be renumbered against.
/// </para>
/// </summary>
public sealed class QueueTicket : Entity
{
    private QueueTicket()
    {
    }

    /// <summary>Display number, unique per branch per day — <c>A014</c>.</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>The numeric part, kept separately so the board can order without parsing strings.</summary>
    public int Sequence { get; private set; }

    public string BranchId { get; private set; } = string.Empty;

    public DateOnly ServiceDate { get; private set; }

    /// <summary>Null for a ticket issued at the counter without an appointment.</summary>
    public string? AppointmentId { get; private set; }

    public string PetId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>The vet this ticket is assigned to. Null until someone is free.</summary>
    public string? StaffId { get; private set; }

    /// <summary>The consulting room, from the branch's registry. Null until assigned.</summary>
    public string? RoomId { get; private set; }

    public QueueTicketStatus Status { get; private set; } = QueueTicketStatus.Waiting;

    /// <summary>
    /// How urgently this patient must be seen. Ordering reads this before anything else, so an
    /// emergency is called next regardless of number. A contract extension — see
    /// <see cref="QueuePriority"/>.
    /// </summary>
    public QueuePriority Priority { get; private set; } = QueuePriority.Normal;

    /// <summary>Why the priority was raised. Required for anything above normal, and audited.</summary>
    public string? PriorityReason { get; private set; }

    public string? PrioritySetByStaffId { get; private set; }

    public DateTimeOffset? PrioritySetAt { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset? CalledAt { get; private set; }

    /// <summary>Who pressed call — the contract pushes a LINE notification on this action.</summary>
    public string? CalledByStaffId { get; private set; }

    public DateTimeOffset? EnteredRoomAt { get; private set; }

    public DateTimeOffset? ServedAt { get; private set; }

    /// <summary>When it was last passed over. Also the ordering key that puts it at the back.</summary>
    public DateTimeOffset? SkippedAt { get; private set; }

    /// <summary>How many times this ticket has been passed over, for the counter's judgement.</summary>
    public int SkipCount { get; private set; }

    public DateTimeOffset? LeftAt { get; private set; }

    /// <summary>Why the owner left — "รอไม่ไหว", "นัดใหม่แล้ว". Kept for the wait-time statistics.</summary>
    public string? LeftReason { get; private set; }

    /// <summary>True while the ticket still belongs on today's board.</summary>
    public bool IsLive => QueueTicketTransitions.IsLive(Status);

    /// <param name="appointment">The visit being checked in.</param>
    /// <param name="sequence">The allocated position in the numbering.</param>
    /// <param name="serviceDate">The clinic-local day the ticket belongs to.</param>
    /// <param name="now">When it was issued.</param>
    /// <param name="number">
    /// The number as the clinic's queue settings print it. Omitted, it falls back to the contract's
    /// own example format — see <see cref="FormatNumber"/>.
    /// </param>
    /// <param name="priority">Where the ticket starts, from the appointment's type.</param>
    /// <param name="priorityReason">Why, when it is not normal.</param>
    public static QueueTicket IssueForAppointment(
        Appointments.Appointment appointment,
        int sequence,
        DateOnly serviceDate,
        DateTimeOffset now,
        string? number = null,
        QueuePriority priority = QueuePriority.Normal,
        string? priorityReason = null) =>
        new()
        {
            Id = EntityId.New(EntityId.QueueTicket),
            Number = number ?? FormatNumber(sequence),
            Sequence = sequence,
            BranchId = appointment.BranchId,
            ServiceDate = serviceDate,
            AppointmentId = appointment.Id,
            PetId = appointment.PetId,
            CustomerId = appointment.CustomerId,
            ServiceId = appointment.ServiceId,
            StaffId = appointment.StaffId,
            IssuedAt = now,

            // An emergency appointment type enters the queue as an emergency. Set by the clinic's
            // own type rules rather than by a person, so there is no staff id to record.
            Priority = priority,
            PriorityReason = priority == QueuePriority.Normal ? null : priorityReason,
            PrioritySetAt = priority == QueuePriority.Normal ? null : now,
        };

    /// <summary><c>POST /staff/queue/tickets</c> — "ออกบัตรคิวให้ walk-in ที่ยังไม่มีนัด".</summary>
    public static QueueTicket IssueWalkIn(
        string branchId,
        string petId,
        string customerId,
        string serviceId,
        int sequence,
        DateOnly serviceDate,
        DateTimeOffset now,
        string? staffId = null,
        string? number = null) =>
        new()
        {
            Id = EntityId.New(EntityId.QueueTicket),
            Number = number ?? FormatNumber(sequence),
            Sequence = sequence,
            BranchId = branchId,
            ServiceDate = serviceDate,
            PetId = petId,
            CustomerId = customerId,
            ServiceId = serviceId,
            StaffId = staffId,
            IssuedAt = now,
        };

    /// <summary>
    /// <c>POST /staff/queue/{id}/assign</c> — "จ่ายหมอและห้องตรวจ · ย้ายคิวข้ามห้องได้".
    /// <para>
    /// Not a state transition: a ticket keeps its status through a reassignment, which is what
    /// makes moving someone between rooms mid-visit possible. A null argument leaves that side
    /// alone, so a room can be changed without re-stating the vet.
    /// </para>
    /// </summary>
    public void Assign(string? staffId, string? roomId)
    {
        if (!IsLive)
        {
            throw new StateConflictException(nameof(QueueTicket), Status.ToString(), nameof(Assign));
        }

        if (staffId is { Length: > 0 })
        {
            StaffId = staffId;
        }

        if (roomId is { Length: > 0 })
        {
            RoomId = roomId;
        }
    }

    /// <summary><c>POST /staff/queue/{id}/call</c> — "เรียกคิว · ยิงแจ้งเตือน LINE ให้เจ้าของ".</summary>
    public void Call(string byStaffId, DateTimeOffset now)
    {
        Transition(QueueTicketStatus.Called, nameof(Call));

        CalledAt = now;
        CalledByStaffId = byStaffId;
    }

    /// <summary><c>POST /staff/queue/{id}/skip</c> — "ข้ามคิวที่ยังไม่มา · เลื่อนไปท้ายแถว".</summary>
    public void Skip(DateTimeOffset now)
    {
        Transition(QueueTicketStatus.Skipped, nameof(Skip));

        // Overwritten on each skip on purpose: the ordering key is when you were *last* passed
        // over, so a second skip sends you behind everyone skipped since the first.
        SkippedAt = now;
        SkipCount++;
    }

    /// <summary>
    /// Called when the consultation opens (<c>POST /staff/consultations</c> — "เปิดเคสจากคิวหรือจากนัด").
    /// Lives here rather than in the Consultation module because the transition rule is the
    /// ticket's own.
    /// </summary>
    public void EnterRoom(DateTimeOffset now)
    {
        Transition(QueueTicketStatus.InRoom, nameof(EnterRoom));
        EnteredRoomAt = now;
    }

    /// <summary>Called when the consultation closes.</summary>
    public void MarkServed(DateTimeOffset now)
    {
        Transition(QueueTicketStatus.Served, nameof(MarkServed));
        ServedAt = now;
    }

    /// <summary>
    /// <c>POST /staff/queue/{id}/leave</c> — the owner gave up and went home.
    /// <para>
    /// A contract extension. The reference draws <c>left</c> in the queue state machine but gives
    /// no endpoint that reaches it, so a ticket could only ever arrive there in theory — leaving
    /// the departed owner on the board indefinitely and skewing every wait-time figure derived from
    /// it. See <c>docs/contract-extensions.md § 2</c>.
    /// </para>
    /// </summary>
    public void MarkLeft(DateTimeOffset now, string? reason = null)
    {
        Transition(QueueTicketStatus.Left, nameof(MarkLeft));
        LeftAt = now;
        LeftReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>
    /// <c>POST /staff/queue/{id}/priority</c> — raise or lower a ticket's urgency.
    /// <para>
    /// Not a state transition: an emergency that arrives while already waiting keeps its number and
    /// its status, and only its place in the order changes. Raising priority demands a reason,
    /// because jumping the queue is a decision someone has to be able to defend afterwards — it is
    /// written to the audit log for that purpose.
    /// </para>
    /// </summary>
    public void SetPriority(QueuePriority priority, string? reason, string byStaffId, DateTimeOffset now)
    {
        if (!IsLive)
        {
            throw new StateConflictException(nameof(QueueTicket), Status.ToString(), nameof(SetPriority));
        }

        if (priority != QueuePriority.Normal && string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("invalid_priority", "ต้องระบุเหตุผลเมื่อเลื่อนลำดับความเร่งด่วน");
        }

        Priority = priority;
        PriorityReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        PrioritySetByStaffId = byStaffId;
        PrioritySetAt = now;
    }

    /// <summary>
    /// The contract's example format — <c>A014</c>. What a clinic actually prints comes from its
    /// queue settings (<c>PATCH /clinic/settings/queue</c>); this is what those settings default to.
    /// </summary>
    public static string FormatNumber(int sequence) => $"A{sequence:D3}";

    private void Transition(QueueTicketStatus next, string attempted)
    {
        if (!QueueTicketTransitions.IsAllowed(Status, next))
        {
            throw new StateConflictException(nameof(QueueTicket), Status.ToString(), attempted);
        }

        Status = next;
    }
}
