using System.Security.Cryptography;
using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// One staff member's calendar subscription link — the token behind
/// <c>GET /staff/integrations/calendar/ics?token=…</c>.
/// <para>
/// Calendar apps (Google Calendar, Apple Calendar, Outlook) fetch a feed by URL on their own
/// schedule and cannot send a bearer token, so the URL itself carries a secret. One per staff
/// member, made the first time they ask for their link. It is looked up by
/// <see cref="TokenHash"/>; <see cref="TokenCiphertext"/> is the token encrypted with the API's
/// Data Protection keys, kept so the settings page can show the same link again rather than
/// breaking every calendar that already subscribed.
/// </para>
/// <para>
/// The token alone grants nothing beyond the feed, and the feed is checked against its owner on
/// every fetch: a deactivated staff member's link returns 401.
/// </para>
/// </summary>
public sealed class CalendarFeed : Entity
{
    public const string TokenPrefix = "hpcal_";

    private CalendarFeed()
    {
    }

    public string StaffId { get; private set; } = string.Empty;

    public string TokenHash { get; private set; } = string.Empty;

    public string TokenCiphertext { get; private set; } = string.Empty;

    public DateTimeOffset? LastUsedAt { get; private set; }

    public static string NewToken() =>
        TokenPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static CalendarFeed Create(string staffId, string token, string tokenCiphertext) =>
        new()
        {
            Id = EntityId.New(EntityId.CalendarFeed),
            StaffId = staffId,
            TokenHash = ApiKey.Hash(token),
            TokenCiphertext = tokenCiphertext,
        };

    public void MarkUsed(DateTimeOffset now) => LastUsedAt = now;
}
