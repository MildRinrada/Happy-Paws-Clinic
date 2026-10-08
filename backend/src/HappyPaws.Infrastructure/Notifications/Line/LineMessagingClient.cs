using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HappyPaws.Application.Integrations;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Notifications.Line;

/// <summary>LINE refused a call. The reason is LINE's own message, which carries no secret.</summary>
internal sealed class LineApiException(HttpStatusCode status, string reason)
    : HttpRequestException($"LINE refused the request: {(int)status} {reason}", null, status);

/// <summary>
/// The LINE Messaging API: <c>POST /v2/bot/message/push</c> and <c>/v2/bot/message/reply</c>,
/// with the channel access token as a bearer token.
/// <para>
/// A push carries <c>X-Line-Retry-Key</c>, so if a network failure makes the caller unsure whether
/// LINE accepted it, LINE itself drops a repeat with the same key rather than delivering twice.
/// </para>
/// <para>
/// A client per call from the factory, for the same reason as the SMS transport: this class lives
/// as long as the process, and a client kept that long would keep a DNS answer that long too.
/// </para>
/// </summary>
internal sealed class LineMessagingClient(IHttpClientFactory clients, IOptions<LineOptions> options)
{
    public const string ClientName = "line";

    /// <summary>LINE's limits: 5,000 characters of text, 20 of a button label, 13 buttons.</summary>
    private const int TextMax = 5_000;

    private const int LabelMax = 20;

    private const int QuickRepliesMax = 13;

    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public Task PushAsync(
        string accessToken,
        string to,
        string text,
        IReadOnlyList<LineQuickReply>? quickReplies,
        CancellationToken cancellationToken = default) =>
        PostAsync(
            "/v2/bot/message/push",
            accessToken,
            new { to, messages = new[] { Message(text, quickReplies) } },
            Guid.NewGuid(),
            cancellationToken);

    public Task ReplyAsync(
        string accessToken,
        string replyToken,
        string text,
        IReadOnlyList<LineQuickReply>? quickReplies,
        CancellationToken cancellationToken = default) =>
        PostAsync(
            "/v2/bot/message/reply",
            accessToken,
            new { replyToken, messages = new[] { Message(text, quickReplies) } },
            retryKey: null,
            cancellationToken);

    /// <summary>
    /// <c>GET /v2/bot/profile/{userId}</c>: the display name and picture of someone who follows the
    /// account. LINE answers only for a follower; anyone else is a 404, which becomes an exception
    /// here like any other refusal.
    /// </summary>
    public async Task<LineProfile> ProfileAsync(string accessToken, string userId, CancellationToken cancellationToken = default)
    {
        var http = clients.CreateClient(ClientName);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{options.Value.BaseUrl.TrimEnd('/')}/v2/bot/profile/{Uri.EscapeDataString(userId)}");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LineApiException(response.StatusCode, reason[..Math.Min(300, reason.Length)]);
        }

        var profile = await response.Content.ReadFromJsonAsync<ProfileBody>(Json, cancellationToken);

        return new LineProfile(profile?.DisplayName, profile?.PictureUrl);
    }

    /// <summary>The parts of LINE's profile answer kept: no status message, no language.</summary>
    private sealed record ProfileBody(
        [property: JsonPropertyName("displayName")] string? DisplayName,
        [property: JsonPropertyName("pictureUrl")] string? PictureUrl);

    /// <summary>A text message, with its buttons as LINE quick replies of the postback kind.</summary>
    internal static object Message(string text, IReadOnlyList<LineQuickReply>? quickReplies) => new
    {
        type = "text",
        text = text.Length <= TextMax ? text : text[..TextMax],
        quickReply = quickReplies is { Count: > 0 }
            ? new
            {
                items = quickReplies.Take(QuickRepliesMax).Select(reply => new
                {
                    type = "action",
                    action = new
                    {
                        type = "postback",
                        label = reply.Label.Length <= LabelMax ? reply.Label : reply.Label[..LabelMax],
                        data = reply.Data,
                        displayText = reply.Label,
                    },
                }).ToArray(),
            }
            : null,
    };

    private async Task PostAsync(string path, string accessToken, object body, Guid? retryKey, CancellationToken cancellationToken)
    {
        var http = clients.CreateClient(ClientName);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.Value.BaseUrl.TrimEnd('/')}{path}")
        {
            Content = JsonContent.Create(body, options: Json),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (retryKey is { } key)
        {
            request.Headers.Add("X-Line-Retry-Key", key.ToString());
        }

        using var response = await http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LineApiException(response.StatusCode, reason[..Math.Min(300, reason.Length)]);
        }
    }
}
