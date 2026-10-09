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

    /// <summary>
    /// Off, nothing new is queued for it and nothing already queued is sent. Switched with
    /// <c>PATCH /staff/webhooks/{id}</c> (<c>docs/contract-extensions.md § 22</c>).
    /// </summary>
    public bool Active { get; private set; } = true;

    /// <summary>
    /// Deleted by <c>DELETE /staff/webhooks/{id}</c>. The row stays so its delivery log stays
    /// readable, but its secret is destroyed and it is off for good. There is no <c>deleted_at</c>
    /// column (no migration in that change), so an empty <see cref="SecretCiphertext"/> is the
    /// marker: a live webhook always has one, and a webhook without one could not sign anything.
    /// </summary>
    public bool IsDeleted => SecretCiphertext.Length == 0;

    public string CreatedByStaffId { get; private set; } = string.Empty;

    public static WebhookSubscription Create(
        string url,
        IEnumerable<string> events,
        string? description,
        string secretCiphertext,
        string createdByStaffId)
    {
        var chosen = Normalize(events);

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

    /// <summary>A new destination. The caller has checked it with <see cref="WebhookUrl"/>, as on create.</summary>
    public void ChangeUrl(string url)
    {
        EnsureNotDeleted();
        Url = url.Trim();
    }

    public void ChangeEvents(IEnumerable<string> events)
    {
        EnsureNotDeleted();
        var chosen = Normalize(events);
        _events.Clear();
        _events.AddRange(chosen);
    }

    /// <summary>Blank clears it.</summary>
    public void ChangeDescription(string? description)
    {
        EnsureNotDeleted();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetActive(bool active)
    {
        EnsureNotDeleted();
        Active = active;
    }

    /// <summary>Replaces the signing secret. The old one stops signing at once; there is no overlap.</summary>
    public void RotateSecret(string secretCiphertext)
    {
        EnsureNotDeleted();

        if (string.IsNullOrEmpty(secretCiphertext))
        {
            throw new DomainException("invalid_webhook", "secret ใหม่ว่างไม่ได้");
        }

        SecretCiphertext = secretCiphertext;
    }

    /// <summary>
    /// Off for good: the secret is destroyed, and the query string goes from the URL, since it can
    /// hold the receiver's own token. What is left (where it pointed, which events) is what the
    /// delivery log and the audit log need.
    /// </summary>
    public void Delete()
    {
        EnsureNotDeleted();

        Active = false;
        SecretCiphertext = string.Empty;

        if (Uri.TryCreate(Url, UriKind.Absolute, out var uri))
        {
            Url = uri.GetLeftPart(UriPartial.Path);
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException("state_conflict", "webhook นี้ถูกลบแล้ว");
        }
    }

    private static List<string> Normalize(IEnumerable<string> events)
    {
        var chosen = events.Select(type => type.Trim()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        if (chosen.Count == 0 || chosen.Any(type => !WebhookEventTypes.All.Contains(type)))
        {
            throw new DomainException("invalid_webhook", "ต้องเลือก event ที่มีอยู่จริงอย่างน้อยหนึ่งรายการ");
        }

        return chosen;
    }
}
