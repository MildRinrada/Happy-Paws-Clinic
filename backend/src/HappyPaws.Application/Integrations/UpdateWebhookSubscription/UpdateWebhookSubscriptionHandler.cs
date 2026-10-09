using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.UpdateWebhookSubscription;

/// <summary>
/// <c>PATCH /staff/webhooks/{id}</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 22</c>. Every field is optional and an absent one is left alone.
/// <code>{ "url": "https://example.com/hooks/v2", "events": ["invoice.issued"], "description": "", "active": false }</code>
/// </summary>
/// <param name="SubscriptionId">The webhook.</param>
/// <param name="Url">A new destination, checked as on create.</param>
/// <param name="Events">A new list of events, replacing the old one whole.</param>
/// <param name="Description">A new description; an empty string clears it.</param>
/// <param name="Active">Switches it on or off.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record UpdateWebhookSubscriptionCommand(
    string SubscriptionId,
    string? Url = null,
    IReadOnlyList<string>? Events = null,
    string? Description = null,
    bool? Active = null,
    uint? IfMatchVersion = null);

/// <summary>
/// The create rules, applied to the fields that were sent. Only to those: switching a webhook off
/// must work even if its stored URL would no longer pass a stricter rule.
/// </summary>
public sealed class UpdateWebhookSubscriptionValidator : AbstractValidator<UpdateWebhookSubscriptionCommand>
{
    public UpdateWebhookSubscriptionValidator(IntegrationPolicy policy)
    {
        RuleFor(command => command.Url)
            .Must(url => WebhookUrl.Problem(url!, policy.AllowLocalhost) is null)
            .WithMessage(command => WebhookUrl.Problem(command.Url!, policy.AllowLocalhost) ?? string.Empty)
            .When(command => command.Url is not null);

        RuleFor(command => command.Events)
            .Must(events => events!.Count > 0).WithMessage("ต้องเลือก event อย่างน้อยหนึ่งรายการ")
            .Must(events => events!.All(type => WebhookEventTypes.All.Contains(type?.Trim() ?? string.Empty)))
            .WithMessage(command => "ไม่รู้จัก event: " + string.Join(", ", command.Events!.Where(type => !WebhookEventTypes.All.Contains(type?.Trim() ?? string.Empty))))
            .When(command => command.Events is not null);

        RuleFor(command => command.Description).MaximumLength(WebhookSubscription.DescriptionMaxLength);
    }
}

/// <summary>
/// Changes a webhook's destination, events or description, or switches it on or off.
/// <para>
/// <b>Off means off.</b> Fan-out already skips a webhook that is off; switching one off also
/// stops every delivery still waiting for it, in the same transaction, as <c>failed</c> with
/// <c>webhook_disabled</c>, and the sender checks again before each attempt. Events that happen
/// while it is off are never queued for it, so switching it back on does not replay them; a
/// stopped delivery can be sent again by hand (<c>…/redeliver</c>). An attempt already on the
/// wire when the switch is pressed may still arrive.
/// </para>
/// <para>
/// <b>A new URL</b> applies to every attempt from now on, retries of earlier events included.
/// Audited with the new destination (no query string) and the fields that changed; an edit that
/// changes nothing writes nothing.
/// </para>
/// </summary>
public sealed class UpdateWebhookSubscriptionHandler(
    IWebhookRepository webhooks,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<WebhookDto> HandleAsync(UpdateWebhookSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        WebhookSubscription? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var subscription = await webhooks.LoadLiveAsync(command.SubscriptionId, token);

            ConcurrencyGuard.Ensure(subscription, command.IfMatchVersion);

            var before = Snapshot(subscription);

            if (command.Url is { } url)
            {
                subscription.ChangeUrl(url);
            }

            if (command.Events is { } events)
            {
                subscription.ChangeEvents(events);
            }

            if (command.Description is { } description)
            {
                subscription.ChangeDescription(description);
            }

            if (command.Active is { } active)
            {
                subscription.SetActive(active);
            }

            var changed = Changed(before, Snapshot(subscription));

            if (before.Active && !subscription.Active)
            {
                await webhooks.HaltPendingAsync(subscription.Id, WebhookDelivery.HaltedDisabled, clock.UtcNow, token);
            }

            if (changed.Count > 0)
            {
                audit.Record(new AuditEvent(
                    AuditActions.WebhookUpdated,
                    AuditResources.Webhook,
                    subscription.Id,
                    Details: new Dictionary<string, object?>
                    {
                        ["changed"] = changed,
                        ["destination"] = WebhookLookup.Destination(subscription.Url),
                        ["events"] = subscription.Events.ToArray(),
                        ["active"] = subscription.Active,
                    }));
            }

            updated = subscription;
        }, cancellationToken);

        return WebhookDto.From(updated!);
    }

    private sealed record State(string Url, string Events, string? Description, bool Active);

    private static State Snapshot(WebhookSubscription subscription) =>
        new(subscription.Url, string.Join(',', subscription.Events), subscription.Description, subscription.Active);

    /// <summary>The wire names of what changed, for the audit row.</summary>
    private static List<string> Changed(State before, State after)
    {
        var changed = new List<string>();

        if (before.Url != after.Url)
        {
            changed.Add("url");
        }

        if (before.Events != after.Events)
        {
            changed.Add("events");
        }

        if (before.Description != after.Description)
        {
            changed.Add("description");
        }

        if (before.Active != after.Active)
        {
            changed.Add("active");
        }

        return changed;
    }
}
