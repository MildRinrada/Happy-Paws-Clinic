namespace HappyPaws.Application.Integrations.Webhooks;

/// <summary>One attempt to deliver, as the sender is asked to make it.</summary>
public sealed record WebhookSendRequest(
    string Url,
    string EventId,
    string EventType,
    string Body,
    string Secret,
    DateTimeOffset At);

/// <summary>
/// What came of one attempt. <see cref="Error"/> is a short code: <c>http_503</c>, <c>timeout</c>,
/// <c>redirect_not_followed</c>, <c>address_blocked</c>, <c>connection_failed</c>.
/// <see cref="ResponseBody"/> is already cut to a few hundred characters.
/// </summary>
public sealed record WebhookSendResult(bool Succeeded, int? StatusCode, string? Error, string? ResponseBody, int DurationMs);

/// <summary>
/// Sends one signed delivery over HTTP. Implemented in <c>Infrastructure/Webhooks/</c>, where the
/// network rules live: https, public addresses only (checked at connect time), no redirects
/// followed, a timeout, and a cap on how much of the answer is read.
/// </summary>
public interface IWebhookSender
{
    Task<WebhookSendResult> SendAsync(WebhookSendRequest request, CancellationToken cancellationToken = default);
}
