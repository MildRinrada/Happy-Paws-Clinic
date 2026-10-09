using HappyPaws.Domain.Common;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.Domain.Messaging;

/// <summary>Who wrote a message: the customer, or the clinic.</summary>
public enum MessageDirection
{
    Inbound,
    Outbound,
}

/// <summary>
/// What a message is. Only text is kept as words; for a picture, a sticker or a file the thread
/// records that one was sent, not the content, which stays with the channel.
/// </summary>
public enum MessageKind
{
    Text,
    Image,
    Video,
    Audio,
    File,
    Location,
    Sticker,
    Other,
}

/// <summary>
/// One message in a <see cref="MessageThread"/>.
/// <para>
/// An inbound message carries the channel it came on and the channel's own id for it, which is
/// unique per channel so a webhook delivered twice is stored once. An outbound message carries the
/// staff member who wrote it and, once sent, what became of it: the same outcome the delivery log
/// records under <c>batch_id</c> equal to this message's id.
/// </para>
/// </summary>
public sealed class ThreadMessage : Entity
{
    /// <summary>LINE's own limit on a text message.</summary>
    public const int InboundTextMax = 5_000;

    /// <summary>What a customer may type in the portal's chat: the same as a reply, so an answer quoting it fits.</summary>
    public const int PortalTextMax = 1_000;

    /// <summary>A reply has to fit LINE, SMS and email alike; SMS is the tight one.</summary>
    public const int ReplyTextMax = 1_000;

    public const int ExternalIdMax = 100;

    private ThreadMessage()
    {
    }

    public string ThreadId { get; private set; } = string.Empty;

    public MessageDirection Direction { get; private set; }

    /// <summary>
    /// The channel it came on (inbound), or the channel it was delivered on (outbound, null until
    /// it is sent, and when it reached no channel).
    /// </summary>
    public MessageChannel? Channel { get; private set; }

    public MessageKind Kind { get; private set; }

    /// <summary>The words. Null for a picture or a sticker.</summary>
    public string? Text { get; private set; }

    /// <summary>The channel's id for an inbound message (LINE's message id).</summary>
    public string? ExternalId { get; private set; }

    /// <summary>The staff member who wrote a reply.</summary>
    public string? SentByStaffId { get; private set; }

    /// <summary>When it was received or written.</summary>
    public DateTimeOffset SentAt { get; private set; }

    /// <summary>What became of a reply: sent, failed or skipped. Null for inbound and before sending.</summary>
    public DeliveryStatus? DeliveryStatus { get; private set; }

    public string? DeliveryErrorCode { get; private set; }

    /// <summary>The channel that failed before the one that carried the reply.</summary>
    public DeliveryChannel? FallbackFrom { get; private set; }

    internal static ThreadMessage Inbound(
        string threadId,
        MessageChannel channel,
        MessageKind kind,
        string? text,
        string? externalId,
        DateTimeOffset at)
    {
        var words = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        var external = string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim();

        return new ThreadMessage
        {
            Id = EntityId.New(EntityId.ThreadMessage),
            ThreadId = threadId,
            Direction = MessageDirection.Inbound,
            Channel = channel,
            Kind = kind,
            Text = words is { Length: > InboundTextMax } ? words[..InboundTextMax] : words,
            ExternalId = external is { Length: > ExternalIdMax } ? external[..ExternalIdMax] : external,
            SentAt = at,
        };
    }

    internal static ThreadMessage Outbound(string threadId, string staffId, string text, DateTimeOffset at)
    {
        var words = text?.Trim();

        if (string.IsNullOrEmpty(words) || words.Length > ReplyTextMax)
        {
            throw new DomainException("invalid_reply", $"ข้อความตอบกลับต้องมี 1 ถึง {ReplyTextMax} ตัวอักษร");
        }

        return new ThreadMessage
        {
            Id = EntityId.New(EntityId.ThreadMessage),
            ThreadId = threadId,
            Direction = MessageDirection.Outbound,
            Kind = MessageKind.Text,
            Text = words,
            SentByStaffId = staffId,
            SentAt = at,
        };
    }

    /// <summary>Records what the delivery did with a reply.</summary>
    public void RecordDelivery(DeliveryStatus status, DeliveryChannel? channel, string? errorCode, DeliveryChannel? fallbackFrom)
    {
        if (Direction != MessageDirection.Outbound)
        {
            throw new DomainException("invalid_reply", "บันทึกผลการส่งได้เฉพาะข้อความตอบกลับ");
        }

        DeliveryStatus = status;
        Channel = channel is { } used ? MessageChannels.From(used) : null;
        DeliveryErrorCode = errorCode;
        FallbackFrom = fallbackFrom;
    }
}
