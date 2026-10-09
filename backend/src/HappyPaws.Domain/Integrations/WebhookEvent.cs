using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// One event waiting to go out: the webhook outbox.
/// <para>
/// A use case writes this row in the same transaction as the change it describes. If the
/// transaction rolls back, the event never existed; if it commits, the event is on disk before the
/// response leaves, and survives the process dying a moment later. Nothing is sent from inside the
/// transaction. The webhook job reads the outbox afterwards, makes one
/// <see cref="WebhookDelivery"/> per subscription that wants this type
/// (<see cref="MarkDispatched"/>), and sends those.
/// </para>
/// <para>
/// <see cref="Body"/> is the exact JSON that will be sent and signed, stored as text so every
/// retry sends the same bytes.
/// </para>
/// </summary>
public sealed class WebhookEvent : Entity
{
    /// <summary>How long an event and its delivery log are kept: long enough to answer "did you send it?".</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    private WebhookEvent()
    {
    }

    public string Type { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>When the job turned this event into deliveries. Null while it waits.</summary>
    public DateTimeOffset? DispatchedAt { get; private set; }

    /// <summary>A new outbox row. The id is made by the caller, because the body it serialised already names it.</summary>
    public static WebhookEvent Create(string id, string type, string body, DateTimeOffset occurredAt)
    {
        if (!WebhookEventTypes.All.Contains(type))
        {
            throw new ArgumentException($"unknown webhook event type {type}", nameof(type));
        }

        return new WebhookEvent { Id = id, Type = type, Body = body, OccurredAt = occurredAt };
    }

    public void MarkDispatched(DateTimeOffset now) => DispatchedAt ??= now;
}
