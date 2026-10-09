using System.Net;
using HappyPaws.Application.Common;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Notifications.Routing;
using HappyPaws.Infrastructure.Persistence.Repositories.Notifications;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Notifications;

/// <summary>
/// Delivers a message that has passed the consent gate: picks the channel, sends, falls back, and
/// writes every attempt to the delivery log.
/// <para>
/// <b>Routing.</b> With no channel asked for, LINE is tried first, then SMS, then email, skipping
/// any that cannot carry this message to this person (not set up, no address, switched off, turned
/// off by the customer). When a channel is tried and fails, the next one is tried, and its row
/// names the one that failed in <c>fallback_from</c> — the contract's "fallback ไป SMS เมื่อไหร่".
/// A channel asked for by name is the only one tried. A preferred channel (a thread reply's "the
/// channel the customer used last") is tried first, with the usual order after it.
/// </para>
/// <para>
/// <b>Nothing set up.</b> When no channel is configured at all, which is a developer's machine, the
/// message goes to <see cref="LoggingNotificationChannel"/> as before and the log row says
/// <c>channel_not_configured</c>, so dev keeps working and nobody mistakes it for delivery.
/// </para>
/// <para>
/// <b>Never throws</b> for an undeliverable message (the <see cref="INotificationChannel"/> rule).
/// Failures are logged at error with the customer, template and provider status, never with the
/// message text or a credential.
/// </para>
/// <para>
/// <b>Not an outbox.</b> A message is sent once, in the request that caused it, after its
/// transaction committed. If the process stops in between, the message is lost, as before; what is
/// new is that a failure is now on record in the log rather than only in the application log.
/// </para>
/// </summary>
internal sealed class DeliveringNotificationChannel(
    NotificationRecipientReader recipients,
    NotificationComposer composer,
    IEnumerable<IDeliveryRoute> routes,
    IDeliveryLog deliveryLog,
    LoggingNotificationChannel fallback,
    ILogger<DeliveringNotificationChannel> logger) : INotificationChannel
{
    private static readonly DeliveryChannel[] Order = [DeliveryChannel.Line, DeliveryChannel.Sms, DeliveryChannel.Email];

    public async Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var recipient = await recipients.ReadAsync(request.CustomerId, cancellationToken);

        if (recipient is null)
        {
            await RecordAsync(request, DeliveryStatus.Skipped, errorCode: DeliveryErrors.NoRecipient, cancellationToken: cancellationToken);
            return;
        }

        var asked = Asked(request.Channel);
        var preferred = asked is null ? Asked(request.PreferredChannel) : null;
        var candidates = Order
            .OrderBy(channel => channel == preferred ? 0 : 1)
            .Where(channel => asked is null || channel == asked)
            .Select(channel => routes.First(route => route.Channel == channel))
            .ToList();

        ComposedMessage? message = null;
        DeliveryChannel? failedBefore = null;
        (DeliveryChannel Channel, string Reason)? firstSkip = null;
        var anyConfigured = false;

        foreach (var route in candidates)
        {
            var reason = await route.UnavailableReasonAsync(recipient, request, cancellationToken);
            anyConfigured |= reason != DeliveryErrors.NotConfigured;

            if (reason is not null)
            {
                firstSkip ??= (route.Channel, reason);
                continue;
            }

            message ??= await composer.ComposeAsync(request, cancellationToken);

            try
            {
                await route.SendAsync(recipient, message, cancellationToken);
                await RecordAsync(request, DeliveryStatus.Sent, route.Channel, route.MaskedAddress(recipient), null, failedBefore, cancellationToken);
                return;
            }
            catch (Exception error) when (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                var code = ErrorCodeOf(error);

                logger.LogError(
                    error,
                    "Notification {Template} to customer {CustomerId} failed on {Channel} ({Code})",
                    request.TemplateKey,
                    request.CustomerId,
                    route.Channel,
                    code);

                await RecordAsync(request, DeliveryStatus.Failed, route.Channel, route.MaskedAddress(recipient), code, failedBefore, cancellationToken);
                failedBefore = route.Channel;
            }
        }

        if (failedBefore is not null)
        {
            return;
        }

        if (!anyConfigured && asked is null)
        {
            // A developer's machine: no LINE, no SMS, no email. Keep the loud log line.
            await fallback.SendAsync(request, cancellationToken);
            await RecordAsync(request, DeliveryStatus.Skipped, errorCode: DeliveryErrors.NotConfigured, cancellationToken: cancellationToken);
            return;
        }

        await RecordAsync(
            request,
            DeliveryStatus.Skipped,
            firstSkip?.Channel ?? asked,
            errorCode: firstSkip?.Reason ?? DeliveryErrors.NoChannel,
            cancellationToken: cancellationToken);
    }

    /// <summary>The channel the request names, or null for automatic routing (also for an unknown name).</summary>
    private static DeliveryChannel? Asked(string? channel) =>
        channel is { Length: > 0 } && WireEnum.TryParse<DeliveryChannel>(channel, out var parsed) ? parsed : null;

    /// <summary>A short code for the log. Provider bodies stay in the application log only.</summary>
    private static string ErrorCodeOf(Exception error) => error switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } => DeliveryErrors.ProviderUnauthorized,
        HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } => DeliveryErrors.ProviderQuota,
        HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity or HttpStatusCode.NotFound } => DeliveryErrors.ProviderRejected,
        HttpRequestException { StatusCode: null } => DeliveryErrors.ProviderUnreachable,
        OperationCanceledException or TimeoutException or IOException or System.Net.Sockets.SocketException => DeliveryErrors.ProviderUnreachable,
        _ => DeliveryErrors.ProviderError,
    };

    private async Task RecordAsync(
        NotificationRequest request,
        DeliveryStatus status,
        DeliveryChannel? channel = null,
        string? recipient = null,
        string? errorCode = null,
        DeliveryChannel? fallbackFrom = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await deliveryLog.RecordAsync(
                NotificationDelivery.Record(
                    request.CustomerId,
                    request.TemplateKey,
                    status,
                    request.Source,
                    channel,
                    string.IsNullOrEmpty(recipient) ? null : recipient,
                    errorCode,
                    fallbackFrom,
                    request.BatchId,
                    request.SentByStaffId,
                    request.BranchId),
                cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // The message went (or did not); losing its log row must not turn into a failed request.
            logger.LogError(error, "Delivery log row for {Template} to customer {CustomerId} was not saved", request.TemplateKey, request.CustomerId);
        }
    }
}
