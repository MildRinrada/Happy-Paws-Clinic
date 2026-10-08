using System.Net;
using System.Text;
using System.Text.Json;
using HappyPaws.Application.Integrations;
using HappyPaws.Infrastructure.Notifications.Line;
using Microsoft.Extensions.Options;

namespace HappyPaws.IntegrationTests.Messaging;

/// <summary>
/// The LINE Messaging API client and the webhook signature, without LINE: what a push looks like on
/// the wire, what a refusal becomes, and the HMAC the webhook is checked with.
/// </summary>
public class LineClientTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static LineMessagingClient Client(RecordingHttpHandler handler) =>
        new(new SingleClientFactory(handler), Options.Create(new LineOptions { BaseUrl = "https://line.test/" }));

    [Fact]
    public async Task A_push_is_a_bearer_call_with_a_retry_key_and_postback_buttons()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.OK);

        await Client(handler).PushAsync(
            "token-1", "Uabc", "ถึงคิว A014 แล้ว", [new LineQuickReply("ดูคิวของฉัน", "action=view_queue&ticket_id=tkt_1")], Token);

        var (request, body) = Assert.Single(handler.Sent);
        Assert.Equal("https://line.test/v2/bot/message/push", request.RequestUri!.ToString());
        Assert.Equal("Bearer token-1", request.Headers.Authorization!.ToString());
        Assert.True(Guid.TryParse(request.Headers.GetValues("X-Line-Retry-Key").Single(), out _));

        var json = JsonDocument.Parse(body).RootElement;
        Assert.Equal("Uabc", json.GetProperty("to").GetString());
        var message = json.GetProperty("messages")[0];
        Assert.Equal("text", message.GetProperty("type").GetString());
        Assert.Equal("ถึงคิว A014 แล้ว", message.GetProperty("text").GetString());

        var action = message.GetProperty("quickReply").GetProperty("items")[0].GetProperty("action");
        Assert.Equal("postback", action.GetProperty("type").GetString());
        Assert.Equal("action=view_queue&ticket_id=tkt_1", action.GetProperty("data").GetString());
    }

    [Fact]
    public async Task A_message_with_no_buttons_has_no_quick_reply_at_all()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.OK);

        await Client(handler).ReplyAsync("token-1", "reply-1", "สวัสดี", null, Token);

        var (request, body) = Assert.Single(handler.Sent);
        Assert.EndsWith("/v2/bot/message/reply", request.RequestUri!.ToString());
        Assert.False(request.Headers.Contains("X-Line-Retry-Key"));
        Assert.False(JsonDocument.Parse(body).RootElement.GetProperty("messages")[0].TryGetProperty("quickReply", out _));
    }

    [Fact]
    public async Task A_refusal_is_an_exception_carrying_the_status()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.TooManyRequests, """{"message":"You have reached your monthly limit."}""");

        var error = await Assert.ThrowsAsync<LineApiException>(() => Client(handler).PushAsync("t", "U1", "x", null, Token));

        Assert.Equal(HttpStatusCode.TooManyRequests, error.StatusCode);
        Assert.Contains("monthly limit", error.Message);
    }

    [Fact]
    public void The_signature_is_the_base64_HMAC_of_the_exact_body()
    {
        var body = Encoding.UTF8.GetBytes("""{"events":[]}""");
        var signature = LineSignature.Compute("secret", body);

        // Computed independently: HMAC-SHA256 keyed with the secret, then base64.
        var expected = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(Encoding.UTF8.GetBytes("secret"), body));
        Assert.Equal(expected, signature);

        Assert.True(LineSignature.IsValid("secret", body, signature));
        Assert.False(LineSignature.IsValid("other-secret", body, signature));
        Assert.False(LineSignature.IsValid("secret", body, null));
        Assert.False(LineSignature.IsValid("secret", body, signature[..^2]));
    }
}
