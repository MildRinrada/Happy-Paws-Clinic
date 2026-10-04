using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Messaging.Sms;

/// <summary>Hands a text message to an SMS gateway. Throws when the gateway refuses; callers decide what that means.</summary>
internal interface ISmsTransport
{
    Task SendAsync(string phoneDigits, string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// ThaiBulkSMS, API v2: <c>POST /sms</c> with basic authentication (key and secret) and a form
/// body of <c>msisdn</c>, <c>message</c> and optionally <c>sender</c>. A Thai number goes as the
/// customer typed it, <c>0812345678</c>, which the gateway accepts as well as <c>66812345678</c>.
/// <para>
/// A client per send from the factory: the senders that hold this transport live as long as the
/// process, and a client kept that long would keep a DNS answer that long too.
/// </para>
/// </summary>
internal sealed class ThaiBulkSmsTransport(IHttpClientFactory clients, IOptions<SmsOptions> options) : ISmsTransport
{
    public const string ClientName = "thaibulksms";

    public async Task SendAsync(string phoneDigits, string text, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var http = clients.CreateClient(ClientName);

        var form = new Dictionary<string, string> { ["msisdn"] = phoneDigits, ["message"] = text };

        if (!string.IsNullOrWhiteSpace(settings.Sender))
        {
            form["sender"] = settings.Sender;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/sms")
        {
            Content = new FormUrlEncodedContent(form),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.ApiKey}:{settings.ApiSecret}")));

        using var response = await http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // The body names the reason (no credit, bad sender name, bad number) and carries no secret.
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"ThaiBulkSMS refused the message: {(int)response.StatusCode} {reason[..Math.Min(300, reason.Length)]}",
                null,
                response.StatusCode);
        }
    }
}
