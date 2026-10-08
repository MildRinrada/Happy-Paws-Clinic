using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Notifications.Routing;
using HappyPaws.Infrastructure.Persistence.Repositories.Notifications;

namespace HappyPaws.Infrastructure.Notifications.Line;

/// <summary>
/// LINE push to the clinic's own Official Account followers.
/// <para>
/// Reaches only a customer whose record carries a LINE address and who has not blocked the
/// account. The address is delivery information, like an email address; nothing here learns or
/// links one (<c>docs/assumptions.md § A</c>).
/// </para>
/// </summary>
internal sealed class LineDeliveryRoute(LineCredentialResolver credentials, LineMessagingClient client) : IDeliveryRoute
{
    public DeliveryChannel Channel => DeliveryChannel.Line;

    public async Task<string?> UnavailableReasonAsync(
        NotificationRecipient recipient,
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var line = await credentials.ResolveAsync(cancellationToken);

        if (line.AccessToken is null)
        {
            return DeliveryErrors.NotConfigured;
        }

        if (!line.Settings.Enabled)
        {
            return DeliveryErrors.ChannelDisabled;
        }

        if (!line.Settings.IsTemplateEnabled(request.TemplateKey))
        {
            return DeliveryErrors.TemplateDisabled;
        }

        if (recipient.LineUserId is null)
        {
            return DeliveryErrors.NoAddress;
        }

        if (!recipient.LineReachable)
        {
            return DeliveryErrors.Unfollowed;
        }

        return recipient.AllowLine ? null : DeliveryErrors.OptedOut;
    }

    public string MaskedAddress(NotificationRecipient recipient) =>
        recipient.LineUserId is { } id ? RecipientMask.Line(id) : string.Empty;

    public async Task SendAsync(NotificationRecipient recipient, ComposedMessage message, CancellationToken cancellationToken = default)
    {
        var line = await credentials.ResolveAsync(cancellationToken);

        await client.PushAsync(line.AccessToken!, recipient.LineUserId!, message.Line, message.QuickReplies, cancellationToken);
    }
}
