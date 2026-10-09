using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Integrations;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Integrations.DispatchWebhooks;

/// <summary>
/// What the webhook job runs every few seconds: empty the outbox into deliveries, then send the
/// deliveries that are due.
/// <para>
/// <b>Fan-out.</b> Outbox rows not yet dispatched are locked (<c>FOR UPDATE SKIP LOCKED</c>),
/// and for each one a <see cref="WebhookDelivery"/> is made for every active subscription that
/// wants its type and already existed when the event happened. The row is then marked dispatched,
/// all in one transaction, so an event is fanned out exactly once even with several instances.
/// </para>
/// <para>
/// <b>Sending.</b> Each due delivery is claimed in its own short transaction that locks it and
/// pushes its due time forward by a lease, then sent with no transaction or lock held (a slow
/// receiver must not hold a row lock for ten seconds), then its outcome is written in a second
/// short transaction. A failure schedules the next retry by <see cref="WebhookRetrySchedule"/>;
/// after the last one the delivery is <c>failed</c> for good.
/// </para>
/// </summary>
public sealed class DispatchWebhooksHandler(
    IWebhookRepository webhooks,
    IWebhookSender sender,
    ISecretProtector protector,
    IntegrationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<DispatchWebhooksHandler> logger)
{
    /// <summary>The most outbox rows one fan-out takes.</summary>
    public const int FanOutBatch = 200;

    /// <summary>The most deliveries one run sends, so one instance does not hog a burst.</summary>
    public const int MaxSendsPerRun = 50;

    /// <summary>Fans out, then sends what is due. Returns how many attempts it made.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        await FanOutAsync(cancellationToken);

        var sent = 0;

        while (sent < MaxSendsPerRun && await ClaimAsync(cancellationToken) is { } delivery)
        {
            await SendAsync(delivery, cancellationToken);
            sent++;
        }

        return sent;
    }

    /// <summary>Turns undispatched outbox rows into deliveries. Returns how many deliveries it made.</summary>
    public async Task<int> FanOutAsync(CancellationToken cancellationToken = default)
    {
        var made = 0;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            made = 0;

            var events = await webhooks.ClaimUndispatchedAsync(FanOutBatch, token);
            if (events.Count == 0)
            {
                return;
            }

            var subscriptions = await webhooks.ListActiveAsync(token);
            var now = clock.UtcNow;

            foreach (var webhookEvent in events)
            {
                foreach (var subscription in subscriptions)
                {
                    if (subscription.Receives(webhookEvent.Type) && subscription.CreatedAt <= webhookEvent.OccurredAt)
                    {
                        webhooks.AddDelivery(WebhookDelivery.Queue(subscription.Id, webhookEvent, now));
                        made++;
                    }
                }

                webhookEvent.MarkDispatched(now);
            }
        }, cancellationToken);

        return made;
    }

    /// <summary>Takes the delivery due longest ago and leases it, or returns null. Public so tests can race two workers.</summary>
    public async Task<WebhookDelivery?> ClaimAsync(CancellationToken cancellationToken = default)
    {
        WebhookDelivery? claimed = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;

            claimed = await webhooks.ClaimDueAsync(now, token);
            claimed?.Lease(now + Lease);
        }, cancellationToken);

        return claimed;
    }

    /// <summary>Long enough for one attempt to time out, with room to write its outcome.</summary>
    private TimeSpan Lease => policy.Timeout + TimeSpan.FromMinutes(1);

    private async Task SendAsync(WebhookDelivery claimed, CancellationToken cancellationToken)
    {
        var subscription = await webhooks.FindAsync(claimed.SubscriptionId, cancellationToken);
        var webhookEvent = await webhooks.FindEventAsync(claimed.EventId, cancellationToken);

        // Switched off or deleted since it was queued (docs/contract-extensions.md § 22): stopped
        // here, unsent, and no attempt is counted. Switching off also stops pending rows at once;
        // this catches a row claimed in the moment between.
        var halt = subscription switch
        {
            { IsDeleted: true } => WebhookDelivery.HaltedDeleted,
            { Active: false } => WebhookDelivery.HaltedDisabled,
            _ => null,
        };

        WebhookSendResult result;

        if (halt is not null)
        {
            result = new WebhookSendResult(false, null, halt, null, 0);
        }
        else if (subscription is null || webhookEvent is null)
        {
            result = new WebhookSendResult(false, null, "subscription_missing", null, 0);
        }
        else
        {
            result = await AttemptAsync(subscription, webhookEvent, cancellationToken);
        }

        var at = clock.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var delivery = await webhooks.LoadDeliveryAsync(claimed.Id, token);

            // Another worker recorded this attempt after our lease ran out. Its word stands.
            if (delivery is not { Status: WebhookDeliveryStatus.Pending } || delivery.Attempts != claimed.Attempts)
            {
                return;
            }

            if (halt is not null)
            {
                delivery.Halt(at, halt);
            }
            else if (result.Succeeded)
            {
                delivery.RecordSuccess(at, result.StatusCode ?? 200, result.ResponseBody, result.DurationMs);
            }
            else
            {
                delivery.RecordFailure(at, result.StatusCode, result.Error ?? "failed", result.ResponseBody, result.DurationMs);
            }
        }, cancellationToken);

        if (!result.Succeeded && halt is null)
        {
            logger.LogInformation(
                "Webhook delivery {DeliveryId} ({EventType}) to {SubscriptionId} failed: {Error}",
                claimed.Id,
                claimed.EventType,
                claimed.SubscriptionId,
                result.Error);
        }
    }

    private async Task<WebhookSendResult> AttemptAsync(
        WebhookSubscription subscription,
        WebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        string secret;

        try
        {
            secret = protector.Unprotect(subscription.SecretCiphertext, SecretPurposes.WebhookSigning);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The Data Protection keys that encrypted the secret are gone (a container restarted
            // without a persisted key ring). Unsigned delivery is not an option; say why it failed.
            logger.LogError(exception, "Webhook {SubscriptionId}: its signing secret cannot be decrypted", subscription.Id);
            return new WebhookSendResult(false, null, "secret_unavailable", null, 0);
        }

        return await sender.SendAsync(
            new WebhookSendRequest(
                subscription.Url,
                webhookEvent.Id,
                webhookEvent.Type,
                webhookEvent.Body,
                secret,
                clock.UtcNow),
            cancellationToken);
    }
}
