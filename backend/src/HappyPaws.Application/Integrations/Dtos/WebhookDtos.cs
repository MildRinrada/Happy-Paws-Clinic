using HappyPaws.Application.Common;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.Dtos;

/// <summary>
/// The answer to <c>POST /staff/webhooks</c>. <see cref="Secret"/> appears here and nowhere else,
/// ever; <see cref="Signing"/> says how to check it.
/// </summary>
public sealed record CreatedWebhookDto(
    string Id,
    string Url,
    IReadOnlyList<string> Events,
    string? Description,
    bool Active,
    string Secret,
    WebhookSigningDto Signing,
    DateTimeOffset CreatedAt)
{
    public static CreatedWebhookDto From(WebhookSubscription subscription, string secret) =>
        new(
            subscription.Id,
            subscription.Url,
            subscription.Events,
            subscription.Description,
            subscription.Active,
            secret,
            WebhookSigningDto.Instance,
            subscription.CreatedAt == default ? DateTimeOffset.UtcNow : subscription.CreatedAt);
}

/// <summary>How a receiver verifies a delivery: HMAC-SHA256 of <c>timestamp.body</c>.</summary>
public sealed record WebhookSigningDto(string Algorithm, string Header, string TimestampHeader, string SignedPayload)
{
    public static WebhookSigningDto Instance { get; } = new(
        "HMAC-SHA256",
        WebhookSignature.Header,
        WebhookSignature.TimestampHeader,
        "<timestamp>.<body>");
}

/// <summary>One row of <c>GET /staff/webhooks/{id}/deliveries</c>: an event, and how its latest attempt went.</summary>
public sealed record WebhookDeliveryDto(
    string Id,
    string EventId,
    string EventType,
    string Status,
    int Attempts,
    int MaxAttempts,
    DateTimeOffset? NextAttemptAt,
    DateTimeOffset? LastAttemptAt,
    int? LastStatusCode,
    string? LastError,
    string? LastResponseBody,
    int? LastDurationMs,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt)
{
    public static WebhookDeliveryDto From(WebhookDelivery delivery) =>
        new(
            delivery.Id,
            delivery.EventId,
            delivery.EventType,
            WireEnum.Of(delivery.Status),
            delivery.Attempts,
            WebhookRetrySchedule.MaxAttempts,
            delivery.NextAttemptAt,
            delivery.LastAttemptAt,
            delivery.LastStatusCode,
            delivery.LastError,
            delivery.LastResponseBody,
            delivery.LastDurationMs,
            delivery.CompletedAt,
            delivery.CreatedAt);
}
