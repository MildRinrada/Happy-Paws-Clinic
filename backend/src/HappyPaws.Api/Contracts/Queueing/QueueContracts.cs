namespace HappyPaws.Api.Contracts.Queueing;

/// <summary>
/// Request body for <c>POST /staff/queue/tickets</c> — a number for a walk-in with no appointment.
/// <para>
/// As with the booking endpoints, the reference specifies this endpoint's behaviour but not its
/// schema; the field names follow the vocabulary the document uses. See
/// <c>docs/assumptions.md § 15</c>.
/// </para>
/// </summary>
public sealed record IssueWalkInTicketRequest(
    string PetId,
    string ServiceId,
    string? BranchId = null,
    string? StaffId = null,
    string? Priority = null,
    string? PriorityReason = null);

/// <summary>
/// <c>POST /staff/queue/{id}/priority</c> — <c>normal</c>, <c>urgent</c> or <c>emergency</c>.
/// A reason is required for anything above normal. A contract extension, see
/// <c>docs/contract-extensions.md § 1</c>.
/// </summary>
public sealed record SetPriorityRequest(string Priority, string? Reason = null);

/// <summary>
/// <c>POST /staff/queue/{id}/leave</c> — the owner gave up waiting. A contract extension, see
/// <c>docs/contract-extensions.md § 2</c>.
/// </summary>
public sealed record LeaveQueueRequest(string? Reason = null);

/// <summary>
/// Request body for <c>POST /staff/queue/{id}/assign</c>. Either field may be sent alone — a room
/// change mid-visit does not require re-stating the vet.
/// </summary>
public sealed record AssignTicketRequest(string? StaffId = null, string? RoomId = null);
