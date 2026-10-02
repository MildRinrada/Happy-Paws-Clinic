using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Domain.Queueing;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Queueing.CallTicket;

/// <summary><c>POST /staff/queue/{id}/call</c> — "เรียกคิว · ยิงแจ้งเตือน LINE ให้เจ้าของ".</summary>
public sealed record CallTicketCommand(string TicketId, uint? IfMatchVersion = null);

/// <summary>
/// Calls a patient forward, then tells the owner.
/// <para>
/// The order matters and is deliberate. The ticket is committed first; the notification and the
/// SSE event go out afterwards, outside the transaction. A LINE gateway that hangs would otherwise
/// hold a row lock for its whole timeout, and a push that fails would roll back a call the
/// receptionist has already announced out loud.
/// </para>
/// <para>
/// The trade that buys: if the process dies between commit and dispatch, the owner is not messaged
/// while the board says "called". That is the right way round — a queue that stops working because
/// LINE is down would be worse than a message that occasionally does not arrive, and the owner is
/// standing in the waiting room looking at the same board.
/// </para>
/// </summary>
public sealed class CallTicketHandler(
    QueueAccess access,
    IQueueEventStream events,
    INotificationDispatcher notifications,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<CallTicketHandler> logger)
{
    public async Task<QueueTicketDto> HandleAsync(
        CallTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        QueueTicket? called = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var ticket = await access.ForStaffAsync(command.TicketId, token);

            ConcurrencyGuard.Ensure(ticket, command.IfMatchVersion);

            ticket.Call(staffId, clock.UtcNow);
            called = ticket;
        }, cancellationToken);

        events.Publish(new QueueEvent(
            QueueEventTypes.TicketCalled, called!.BranchId, called.Id, called.CustomerId));

        await NotifyOwnerAsync(called, cancellationToken);

        return QueueTicketDto.From(called);
    }

    private async Task NotifyOwnerAsync(QueueTicket ticket, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["number"] = ticket.Number,
            ["room"] = ticket.RoomId ?? string.Empty,
        };

        try
        {
            await notifications.SendAsync(
                new NotificationRequest(ticket.CustomerId, NotificationTemplates.QueueCalled, data),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The patient has been called; that is the fact of record. A channel failure is worth
            // knowing about but must not turn a successful call into an error at the counter.
            logger.LogError(
                exception,
                "Queue call notification failed for ticket {TicketId} (customer {CustomerId})",
                ticket.Id,
                ticket.CustomerId);
        }
    }
}
