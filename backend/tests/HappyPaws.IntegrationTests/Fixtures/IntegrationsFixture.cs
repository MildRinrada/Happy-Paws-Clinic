using System.Collections.Concurrent;
using System.Net;
using HappyPaws.Application.Integrations;
using HappyPaws.Application.Integrations.DispatchWebhooks;
using HappyPaws.Infrastructure.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// A clinic whose webhooks really go out, through the production sender and its address checks,
/// to a fake receiver at the edge; and whose name lookups are scripted, so a test can make a host
/// name resolve to a private address.
/// <para>
/// Its own container. The payment and lab settings are one row per clinic, and webhook
/// subscriptions receive every matching event in the database, so these tests keep to themselves.
/// Production network rules apply: no localhost, even though the host runs as Development, and a
/// one-second timeout so the timeout test is quick. API keys get a rate limit of five a minute, so
/// the limit can be reached in a test.
/// </para>
/// </summary>
public sealed class IntegrationsFixture : ApiFixture
{
    public const int ApiKeyPerMinute = 5;

    internal FakeWebhookReceiver Receiver { get; } = new();

    internal ScriptedResolver Resolver { get; } = new();

    protected override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>
    {
        ["RateLimits:PerApiKeyPerMinute"] = ApiKeyPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["DataProtection:KeysDirectory"] = Path.Combine(Path.GetTempPath(), "happypaws-tests", "dp-keys", Guid.NewGuid().ToString("N")),
    };

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<IntegrationPolicy>();
        services.AddSingleton(new IntegrationPolicy(AllowLocalhost: false, Timeout: TimeSpan.FromSeconds(1)));

        services.RemoveAll<IWebhookAddressResolver>();
        services.AddSingleton<IWebhookAddressResolver>(Resolver);

        services.AddHttpClient("webhooks")
            .ConfigurePrimaryHttpMessageHandler(() => Receiver)
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
    }

    /// <summary>Runs the webhook job once, as the scheduler would.</summary>
    public async Task<int> DispatchAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DispatchWebhooksHandler>().RunAsync(cancellationToken);
    }

    /// <summary>Runs the job until a run sends nothing, so every due delivery has had its attempt.</summary>
    public async Task DrainAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 20 && await DispatchAsync(cancellationToken) > 0; i++)
        {
        }
    }
}

[CollectionDefinition(nameof(IntegrationsCollection))]
public sealed class IntegrationsCollection : ICollectionFixture<IntegrationsFixture>;

/// <summary>One request the fake receiver got.</summary>
internal sealed record ReceivedWebhook(string Host, string Path, IReadOnlyDictionary<string, string> Headers, string Body);

/// <summary>
/// Answers webhook deliveries. 204 by default; per host, a test can set a status, a body, a
/// redirect or a delay.
/// </summary>
internal sealed class FakeWebhookReceiver : HttpMessageHandler
{
    private readonly ConcurrentQueue<ReceivedWebhook> _received = new();
    private readonly ConcurrentDictionary<string, Func<HttpResponseMessage>> _answers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TimeSpan> _delays = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ReceivedWebhook> To(string host) => [.. _received.Where(item => item.Host.Equals(host, StringComparison.OrdinalIgnoreCase))];

    public void Answer(string host, Func<HttpResponseMessage> answer) => _answers[host] = answer;

    public void Delay(string host, TimeSpan delay) => _delays[host] = delay;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var host = request.RequestUri!.Host;
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

        var headers = request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .ToDictionary(header => header.Key, header => string.Join(",", header.Value), StringComparer.OrdinalIgnoreCase);

        _received.Enqueue(new ReceivedWebhook(host, request.RequestUri.AbsolutePath, headers, body));

        if (_delays.TryGetValue(host, out var delay))
        {
            await Task.Delay(delay, cancellationToken);
        }

        return _answers.TryGetValue(host, out var answer) ? answer() : new HttpResponseMessage(HttpStatusCode.NoContent);
    }
}

/// <summary>Name lookups a test controls. Anything not scripted resolves to a public address.</summary>
internal sealed class ScriptedResolver : IWebhookAddressResolver
{
    public static readonly IPAddress Public = IPAddress.Parse("93.184.216.34");

    private readonly ConcurrentDictionary<string, IPAddress[]> _names = new(StringComparer.OrdinalIgnoreCase);

    public void Map(string host, params string[] addresses) => _names[host] = [.. addresses.Select(IPAddress.Parse)];

    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken = default) =>
        Task.FromResult(_names.TryGetValue(host, out var addresses) ? addresses : [Public]);
}
