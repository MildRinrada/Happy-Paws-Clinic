using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using HappyPaws.Application.Notifications;
using HappyPaws.Infrastructure.Messaging.Email;
using HappyPaws.Infrastructure.Messaging.Sms;
using HappyPaws.Infrastructure.Notifications;
using HappyPaws.Infrastructure.Notifications.Line;
using HappyPaws.Infrastructure.Notifications.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// A clinic whose messages really go out — through the production router, the LINE HTTP client and
/// the delivery log — to stand-ins at the very edge: a fake LINE server, and SMS and email
/// transports that record what they were handed.
/// <para>
/// Its own container, unlike <see cref="ApiCollection"/>, whose channel is replaced wholesale by
/// <see cref="RecordingNotificationChannel"/>. The LINE settings are one row per clinic, and the
/// tests here change them; doing that under two hundred unrelated tests would make those depend on
/// execution order.
/// </para>
/// </summary>
public sealed class DeliveryFixture : ApiFixture
{
    internal const string LineBaseUrl = "http://line.test";
    internal const string PortalUrl = "http://portal.test/portal";

    internal FakeLineServer Line { get; } = new();

    internal FakeTransports Transports { get; } = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        // The real channel back in place of the recording one the shared wiring installs.
        services.RemoveAll<INotificationChannel>();
        services.AddScoped<INotificationChannel, DeliveringNotificationChannel>();

        services.RemoveAll<IDeliveryTransports>();
        services.AddSingleton<IDeliveryTransports>(Transports);

        services.AddSingleton<IOptions<LineOptions>>(Options.Create(new LineOptions { BaseUrl = LineBaseUrl, PortalUrl = PortalUrl }));
        services.AddHttpClient(LineMessagingClient.ClientName)
            .ConfigurePrimaryHttpMessageHandler(() => Line)
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
    }
}

[CollectionDefinition(nameof(DeliveryCollection))]
public sealed class DeliveryCollection : ICollectionFixture<DeliveryFixture>;

/// <summary>One call the fake LINE server received.</summary>
internal sealed record LineCall(string Path, string? Authorization, string? RetryKey, JsonElement Body)
{
    /// <summary>The <c>to</c> of a push.</summary>
    public string? To => Body.TryGetProperty("to", out var to) ? to.GetString() : null;

    public string Text => Body.GetProperty("messages")[0].GetProperty("text").GetString()!;

    /// <summary>The postback data of each chat button under the message.</summary>
    public IReadOnlyList<string> Buttons =>
        Body.GetProperty("messages")[0].TryGetProperty("quickReply", out var quick)
            ? [.. quick.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("action").GetProperty("data").GetString()!)]
            : [];
}

/// <summary>Answers like the LINE Messaging API: 200 by default, or whatever a test sets.</summary>
internal sealed class FakeLineServer : HttpMessageHandler
{
    private readonly ConcurrentQueue<LineCall> _calls = new();

    /// <summary>The status every call is answered with until changed.</summary>
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    public IReadOnlyList<LineCall> Calls => [.. _calls];

    public IReadOnlyList<LineCall> PushesTo(string lineUserId) =>
        [.. _calls.Where(call => call.Path == "/v2/bot/message/push" && call.To == lineUserId)];

    public IReadOnlyList<LineCall> Replies => [.. _calls.Where(call => call.Path == "/v2/bot/message/reply")];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync(cancellationToken);

        _calls.Enqueue(new LineCall(
            request.RequestUri!.AbsolutePath,
            request.Headers.Authorization?.ToString(),
            request.Headers.TryGetValues("X-Line-Retry-Key", out var keys) ? keys.First() : null,
            JsonDocument.Parse(body).RootElement.Clone()));

        var answer = Status == HttpStatusCode.OK && request.RequestUri.AbsolutePath.StartsWith(ProfilePath, StringComparison.Ordinal)
            ? JsonSerializer.Serialize(ProfileOf(Uri.UnescapeDataString(request.RequestUri.AbsolutePath[ProfilePath.Length..])))
            : Status == HttpStatusCode.OK ? "{}" : """{"message":"refused by the fake"}""";

        return new HttpResponseMessage(Status) { Content = new StringContent(answer) };
    }

    private const string ProfilePath = "/v2/bot/profile/";

    /// <summary>The profile the fake gives every follower: a name made from the id, so a test can search for it.</summary>
    public static object ProfileOf(string userId) => new
    {
        userId,
        displayName = DisplayNameOf(userId),
        pictureUrl = $"https://profile.line-scdn.net/{userId}",
        statusMessage = "ไม่ถูกเก็บ",
    };

    public static string DisplayNameOf(string userId) => $"ผู้ติดตาม {userId[^8..]}";

    /// <summary>The profile reads LINE was asked for.</summary>
    public IReadOnlyList<LineCall> ProfileReads => [.. _calls.Where(call => call.Path.StartsWith(ProfilePath, StringComparison.Ordinal))];
}

/// <summary>SMS and email that record what they are handed, each switchable on, off, or failing.</summary>
internal sealed class FakeTransports : IDeliveryTransports, ISmsTransport, IEmailTransport
{
    private readonly ConcurrentQueue<(string Phone, string Text)> _sms = new();
    private readonly ConcurrentQueue<EmailMessage> _email = new();

    public bool SmsConfigured { get; set; } = true;

    public bool EmailConfigured { get; set; } = true;

    public bool SmsFails { get; set; }

    public ISmsTransport? Sms => SmsConfigured ? this : null;

    public IEmailTransport? Email => EmailConfigured ? this : null;

    public IReadOnlyList<string> SmsTo(string phoneDigits) => [.. _sms.Where(sent => sent.Phone == phoneDigits).Select(sent => sent.Text)];

    public IReadOnlyList<EmailMessage> EmailTo(string address) => [.. _email.Where(sent => sent.To == address)];

    public Task SendAsync(string phoneDigits, string text, CancellationToken cancellationToken = default)
    {
        if (SmsFails)
        {
            throw new HttpRequestException("gateway refused", null, HttpStatusCode.BadRequest);
        }

        _sms.Enqueue((phoneDigits, text));
        return Task.CompletedTask;
    }

    public Task SendAsync(EmailMessage message, string fromName, CancellationToken cancellationToken = default)
    {
        _email.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>Back to the state every test starts from: both on, nothing failing.</summary>
    public void Reset()
    {
        SmsConfigured = true;
        EmailConfigured = true;
        SmsFails = false;
    }
}
