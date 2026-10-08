namespace HappyPaws.Infrastructure.Notifications.Line;

/// <summary>
/// <c>Messaging:Line</c> — the clinic's LINE Official Account (Messaging API channel).
/// <para>
/// The three credentials can also be set from the console with <c>PUT /staff/integrations/line</c>;
/// a value stored there wins over the one here. Set the secret and the token from the environment
/// (<c>Messaging__Line__ChannelSecret</c>, <c>Messaging__Line__ChannelAccessToken</c>), never in a
/// committed file. With no access token anywhere, nothing is pushed to LINE and messages take SMS or
/// email, or the log when neither is set up.
/// </para>
/// </summary>
public sealed class LineOptions
{
    public const string SectionName = "Messaging:Line";

    public string? ChannelId { get; init; }

    /// <summary>Checks <c>X-Line-Signature</c> on the webhook.</summary>
    public string? ChannelSecret { get; init; }

    /// <summary>The long-lived channel access token from the LINE Developers console.</summary>
    public string? ChannelAccessToken { get; init; }

    /// <summary>Replaced in tests by a fake server.</summary>
    public string BaseUrl { get; init; } = "https://api.line.me";

    /// <summary>Where a chat reply sends an owner for what a button cannot do.</summary>
    public string PortalUrl { get; init; } = "http://localhost:3000/portal";
}
