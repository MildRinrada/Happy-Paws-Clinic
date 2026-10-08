using System.Net;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Messaging;
using HappyPaws.Infrastructure.Messaging.Email;
using HappyPaws.Infrastructure.Notifications.Routing;
using HappyPaws.Infrastructure.Persistence.Repositories.Notifications;

namespace HappyPaws.Infrastructure.Notifications.Email;

/// <summary>
/// Email through the clinic's SMTP server (<c>Messaging:Email</c>), to the address on the customer
/// record. The subject is the template's title and the body its LINE wording, which has room.
/// </summary>
internal sealed class EmailDeliveryRoute(IDeliveryTransports transports, ClinicNameReader clinic) : IDeliveryRoute
{
    public DeliveryChannel Channel => DeliveryChannel.Email;

    public Task<string?> UnavailableReasonAsync(
        NotificationRecipient recipient,
        NotificationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            transports.Email is null ? DeliveryErrors.NotConfigured
            : recipient.Email is null ? DeliveryErrors.NoAddress
            : recipient.AllowEmail ? null
            : DeliveryErrors.OptedOut);

    public string MaskedAddress(NotificationRecipient recipient) =>
        recipient.Email is { } email ? RecipientMask.Email(email) : string.Empty;

    public async Task SendAsync(NotificationRecipient recipient, ComposedMessage message, CancellationToken cancellationToken = default)
    {
        var clinicName = await clinic.ReadAsync(cancellationToken);

        await transports.Email!.SendAsync(Compose(recipient.Email!, message, clinicName), clinicName, cancellationToken);
    }

    internal static EmailMessage Compose(string to, ComposedMessage message, string clinicName)
    {
        static string Encode(string value) => WebUtility.HtmlEncode(value);

        var html =
            "<div style=\"font-family:sans-serif;font-size:15px;line-height:1.6;color:#1f2a2e;max-width:520px\">"
            + $"<p style=\"margin:0 0 14px\">{Encode(message.Line)}</p>"
            + $"<p style=\"margin:0;font-size:13px;color:#5a6663\">{Encode(clinicName)}</p>"
            + "</div>";

        return new EmailMessage(to, null, message.Title, $"{message.Line}\n\n{clinicName}", html);
    }
}
