using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.RotateWebhookSecret;

/// <summary>
/// <c>POST /staff/webhooks/{id}/rotate-secret</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 22</c>.
/// </summary>
/// <param name="SubscriptionId">The webhook.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record RotateWebhookSecretCommand(string SubscriptionId, uint? IfMatchVersion = null);

/// <summary>
/// Gives a webhook a new signing secret and shows it once, as create does.
/// <para>
/// <b>No overlap.</b> The old secret stops signing at once: there is one secret column and no
/// migration for a second. A delivery the receiver rejects in between is retried on the usual
/// schedule (1, 5, 25 minutes…), so a receiver updated within that window loses nothing. Works on
/// a webhook that is switched off, so the secret can be replaced before it is switched back on.
/// Audited without the secret.
/// </para>
/// </summary>
public sealed class RotateWebhookSecretHandler(
    IWebhookRepository webhooks,
    ISecretProtector protector,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<RotatedWebhookSecretDto> HandleAsync(RotateWebhookSecretCommand command, CancellationToken cancellationToken = default)
    {
        var secret = WebhookSignature.NewSecret();
        WebhookSubscription? rotated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var subscription = await webhooks.LoadLiveAsync(command.SubscriptionId, token);

            ConcurrencyGuard.Ensure(subscription, command.IfMatchVersion);

            subscription.RotateSecret(protector.Protect(secret, SecretPurposes.WebhookSigning));

            audit.Record(new AuditEvent(
                AuditActions.WebhookSecretRotated,
                AuditResources.Webhook,
                subscription.Id,
                Details: new Dictionary<string, object?>
                {
                    ["destination"] = WebhookLookup.Destination(subscription.Url),
                }));

            rotated = subscription;
        }, cancellationToken);

        return new RotatedWebhookSecretDto(
            rotated!.Id,
            secret,
            WebhookSigningDto.Instance,
            rotated.Version,
            rotated.UpdatedAt ?? clock.UtcNow);
    }
}
