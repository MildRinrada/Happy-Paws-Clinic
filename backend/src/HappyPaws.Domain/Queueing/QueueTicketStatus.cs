namespace HappyPaws.Domain.Queueing;

/// <summary>
/// Queue ticket state machine from the contract:
/// <c>waiting → called → in_room → served</c>, with <c>skipped</c> and <c>left</c> as exits.
/// Tickets reset daily per branch.
/// </summary>
public enum QueueTicketStatus
{
    Waiting,
    Called,
    InRoom,
    Served,
    Skipped,
    Left,
}
