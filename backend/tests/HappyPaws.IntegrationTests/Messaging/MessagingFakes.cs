using System.Net;
using HappyPaws.Application.Clinics;
using HappyPaws.Domain.Common;
using HappyPaws.Infrastructure.Messaging;
using HappyPaws.Infrastructure.Messaging.Email;
using HappyPaws.Infrastructure.Messaging.Sms;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Messaging;

/// <summary>An HTTP handler that records each request and answers with a fixed status.</summary>
internal sealed class RecordingHttpHandler(HttpStatusCode status = HttpStatusCode.Created, string body = "{}") : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string Body)> Sent { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Sent.Add((request, request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
        return new HttpResponseMessage(status) { Content = new StringContent(body) };
    }
}

internal sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

internal sealed class RecordingEmail(bool fail = false) : IEmailTransport
{
    public List<EmailMessage> Sent { get; } = [];

    public Task SendAsync(EmailMessage message, string fromName, CancellationToken cancellationToken = default)
    {
        if (fail) throw new InvalidOperationException("mail server refused");
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingSms(bool fail = false) : ISmsTransport
{
    public List<(string Phone, string Text)> Sent { get; } = [];

    public Task SendAsync(string phoneDigits, string text, CancellationToken cancellationToken = default)
    {
        if (fail) throw new HttpRequestException("gateway refused");
        Sent.Add((phoneDigits, text));
        return Task.CompletedTask;
    }
}

/// <summary>A settings store with nothing written yet: every read is the default row.</summary>
internal sealed class EmptySettingsStore : IClinicSettingsStore
{
    public Task<TSettings> ReadAsync<TSettings>(string id, Func<TSettings> createDefault, CancellationToken cancellationToken = default)
        where TSettings : Entity => Task.FromResult(createDefault());

    public Task<TSettings> LoadForUpdateAsync<TSettings>(string id, Func<TSettings> createDefault, CancellationToken cancellationToken = default)
        where TSettings : Entity => Task.FromResult(createDefault());

    public static ClinicNameReader Reader()
    {
        var services = new ServiceCollection();
        services.AddScoped<IClinicSettingsStore, EmptySettingsStore>();

        return new ClinicNameReader(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());
    }
}
