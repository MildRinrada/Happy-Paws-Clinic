using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// The clinic's own LINE Official Account — <c>PUT /staff/integrations/line</c>,
/// "channel id/secret · เปิดปิดชนิดการแจ้งเตือน".
/// <para>
/// One row. The contract says the account is the clinic's own ("LINE Official Account ของคลินิก
/// เอง · ไม่ใช่บัญชีกลางของแพลตฟอร์ม"), so the credentials are the clinic's to set, not the
/// operator's. Every credential may also come from configuration
/// (<c>Messaging:Line:*</c>); a value stored here wins over it.
/// </para>
/// <para>
/// <b>The secret and the access token are stored as given.</b> They are never returned by any
/// endpoint, but they sit in the database in plain text, as the TOTP secrets do. Encrypting them
/// at rest is listed as open work in <c>docs/system-overview.md</c>.
/// </para>
/// <para>
/// Notification types are switched off by listing them, not on by listing them. A template added
/// in a later release then goes out on LINE by default, which is what a clinic that turned LINE
/// on expects; one that wants it off says so.
/// </para>
/// </summary>
public sealed class LineChannelSettings : Entity
{
    public const string SingletonId = "set_line";

    /// <summary>
    /// The monthly cap on broadcast messages. LINE's free plan in Thailand allows a few hundred
    /// messages a month across everything the account sends, so the default leaves the queue
    /// calls and receipts room.
    /// </summary>
    public const int DefaultBroadcastMonthlyLimit = 300;

    public const int BroadcastMonthlyLimitMax = 1_000_000;

    private readonly List<string> _disabledTemplates = [];

    private LineChannelSettings()
    {
    }

    /// <summary>The numeric channel id from the LINE Developers console. Shown back; not a secret.</summary>
    public string? ChannelId { get; private set; }

    /// <summary>Signs the webhook. Never returned.</summary>
    public string? ChannelSecret { get; private set; }

    /// <summary>The long-lived channel access token that authorises a push. Never returned.</summary>
    public string? ChannelAccessToken { get; private set; }

    /// <summary>The master switch. Off, nothing goes out on LINE and every message takes another channel.</summary>
    public bool Enabled { get; private set; } = true;

    /// <summary>Template keys that must not go out on LINE.</summary>
    public IReadOnlyList<string> DisabledTemplates => _disabledTemplates;

    public int BroadcastMonthlyLimit { get; private set; } = DefaultBroadcastMonthlyLimit;

    public static LineChannelSettings CreateDefault() => new() { Id = SingletonId };

    public bool IsTemplateEnabled(string templateKey) => !_disabledTemplates.Contains(templateKey, StringComparer.Ordinal);

    /// <summary>
    /// Applies a partial update; null leaves a value alone. An empty string clears a credential,
    /// which hands it back to configuration.
    /// </summary>
    public void Update(
        string? channelId = null,
        string? channelSecret = null,
        string? channelAccessToken = null,
        bool? enabled = null,
        IReadOnlyCollection<string>? disabledTemplates = null,
        int? broadcastMonthlyLimit = null)
    {
        if (channelId is not null)
        {
            var id = channelId.Trim();

            if (id.Length > 20 || !id.All(char.IsAsciiDigit))
            {
                throw new DomainException("invalid_line_settings", "channel id ต้องเป็นตัวเลขไม่เกิน 20 หลัก");
            }

            ChannelId = id.Length == 0 ? null : id;
        }

        if (channelSecret is not null)
        {
            ChannelSecret = Credential(channelSecret, 200, "channel secret");
        }

        if (channelAccessToken is not null)
        {
            ChannelAccessToken = Credential(channelAccessToken, 1_000, "channel access token");
        }

        Enabled = enabled ?? Enabled;

        if (disabledTemplates is not null)
        {
            _disabledTemplates.Clear();
            _disabledTemplates.AddRange(disabledTemplates.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        }

        if (broadcastMonthlyLimit is { } limit)
        {
            if (limit is < 0 or > BroadcastMonthlyLimitMax)
            {
                throw new DomainException("invalid_line_settings", "โควตาประกาศต่อเดือนต้องอยู่ระหว่าง 0 ถึง 1,000,000");
            }

            BroadcastMonthlyLimit = limit;
        }
    }

    private static string? Credential(string value, int max, string name)
    {
        var trimmed = value.Trim();

        if (trimmed.Length > max || trimmed.Any(char.IsWhiteSpace))
        {
            throw new DomainException("invalid_line_settings", $"{name} ไม่ถูกต้อง");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }
}
