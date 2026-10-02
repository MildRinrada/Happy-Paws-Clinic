using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.LeaveQueue;

/// <summary>
/// <c>POST /staff/queue/{id}/leave</c> — the owner gave up waiting and went home.
/// <para>
/// A contract extension. <c>left</c> is drawn in the reference's queue state machine but no
/// endpoint produces it — see <c>docs/contract-extensions.md § 2</c>.
/// </para>
/// </summary>
public sealed record LeaveQueueCommand(string TicketId, string? Reason = null, uint? IfMatchVersion = null);

/// <summary>
/// Takes a ticket off the board without pretending the patient was seen.
/// <para>
/// The distinction from <c>served</c> is the whole point. Without it, the only ways to clear a
/// departed owner are to leave them waiting forever — which makes the board wrong and every
/// wait-time average meaningless — or to mark them served, which puts a visit that never happened
/// into the day's figures.
/// </para>
/// </summary>
public sealed class LeaveQueueHandler(
    QueueAccess access,
    IQueueEventStream events,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<QueueTicketDto> HandleAsync(
        LeaveQueueCommand command,
        CancellationToken cancellationToken = default)
    {
        QueueTicket? left = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var ticket = await access.ForStaffAsync(command.TicketId, token);

            ConcurrencyGuard.Ensure(ticket, command.IfMatchVersion);

            ticket.MarkLeft(clock.UtcNow, command.Reason);
            left = ticket;
        }, cancellationToken);

        events.Publish(new QueueEvent(
            QueueEventTypes.BoardRefresh, left!.BranchId, left.Id, left.CustomerId));

        return QueueTicketDto.From(left);
    }
}
