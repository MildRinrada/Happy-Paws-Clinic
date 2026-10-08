using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Notifications;

/// <summary>The ways a message can travel to a customer outside the web inbox.</summary>
public enum DeliveryChannel
{
    Line,
    Sms,
    Email,
}

/// <summary>What became of one attempt.</summary>
public enum DeliveryStatus
{
    /// <summary>The provider accepted the message. Not proof it was read.</summary>
    Sent,

    /// <summary>The provider refused it or could not be reached.</summary>
    Failed,

    /// <summary>
    /// Nothing was attempted: no consent, no address for the channel, or the channel is switched
    /// off. <see cref="NotificationDelivery.ErrorCode"/> says which.
    /// </summary>
    Skipped,
}

/// <summary>Why the message was sent at all.</summary>
public enum DeliverySource
{
    /// <summary>A use case sent it: a queue call, a receipt, a waiting-list slot.</summary>
    System,

    /// <summary>A member of staff sent it from <c>POST /staff/notifications/send</c>.</summary>
    Manual,

    /// <summary>An announcement from <c>POST /staff/integrations/line/broadcast</c>.</summary>
    Broadcast,

    /// <summary>A reminder rule sent it (<c>/staff/automations</c>). Its batch id is the dispatch id.</summary>
    Automation,
}

/// <summary>
/// One attempt to deliver one message on one channel — a row of
/// <c>GET /staff/notifications/log</c>, "ส่งสำเร็จ/ล้มเหลว · fallback ไป SMS เมื่อไหร่".
/// <para>
/// <b>Append-only, one row per attempt.</b> A LINE push that failed and the SMS that carried the
/// same message a second later are two rows, the second naming the first in
/// <see cref="FallbackFrom"/>. That is what lets the counter answer "did the owner get it, and
/// how", which a single status column on the message could not.
/// </para>
/// <para>
/// The recipient is stored <b>masked</b> (<c>***5678</c>, <c>s***@mail.com</c>, the tail of a LINE
/// id). The log is read by staff who need to know which address was used, not to copy it; the full
/// address stays on the customer record where it is already protected.
/// </para>
/// </summary>
public sealed class NotificationDelivery : Entity
{
    public const int ErrorCodeMax = 60;

    private NotificationDelivery()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>Which message — <c>queue.called</c>, <c>appointment.reminder</c>.</summary>
    public string TemplateKey { get; private set; } = string.Empty;

    /// <summary>Null when nothing reached a channel: no consent, or no channel could carry it.</summary>
    public DeliveryChannel? Channel { get; private set; }

    /// <summary>The masked address used, when there was one.</summary>
    public string? Recipient { get; private set; }

    public DeliveryStatus Status { get; private set; }

    /// <summary>Why it failed or was skipped — <c>no_consent</c>, <c>no_address</c>, <c>provider_error</c>.</summary>
    public string? ErrorCode { get; private set; }

    /// <summary>The channel that failed just before this attempt, when this one is the fallback.</summary>
    public DeliveryChannel? FallbackFrom { get; private set; }

    public DeliverySource Source { get; private set; }

    /// <summary>Groups the deliveries of one staff send or one broadcast.</summary>
    public string? BatchId { get; private set; }

    /// <summary>Who sent it, for a manual send or a broadcast.</summary>
    public string? SentByStaffId { get; private set; }

    /// <summary>The branch the message is about, so a branch-bound reader sees their own.</summary>
    public string? BranchId { get; private set; }

    public static NotificationDelivery Record(
        string customerId,
        string templateKey,
        DeliveryStatus status,
        DeliverySource source,
        DeliveryChannel? channel = null,
        string? recipient = null,
        string? errorCode = null,
        DeliveryChannel? fallbackFrom = null,
        string? batchId = null,
        string? sentByStaffId = null,
        string? branchId = null)
    {
        if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(templateKey))
        {
            throw new DomainException("invalid_delivery", "บันทึกการส่งต้องมีลูกค้าและชนิดข้อความ");
        }

        if (status == DeliveryStatus.Sent && channel is null)
        {
            // A message that was sent went somewhere; a row that cannot say where is not evidence.
            throw new DomainException("invalid_delivery", "การส่งที่สำเร็จต้องระบุช่องทาง");
        }

        return new NotificationDelivery
        {
            Id = EntityId.New(EntityId.NotificationDelivery),
            CustomerId = customerId,
            TemplateKey = templateKey,
            Status = status,
            Source = source,
            Channel = channel,
            Recipient = recipient,
            ErrorCode = errorCode is null ? null : errorCode[..Math.Min(ErrorCodeMax, errorCode.Length)],
            FallbackFrom = fallbackFrom,
            BatchId = batchId,
            SentByStaffId = sentByStaffId,
            BranchId = branchId,
        };
    }
}
