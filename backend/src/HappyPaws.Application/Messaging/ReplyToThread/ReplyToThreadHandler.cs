using FluentValidation;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Messaging.Dtos;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Messaging;
using HappyPaws.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Messaging.ReplyToThread;

/// <summary><c>POST /staff/threads/{id}/messages</c> — <c>{ "text": "..." }</c>.</summary>
public sealed record ReplyToThreadCommand(string ThreadId, string? Text);

public sealed class ReplyToThreadValidator : AbstractValidator<ReplyToThreadCommand>
{
    public ReplyToThreadValidator()
    {
        RuleFor(command => command.Text)
            .Must(text => !string.IsNullOrWhiteSpace(text)).WithMessage("ต้องพิมพ์ข้อความ")
            .Must(text => text is null || text.Trim().Length <= ThreadMessage.ReplyTextMax)
            .WithMessage($"ข้อความยาวได้ไม่เกิน {ThreadMessage.ReplyTextMax} ตัวอักษร");
    }
}

/// <summary>
/// "ตอบกลับ · ระบบเลือกช่องทางที่ลูกค้าใช้ล่าสุด": staff answer a customer in their thread.
/// <para>
/// <b>The same road as every other message.</b> The reply is stored first, then sent through
/// <see cref="INotificationDispatcher"/> after the write commits, as the <c>thread.reply</c>
/// template. So the consent gate, the web inbox and the delivery log apply exactly as they do to a
/// queue call: a customer who withdrew consent to service messages is not sent the reply, and the
/// answer says <c>skipped</c> with <c>no_consent</c>.
/// </para>
/// <para>
/// <b>The channel.</b> The channel the customer last wrote on is tried first; if it cannot carry
/// the reply (they blocked the LINE account, LINE is switched off) or it fails, the usual order of
/// LINE, SMS and email follows. A reply is never lost for want of the first channel.
/// </para>
/// <para>
/// The delivery log rows carry this message's id as <c>batch_id</c>, and the outcome is read back
/// from them onto the message, so the thread and the log page tell the same story.
/// </para>
/// </summary>
public sealed class ReplyToThreadHandler(
    IMessageThreadRepository threads,
    ICustomerRepository customers,
    INotificationDispatcher dispatcher,
    IDeliveryLog deliveryLog,
    BranchGuard branchGuard,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator,
    ILogger<ReplyToThreadHandler> logger)
{
    public async Task<ThreadMessageDto> HandleAsync(ReplyToThreadCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        MessageThread? thread = null;
        ThreadMessage? message = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            thread = await threads.FindTrackedAsync(command.ThreadId, token)
                ?? throw new NotFoundException("thread", command.ThreadId);

            if (thread.BranchId is { } branchId)
            {
                branchGuard.EnsureCanAccess(branchId);
            }

            var customer = await customers.FindByIdAsync(thread.CustomerId, token);

            if (customer is null || customer.IsMerged)
            {
                // A merge moves the thread to the surviving record, so this is a thread with no one to answer.
                throw new StateConflictException("MessageThread", "customer_merged", "reply");
            }

            message = thread.Reply(staffId, command.Text!, clock.UtcNow);
            threads.AddMessage(message);
        }, cancellationToken);

        await SendAsync(thread!, message!, staffId, cancellationToken);

        return ThreadMessageDto.From(message!, await threads.StaffNameAsync(staffId, cancellationToken));
    }

    private async Task SendAsync(MessageThread thread, ThreadMessage message, string staffId, CancellationToken cancellationToken)
    {
        try
        {
            await dispatcher.SendAsync(
                new NotificationRequest(
                    thread.CustomerId,
                    NotificationTemplates.ThreadReply,
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["message"] = message.Text! })
                {
                    BranchId = thread.BranchId ?? (currentUser.BranchIds.Count == 1 ? currentUser.BranchIds.First() : null),
                    PreferredChannel = thread.LastInboundChannel is { } channel ? WireEnum.Of(channel) : null,
                    Source = DeliverySource.Manual,
                    BatchId = message.Id,
                    SentByStaffId = staffId,
                },
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The reply is saved; a failed send shows on the message, not as a failed request.
            logger.LogError(exception, "Thread reply {MessageId} was not handed to the dispatcher", message.Id);
        }

        try
        {
            var rows = await deliveryLog.ForBatchAsync(message.Id, cancellationToken);
            var outcome = SendResultDto.Summarise(message.Id, [thread.CustomerId], rows).Results[0];

            message.RecordDelivery(
                outcome.Status switch
                {
                    "sent" => DeliveryStatus.Sent,
                    "failed" => DeliveryStatus.Failed,
                    _ => DeliveryStatus.Skipped,
                },
                WireEnum.TryParse<DeliveryChannel>(outcome.Channel, out var used) ? used : null,
                outcome.Status == "unknown" ? DeliveryErrors.NoChannel : outcome.ErrorCode,
                WireEnum.TryParse<DeliveryChannel>(outcome.FallbackFrom, out var from) ? from : null);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Outcome of thread reply {MessageId} was not recorded", message.Id);
        }
    }
}
