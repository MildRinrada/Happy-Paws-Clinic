using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.Webhooks;

/// <summary>
/// Writes the envelope every receiver gets, and adds it to the outbox:
/// <code>
/// { "id": "evt_…", "type": "appointment.created", "occurred_at": "2026-10-09T03:00:00Z",
///   "data": { … } }
/// </code>
/// The body is serialised here, once, and stored as text; every attempt sends and signs those
/// same bytes.
/// </summary>
public sealed class WebhookOutbox(IWebhookRepository webhooks, IClock clock) : IWebhookOutbox
{
    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public void Enqueue(string eventType, object data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var id = EntityId.New(EntityId.WebhookEvent);
        var occurredAt = clock.UtcNow;

        var body = JsonSerializer.Serialize(
            new Envelope(id, eventType, occurredAt.ToUniversalTime(), data),
            Json);

        webhooks.AddEvent(WebhookEvent.Create(id, eventType, body, occurredAt));
    }

    private sealed record Envelope(string Id, string Type, DateTimeOffset OccurredAt, object Data);
}
