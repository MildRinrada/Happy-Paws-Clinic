using HappyPaws.Application.Integrations.DispatchWebhooks;

namespace HappyPaws.Infrastructure.Jobs;

/// <summary>Each tick: turn the webhook outbox into deliveries, then send the ones that are due.</summary>
public sealed class WebhookJob(DispatchWebhooksHandler handler) : IScheduledJob
{
    public Task RunAsync(CancellationToken cancellationToken) => handler.RunAsync(cancellationToken);
}
