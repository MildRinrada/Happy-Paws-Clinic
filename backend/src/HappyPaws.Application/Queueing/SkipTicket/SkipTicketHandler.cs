using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.SkipTicket;

/// <summary><c>POST /staff/queue/{id}/skip</c> — "ข้ามคิวที่ยังไม่มา · เลื่อนไปท้ายแถว".</summary>
public sealed record SkipTicketCommand(string TicketId, uint? IfMatchVersion = null);

/// <summary>
/// Passes over a patient who did not come forward, and puts them at the back of the line.
/// <para>
/// Skipping does not remove anyone from the queue. The contract asks for "เลื่อนไปท้ายแถว" — move
/// to the back — so the ticket stays live and can be called again; it simply sorts behind everyone
/// who has not been passed over. Treating <c>skipped</c> as terminal would mean the owner who
/// stepped out for two minutes has to queue again from the counter.
/// </para>
/// </summary>
public sealed class SkipTicketHandler(
    QueueAccess access,
    IQueueEventStream events,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<QueueTicketDto> HandleAsync(
        SkipTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        QueueTicket? skipped = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var ticket = await access.ForStaffAsync(command.TicketId, token);

            ConcurrencyGuard.Ensure(ticket, command.IfMatchVersion);

            ticket.Skip(clock.UtcNow);
            skipped = ticket;
        }, cancellationToken);

        // The whole board reorders when someone moves to the back, so this is a board.refresh
        // rather than a single-ticket update: a client that only patched one row would show the
        // queue in an order the server no longer agrees with.
        events.Publish(new QueueEvent(
            QueueEventTypes.BoardRefresh, skipped!.BranchId, skipped.Id, skipped.CustomerId));

        return QueueTicketDto.From(skipped);
    }
}
