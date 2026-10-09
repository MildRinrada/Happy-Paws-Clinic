using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using HappyPaws.Application.Integrations;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Infrastructure.Webhooks;

/// <summary>
/// Sends one signed delivery: <c>POST</c> of the stored body, with
/// <c>X-Signature: sha256=…</c>, <c>X-Webhook-Timestamp</c>, <c>X-Webhook-Id</c> (the event id,
/// the same on every retry) and <c>X-Webhook-Event</c>.
/// <para>
/// Before anything is built, the URL is checked again (https, or http to localhost in
/// Development) and its host resolved and checked (<see cref="WebhookAddressGuard"/>); the handler
/// checks once more at connect time. The whole attempt is bounded by the policy timeout. Only the
/// first <see cref="MaxResponseBytes"/> bytes of the answer are read, and only
/// <see cref="WebhookDelivery.ResponseBodyMaxLength"/> characters are kept: the receiver is outside
/// our control, and its answer is stored for people to read.
/// </para>
/// </summary>
internal sealed class WebhookHttpSender(
    IHttpClientFactory clients,
    WebhookAddressGuard guard,
    IntegrationPolicy policy) : IWebhookSender
{
    public const string ClientName = "webhooks";

    public const int MaxResponseBytes = 4_096;

    public async Task<WebhookSendResult> SendAsync(WebhookSendRequest request, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();

        if (WebhookUrl.Problem(request.Url, policy.AllowLocalhost) is not null)
        {
            return Failure("address_blocked", watch);
        }

        var uri = new Uri(request.Url);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(policy.Timeout);

        try
        {
            await guard.CheckAsync(uri.Host, timeout.Token);

            using var message = Build(uri, request);
            var client = clients.CreateClient(ClientName);
            client.Timeout = Timeout.InfiniteTimeSpan;

            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            var status = (int)response.StatusCode;
            var body = await ReadBoundedAsync(response.Content, timeout.Token);

            if (status is >= 200 and < 300)
            {
                return new WebhookSendResult(true, status, null, body, Elapsed(watch));
            }

            var error = status is >= 300 and < 400 ? "redirect_not_followed" : $"http_{status}";
            return new WebhookSendResult(false, status, error, body, Elapsed(watch));
        }
        catch (WebhookAddressBlockedException)
        {
            return Failure("address_blocked", watch);
        }
        catch (HttpRequestException exception) when (exception.InnerException is WebhookAddressBlockedException)
        {
            return Failure("address_blocked", watch);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure("timeout", watch);
        }
        catch (HttpRequestException)
        {
            return Failure("connection_failed", watch);
        }
    }

    private static HttpRequestMessage Build(Uri uri, WebhookSendRequest request)
    {
        var timestamp = request.At.ToUnixTimeSeconds();

        var message = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(request.Body, Encoding.UTF8, "application/json"),
        };

        message.Headers.UserAgent.Add(new ProductInfoHeaderValue("HappyPaws-Webhooks", "1.0"));
        message.Headers.Add(WebhookSignature.Header, WebhookSignature.Sign(request.Secret, timestamp, request.Body));
        message.Headers.Add(WebhookSignature.TimestampHeader, timestamp.ToString(CultureInfo.InvariantCulture));
        message.Headers.Add(WebhookSignature.IdHeader, request.EventId);
        message.Headers.Add(WebhookSignature.EventHeader, request.EventType);

        return message;
    }

    private static async Task<string?> ReadBoundedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[MaxResponseBytes];
        var read = 0;

        while (read < buffer.Length)
        {
            var count = await stream.ReadAsync(buffer.AsMemory(read), cancellationToken);
            if (count == 0)
            {
                break;
            }

            read += count;
        }

        if (read == 0)
        {
            return null;
        }

        var text = Encoding.UTF8.GetString(buffer, 0, read);

        return text.Length <= WebhookDelivery.ResponseBodyMaxLength ? text : text[..WebhookDelivery.ResponseBodyMaxLength];
    }

    private static WebhookSendResult Failure(string error, Stopwatch watch) => new(false, null, error, null, Elapsed(watch));

    private static int Elapsed(Stopwatch watch) => (int)Math.Min(int.MaxValue, watch.ElapsedMilliseconds);
}
