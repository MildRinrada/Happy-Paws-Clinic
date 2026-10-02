using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.SetTicketPriority;

/// <summary>
/// <c>POST /staff/queue/{id}/priority</c> — move a patient up or down the order.
/// <para>
/// A contract extension. The reference's queue is strictly first-come with a skip, which leaves an
/// emergency no way past a routine visit — see <c>docs/contract-extensions.md § 1</c>.
/// </para>
/// </summary>
public sealed record SetTicketPriorityCommand(
    string TicketId,
    string Priority,
    string? Reason = null,
    uint? IfMatchVersion = null);

/// <summary>
/// Changes a ticket's urgency, keeping its number and its status.
/// <para>
/// Always audited, and a reason is required for anything above normal. Jumping the queue is a
/// decision that another owner is entitled to have questioned afterwards, and a board that lets it
/// happen without a trace is a board nobody can defend.
/// </para>
/// </summary>
public sealed class SetTicketPriorityHandler(
    QueueAccess access,
    IQueueEventStream events,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<QueueTicketDto> HandleAsync(
        SetTicketPriorityCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        var priority = QueuePriorityParser.Parse(command.Priority);

        QueueTicket? changed = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var ticket = await access.ForStaffAsync(command.TicketId, token);

            ConcurrencyGuard.Ensure(ticket, command.IfMatchVersion);

            var before = ticket.Priority;
            ticket.SetPriority(priority, command.Reason, staffId, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.QueuePriorityChanged,
                AuditResources.QueueTicket,
                ticket.Id,
                ticket.PetId,
                ticket.BranchId,
                new Dictionary<string, object?>
                {
                    ["from"] = before.ToString(),
                    ["to"] = priority.ToString(),
                    ["reason"] = command.Reason,
                }));

            changed = ticket;
        }, cancellationToken);

        // Everyone's position may have moved, not just this ticket's — the whole board re-reads.
        events.Publish(new QueueEvent(
            QueueEventTypes.BoardRefresh, changed!.BranchId, changed.Id, changed.CustomerId));

        return QueueTicketDto.From(changed);
    }
}
