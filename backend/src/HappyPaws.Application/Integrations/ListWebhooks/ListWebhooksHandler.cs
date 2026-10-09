using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.Webhooks;

namespace HappyPaws.Application.Integrations.ListWebhooks;

/// <summary>
/// <c>GET /staff/webhooks</c> — every webhook that is not deleted, newest first, switched off ones
/// included. Not in the API reference; see <c>docs/contract-extensions.md § 22</c>. No secret.
/// </summary>
public sealed class ListWebhooksHandler(IWebhookRepository webhooks)
{
    public async Task<IReadOnlyList<WebhookDto>> HandleAsync(CancellationToken cancellationToken = default) =>
        [.. (await webhooks.ListAsync(cancellationToken)).Select(WebhookDto.From)];
}
