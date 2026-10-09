using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// An outbound webhook: "send these events to this URL" — <c>POST /staff/webhooks</c>.
/// <para>
/// <b>The signing secret.</b> The server generates it, shows it once in the answer to the create
/// call, and keeps only <see cref="SecretCiphertext"/>: the secret encrypted with the API's Data
/// Protection keys. It cannot be hashed like a password, because the sender needs the secret
/// itself to sign every delivery; encrypting it means a copy of the database alone does not let
/// anyone forge a delivery. It is never returned again.
/// </para>
/// <para>
/// <see cref="Events"/> is an explicit list from <see cref="WebhookEventTypes"/>. There is no
/// "everything" value: a receiver that subscribed to everything would silently start receiving
/// event types added in a later release, some of which may carry data it was never meant to see.
/// </para>
/// </summary>
public sealed class WebhookSubscription : Entity
{
    public const int DescriptionMaxLength = 200;

    private readonly List<string> _events = [];

    private WebhookSubscription()
    {
    }

    public string Url { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IReadOnlyList<string> Events => _events;

    /// <summary>The signing secret, encrypted at rest. Never returned by any endpoint.</summary>
    public string SecretCiphertext { get; private set; } = string.Empty;

    /// <summary>Off, nothing new is queued for it. The contract has no endpoint that turns it off yet.</summary>
    public bool Active { get; private set; } = true;

    public string CreatedByStaffId { get; private set; } = string.Empty;

    public static WebhookSubscription Create(
        string url,
        IEnumerable<string> events,
        string? description,
        string secretCiphertext,
        string createdByStaffId)
    {
        var chosen = events.Select(type => type.Trim()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        if (chosen.Count == 0 || chosen.Any(type => !WebhookEventTypes.All.Contains(type)))
        {
            throw new DomainException("invalid_webhook", "ต้องเลือก event ที่มีอยู่จริงอย่างน้อยหนึ่งรายการ");
        }

        var subscription = new WebhookSubscription
        {
            Id = EntityId.New(EntityId.WebhookSubscription),
            Url = url.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SecretCiphertext = secretCiphertext,
            CreatedByStaffId = createdByStaffId,
        };

        subscription._events.AddRange(chosen);

        return subscription;
    }

    public bool Receives(string eventType) => Active && _events.Contains(eventType, StringComparer.Ordinal);
}
