using System.Security.Cryptography;
using System.Text;
using HappyPaws.Application.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Notifications.Line;

/// <summary>
/// The LINE channel for the Application layer: whether it is set up, whether a webhook is
/// genuine, and replies in the chat.
/// </summary>
internal sealed class LineGateway(
    LineCredentialResolver credentials,
    LineMessagingClient client,
    IOptions<LineOptions> options,
    ILogger<LineGateway> logger) : ILineGateway
{
    public string PortalUrl => options.Value.PortalUrl;

    public async Task<LineConnection> ConnectionAsync(CancellationToken cancellationToken = default) =>
        (await credentials.ResolveAsync(cancellationToken)).ToConnection();

    public async Task<bool> VerifySignatureAsync(
        ReadOnlyMemory<byte> body,
        string? signature,
        CancellationToken cancellationToken = default)
    {
        var secret = (await credentials.ResolveAsync(cancellationToken)).ChannelSecret;

        return secret is not null && LineSignature.IsValid(secret, body.Span, signature);
    }

    public async Task<LineProfile?> ProfileAsync(string lineUserId, CancellationToken cancellationToken = default)
    {
        var token = (await credentials.ResolveAsync(cancellationToken)).AccessToken;

        if (token is null)
        {
            return null;
        }

        try
        {
            return await client.ProfileAsync(token, lineUserId, cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // The follower is still recorded without a name; the counter sees "ไม่ทราบชื่อ".
            logger.LogWarning(error, "LINE profile could not be read");
            return null;
        }
    }

    public async Task ReplyAsync(
        string replyToken,
        string text,
        IReadOnlyList<LineQuickReply>? quickReplies = null,
        CancellationToken cancellationToken = default)
    {
        var token = (await credentials.ResolveAsync(cancellationToken)).AccessToken;

        if (token is null)
        {
            logger.LogWarning("LINE reply not sent: no channel access token is configured");
            return;
        }

        try
        {
            await client.ReplyAsync(token, replyToken, text, quickReplies, cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // A reply token is good for one use and a short time; there is nothing to retry.
            logger.LogError(error, "LINE reply was not delivered");
        }
    }
}

/// <summary>
/// <c>X-Line-Signature</c>: the base64 HMAC-SHA256 of the raw request body, keyed with the channel
/// secret. Compared in constant time, so the time an answer takes says nothing about how much of a
/// forged signature was right.
/// </summary>
internal static class LineSignature
{
    public static string Compute(string channelSecret, ReadOnlySpan<byte> body) =>
        Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(channelSecret), body));

    public static bool IsValid(string channelSecret, ReadOnlySpan<byte> body, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var expected = Encoding.ASCII.GetBytes(Compute(channelSecret, body));
        var given = Encoding.ASCII.GetBytes(signature.Trim());

        return CryptographicOperations.FixedTimeEquals(expected, given);
    }
}
