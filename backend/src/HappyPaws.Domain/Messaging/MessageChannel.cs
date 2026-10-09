using HappyPaws.Domain.Notifications;

namespace HappyPaws.Domain.Messaging;

/// <summary>
/// Where a thread message travelled. The three delivery channels, plus <see cref="Portal"/>: the
/// customer wrote from the portal's chat page (<c>POST /portal/thread/messages</c>,
/// <c>docs/contract-extensions.md § 24</c>).
/// <para>
/// A type of its own rather than a fourth <see cref="DeliveryChannel"/>, because the portal is not
/// a way to <i>send</i> a customer anything: the dispatcher, the delivery log and the staff send
/// endpoint must never see <c>portal</c>. The names of the first three match
/// <see cref="DeliveryChannel"/> exactly, so the stored strings are the same as before.
/// </para>
/// </summary>
public enum MessageChannel
{
    Line,
    Sms,
    Email,
    Portal,
}

/// <summary>Conversions between the thread's channels and the dispatcher's.</summary>
public static class MessageChannels
{
    public static MessageChannel From(DeliveryChannel channel) => channel switch
    {
        DeliveryChannel.Line => MessageChannel.Line,
        DeliveryChannel.Sms => MessageChannel.Sms,
        DeliveryChannel.Email => MessageChannel.Email,
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };

    /// <summary>The delivery channel of the same name, or null for <see cref="MessageChannel.Portal"/>.</summary>
    public static DeliveryChannel? ToDelivery(this MessageChannel channel) => channel switch
    {
        MessageChannel.Line => DeliveryChannel.Line,
        MessageChannel.Sms => DeliveryChannel.Sms,
        MessageChannel.Email => DeliveryChannel.Email,
        _ => null,
    };
}
