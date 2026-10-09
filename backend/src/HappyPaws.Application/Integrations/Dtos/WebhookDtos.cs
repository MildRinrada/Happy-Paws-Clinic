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

/// <summary>
/// A webhook as <c>GET /staff/webhooks</c> and <c>PATCH /staff/webhooks/{id}</c> show it
/// (<c>docs/contract-extensions.md § 22</c>). No secret, ever: not even whether it can be read.
/// <see cref="Version"/> goes back as <c>If-Match</c>.
/// </summary>
public sealed record WebhookDto(
    string Id,
    string Url,
    IReadOnlyList<string> Events,
    string? Description,
    bool Active,
    uint Version,
    string CreatedByStaffId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static WebhookDto From(WebhookSubscription subscription) =>
        new(
            subscription.Id,
            subscription.Url,
            subscription.Events,
            subscription.Description,
            subscription.Active,
            subscription.Version,
            subscription.CreatedByStaffId,
            subscription.CreatedAt,
            subscription.UpdatedAt);
}

/// <summary>
/// The answer to <c>POST /staff/webhooks/{id}/rotate-secret</c>: the new secret, shown this once,
/// as on create. The old secret stopped signing the moment this was made.
/// </summary>
public sealed record RotatedWebhookSecretDto(
    string Id,
    string Secret,
    WebhookSigningDto Signing,
    uint Version,
    DateTimeOffset RotatedAt);

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
