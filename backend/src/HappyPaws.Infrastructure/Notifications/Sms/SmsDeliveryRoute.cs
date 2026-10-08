using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Notifications.Routing;
using HappyPaws.Infrastructure.Persistence.Repositories.Notifications;

namespace HappyPaws.Infrastructure.Notifications.Sms;

/// <summary>
/// SMS through the gateway the sign-in codes already use (<c>Messaging:Sms</c>, ThaiBulkSMS), to the
/// phone number on the customer record.
/// </summary>
internal sealed class SmsDeliveryRoute(IDeliveryTransports transports) : IDeliveryRoute
{
    public DeliveryChannel Channel => DeliveryChannel.Sms;

    public Task<string?> UnavailableReasonAsync(
        NotificationRecipient recipient,
        NotificationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            transports.Sms is null ? DeliveryErrors.NotConfigured
            : Digits(recipient.Phone).Length == 0 ? DeliveryErrors.NoAddress
            : recipient.AllowSms ? null
            : DeliveryErrors.OptedOut);

    public string MaskedAddress(NotificationRecipient recipient) => RecipientMask.Phone(recipient.Phone);

    public Task SendAsync(NotificationRecipient recipient, ComposedMessage message, CancellationToken cancellationToken = default) =>
        transports.Sms!.SendAsync(Digits(recipient.Phone), message.Sms, cancellationToken);

    /// <summary>The counter may have typed <c>081-234-5678</c>; the gateway wants the digits.</summary>
    private static string Digits(string phone) => new([.. phone.Where(char.IsAsciiDigit)]);
}
