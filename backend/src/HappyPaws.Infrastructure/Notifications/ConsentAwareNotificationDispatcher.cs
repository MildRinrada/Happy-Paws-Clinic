using HappyPaws.Application.Customers;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Notifications;

/// <summary>
/// Checks consent before letting a message through to the channel adapter.
/// <para>
/// A decorator rather than a check inside each use case, and that is the whole design. There are
/// already four call sites that send a customer a message and there will be dozens; a rule enforced
/// at each of them is a rule that will be missed at the next one, and the failure mode is marketing
/// sent to someone who opted out. Putting it in front of the port means the only way to send a
/// message is through the gate.
/// </para>
/// <para>
/// A suppressed message is logged at information, not swallowed silently. "Why did the owner not
/// get the reminder" is a question the counter will ask, and "they withdrew consent on the 3rd" has
/// to be answerable.
/// </para>
/// <para>
/// A message that passes the gate is put in the customer's web inbox before it is handed to a
/// channel — <c>GET /portal/notifications</c>. The inbox is written even when the channel then
/// fails, because it is the record of what the clinic said, and a failed inbox write never stops
/// the channel.
/// </para>
/// </summary>
public sealed class ConsentAwareNotificationDispatcher(
    INotificationChannel channel,
    ConsentGate gate,
    IInboxRepository inbox,
    IDeliveryLog deliveryLog,
    ILogger<ConsentAwareNotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (!await gate.MayReceiveAsync(request.CustomerId, request.TemplateKey, cancellationToken))
        {
            logger.LogInformation(
                "Notification {Template} to customer {CustomerId} suppressed — no consent for {Purpose}",
                request.TemplateKey,
                request.CustomerId,
                ConsentGate.PurposeOf(request.TemplateKey));

            // On the delivery log too, so "why did the owner not get it" is answered on the log
            // page and not only in the application log.
            await RecordSuppressedAsync(request, cancellationToken);
            return;
        }

        try
        {
            var text = NotificationText.Render(request);

            await inbox.DeliverAsync(
                InboxNotification.Deliver(request.CustomerId, request.TemplateKey, text.Title, text.Body, NullIfEmpty(text.ReferenceId)),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Inbox copy of {Template} for customer {CustomerId} was not saved", request.TemplateKey, request.CustomerId);
        }

        await channel.SendAsync(request, cancellationToken);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private async Task RecordSuppressedAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await deliveryLog.RecordAsync(
                NotificationDelivery.Record(
                    request.CustomerId,
                    request.TemplateKey,
                    DeliveryStatus.Skipped,
                    request.Source,
                    errorCode: DeliveryErrors.NoConsent,
                    batchId: request.BatchId,
                    sentByStaffId: request.SentByStaffId,
                    branchId: request.BranchId),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Delivery log row for suppressed {Template} (customer {CustomerId}) was not saved", request.TemplateKey, request.CustomerId);
        }
    }
}
