namespace HappyPaws.Application.Integrations;

/// <summary>Where the LINE credentials in use came from.</summary>
public enum LineCredentialSource
{
    /// <summary>Neither the database nor configuration holds them.</summary>
    None,

    /// <summary>Set through <c>PUT /staff/integrations/line</c>.</summary>
    Database,

    /// <summary>From <c>Messaging:Line:*</c> (environment variables in production).</summary>
    Configuration,
}

/// <summary>
/// Whether the clinic's LINE account can be used right now. <c>CanPush</c> needs an access token
/// and the master switch on; <c>CanVerify</c> needs the channel secret, which the webhook checks
/// every request against.
/// </summary>
public sealed record LineConnection(
    bool CanPush,
    bool CanVerify,
    bool Enabled,
    string? ChannelId,
    LineCredentialSource TokenSource,
    LineCredentialSource SecretSource);

/// <summary>A button under a reply: its label, and the postback data a tap sends back.</summary>
public sealed record LineQuickReply(string Label, string Data);

/// <summary>
/// The LINE Messaging API, as the Application layer needs it: is it set up, is this webhook
/// genuine, and answer a chat event. Pushing notifications is not here; that is a delivery
/// channel behind <see cref="Notifications.INotificationDispatcher"/>, like SMS and email.
/// </summary>
public interface ILineGateway
{
    /// <summary>The customer portal's address, for the things a chat button cannot do.</summary>
    string PortalUrl { get; }

    Task<LineConnection> ConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True when <paramref name="signature"/> is the base64 HMAC-SHA256 of the raw body under the
    /// channel secret. False when it is not, or when no secret is configured.
    /// </summary>
    Task<bool> VerifySignatureAsync(ReadOnlyMemory<byte> body, string? signature, CancellationToken cancellationToken = default);

    /// <summary>Answers an event with a text message. Never throws; a failed reply is logged.</summary>
    Task ReplyAsync(
        string replyToken,
        string text,
        IReadOnlyList<LineQuickReply>? quickReplies = null,
        CancellationToken cancellationToken = default);
}
