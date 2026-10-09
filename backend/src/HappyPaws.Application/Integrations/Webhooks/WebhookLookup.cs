using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.Webhooks;

/// <summary>Finding a webhook to change (<c>docs/contract-extensions.md § 22</c>).</summary>
public static class WebhookLookup
{
    /// <summary>
    /// The webhook, tracked, or <c>404</c> when there is none or it was deleted. A deleted webhook
    /// keeps its row for its delivery log, but nothing can be changed on it any more.
    /// </summary>
    public static async Task<WebhookSubscription> LoadLiveAsync(
        this IWebhookRepository webhooks,
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        var subscription = await webhooks.LoadAsync(subscriptionId, cancellationToken);

        return subscription is { IsDeleted: false }
            ? subscription
            : throw new NotFoundException("webhook", subscriptionId);
    }

    /// <summary>Scheme, host and path: where events go, without a query string that may hold the receiver's token.</summary>
    public static string Destination(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Path) : string.Empty;
}
