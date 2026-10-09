using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Integrations;

public enum WebhookDeliveryStatus
{
    /// <summary>Waiting for its next attempt (the first, or a retry).</summary>
    Pending,

    /// <summary>The receiver answered 2xx.</summary>
    Succeeded,

    /// <summary>Every attempt failed; nothing more will be tried.</summary>
    Failed,
}

/// <summary>
/// The contract's retry rule, "retry: 5 ครั้ง แบบ exponential ภายใน 24 ชม.".
/// <para>
/// Read as: one first attempt, then up to five retries, each delay five times the one before:
/// 1, 5, 25, 125 and 625 minutes. The last retry is about 13 hours after the event, inside the
/// 24 hours the contract allows, and an hour-long outage at the receiver costs nothing. No jitter:
/// deliveries already spread out because events do, and a fixed schedule is one a receiver's
/// developer can reason about.
/// </para>
/// </summary>
public static class WebhookRetrySchedule
{
    public const int MaxRetries = 5;

    public const int MaxAttempts = MaxRetries + 1;

    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(25),
        TimeSpan.FromMinutes(125),
        TimeSpan.FromMinutes(625),
    ];

    /// <summary>The wait after failed attempt number <paramref name="attempt"/> (1-based), or null when it was the last.</summary>
    public static TimeSpan? DelayAfter(int attempt) =>
        attempt >= 1 && attempt <= MaxRetries ? Delays[attempt - 1] : null;
}

/// <summary>
/// One event on its way to one subscription, with the outcome of its latest attempt — a row of
/// <c>GET /staff/webhooks/{id}/deliveries</c>.
/// <para>
/// <b>Claiming.</b> The job takes a due row with <c>FOR UPDATE SKIP LOCKED</c> and pushes
/// <see cref="NextAttemptAt"/> forward by a lease before it sends, then records the outcome. Two
/// API instances never send the same attempt; an instance that dies mid-send leaves a row that
/// becomes due again when the lease runs out, so a delivery is sent at least once, and a receiver
/// must expect the odd duplicate (the event id in <c>X-Webhook-Id</c> lets it tell).
/// </para>
/// <para>
/// <b>The receiver's answer is untrusted.</b> Only the status code and the first
/// <see cref="ResponseBodyMaxLength"/> characters of the body are kept, for the clinic to read
/// when a receiver says it never got anything.
/// </para>
/// </summary>
public sealed class WebhookDelivery : Entity
{
    public const int ResponseBodyMaxLength = 1_000;

    public const int ErrorMaxLength = 300;

    /// <summary>The error on a delivery stopped because its webhook was switched off.</summary>
    public const string HaltedDisabled = "webhook_disabled";

    /// <summary>The error on a delivery stopped because its webhook was deleted.</summary>
    public const string HaltedDeleted = "webhook_deleted";

    private WebhookDelivery()
    {
    }

    public string SubscriptionId { get; private set; } = string.Empty;

    public string EventId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public WebhookDeliveryStatus Status { get; private set; } = WebhookDeliveryStatus.Pending;

    public int Attempts { get; private set; }

    /// <summary>When the next attempt is due. Null once the delivery has succeeded or given up.</summary>
    public DateTimeOffset? NextAttemptAt { get; private set; }

    public DateTimeOffset? LastAttemptAt { get; private set; }

    public int? LastStatusCode { get; private set; }

    /// <summary>Why the latest attempt failed, in a short machine-readable form: <c>timeout</c>, <c>http_500</c>, <c>address_blocked</c>.</summary>
    public string? LastError { get; private set; }

    public string? LastResponseBody { get; private set; }

    public int? LastDurationMs { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static WebhookDelivery Queue(string subscriptionId, WebhookEvent webhookEvent, DateTimeOffset now) =>
        new()
        {
            Id = EntityId.New(EntityId.WebhookDelivery),
            SubscriptionId = subscriptionId,
            EventId = webhookEvent.Id,
            EventType = webhookEvent.Type,
            NextAttemptAt = now,
        };

    /// <summary>Holds the row for one send, so another worker does not take it meanwhile.</summary>
    public void Lease(DateTimeOffset until) => NextAttemptAt = until;

    public void RecordSuccess(DateTimeOffset at, int statusCode, string? responseBody, int durationMs)
    {
        Record(at, statusCode, error: null, responseBody, durationMs);
        Status = WebhookDeliveryStatus.Succeeded;
        NextAttemptAt = null;
        CompletedAt = at;
    }

    /// <summary>Records a failed attempt and schedules the next one, or gives up after the last.</summary>
    public void RecordFailure(DateTimeOffset at, int? statusCode, string error, string? responseBody, int durationMs)
    {
        Record(at, statusCode, error, responseBody, durationMs);

        if (WebhookRetrySchedule.DelayAfter(Attempts) is { } delay)
        {
            NextAttemptAt = at + delay;
            return;
        }

        Status = WebhookDeliveryStatus.Failed;
        NextAttemptAt = null;
        CompletedAt = at;
    }

    /// <summary>
    /// Stops a pending delivery without sending it, because its webhook was switched off or deleted.
    /// No attempt is counted. It reads as <c>failed</c> with <paramref name="reason"/> as the error,
    /// so it can be sent again with <see cref="Redeliver"/> once the webhook is back on.
    /// </summary>
    public void Halt(DateTimeOffset at, string reason)
    {
        if (Status != WebhookDeliveryStatus.Pending)
        {
            throw new InvalidOperationException($"delivery {Id} is already {Status}");
        }

        Status = WebhookDeliveryStatus.Failed;
        NextAttemptAt = null;
        CompletedAt = at;
        LastError = Truncate(reason, ErrorMaxLength);
    }

    /// <summary>
    /// Puts a failed delivery back in the queue, due now: the same event, the same id, the same
    /// body. A delivery that used up its retries gets one more attempt and, failing that, is failed
    /// again; one that was halted early resumes the retries it had left. Only a failed delivery can
    /// be sent again, so a second press while the first is queued is refused.
    /// </summary>
    public void Redeliver(DateTimeOffset now)
    {
        if (Status != WebhookDeliveryStatus.Failed)
        {
            throw new DomainException("state_conflict", "ส่งซ้ำได้เฉพาะรายการที่ส่งไม่สำเร็จ");
        }

        Status = WebhookDeliveryStatus.Pending;
        NextAttemptAt = now;
        CompletedAt = null;
    }

    private void Record(DateTimeOffset at, int? statusCode, string? error, string? responseBody, int durationMs)
    {
        if (Status != WebhookDeliveryStatus.Pending)
        {
            throw new InvalidOperationException($"delivery {Id} is already {Status}");
        }

        Attempts++;
        LastAttemptAt = at;
        LastStatusCode = statusCode;
        LastError = Truncate(error, ErrorMaxLength);
        LastResponseBody = Truncate(responseBody, ResponseBodyMaxLength);
        LastDurationMs = Math.Max(0, durationMs);
    }

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
