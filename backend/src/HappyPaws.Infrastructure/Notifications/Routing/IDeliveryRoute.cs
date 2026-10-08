using HappyPaws.Application.Integrations;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Persistence.Repositories.Notifications;

namespace HappyPaws.Infrastructure.Notifications.Routing;

/// <summary>A message in the words of each channel, with the chat buttons LINE may show under it.</summary>
internal sealed record ComposedMessage(string Title, string Line, string Sms, IReadOnlyList<LineQuickReply> QuickReplies);

/// <summary>
/// One way out — LINE, SMS or email — as <see cref="DeliveringNotificationChannel"/> sees it.
/// <para>
/// A route first says whether it can carry this message to this person, and if not, why: the
/// reason is what the delivery log shows. Only then is it asked to send, and a send that fails
/// throws; the channel above turns the exception into a <c>failed</c> row and tries the next route.
/// </para>
/// </summary>
internal interface IDeliveryRoute
{
    DeliveryChannel Channel { get; }

    /// <summary>Null when the route can carry the message; otherwise a <c>DeliveryErrors</c> code.</summary>
    Task<string?> UnavailableReasonAsync(
        NotificationRecipient recipient,
        NotificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>The address this route would use, masked for the log.</summary>
    string MaskedAddress(NotificationRecipient recipient);

    /// <summary>Hands the message to the provider. Throws when the provider refuses it.</summary>
    Task SendAsync(
        NotificationRecipient recipient,
        ComposedMessage message,
        CancellationToken cancellationToken = default);
}
