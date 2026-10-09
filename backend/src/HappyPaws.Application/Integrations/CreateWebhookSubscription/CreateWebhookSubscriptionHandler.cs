using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.CreateWebhookSubscription;

/// <summary>
/// <c>POST /staff/webhooks</c>:
/// <code>{ "url": "https://example.com/hooks/clinic", "events": ["appointment.created"], "description": "ระบบบัญชี" }</code>
/// </summary>
public sealed record CreateWebhookSubscriptionCommand(
    string Url,
    IReadOnlyList<string> Events,
    string? Description = null);

public sealed class CreateWebhookSubscriptionValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    public CreateWebhookSubscriptionValidator(IntegrationPolicy policy)
    {
        RuleFor(command => command.Url)
            .Must(url => WebhookUrl.Problem(url, policy.AllowLocalhost) is null)
            .WithMessage(command => WebhookUrl.Problem(command.Url, policy.AllowLocalhost) ?? string.Empty);

        RuleFor(command => command.Events)
            .NotEmpty().WithMessage("ต้องเลือก event อย่างน้อยหนึ่งรายการ")
            .Must(events => events.All(type => WebhookEventTypes.All.Contains(type?.Trim() ?? string.Empty)))
            .WithMessage(command => "ไม่รู้จัก event: " + string.Join(", ", command.Events.Where(type => !WebhookEventTypes.All.Contains(type?.Trim() ?? string.Empty))));

        RuleFor(command => command.Description).MaximumLength(WebhookSubscription.DescriptionMaxLength);
    }
}

/// <summary>
/// Creates an outbound webhook and its signing secret. The answer carries the secret, once.
/// <para>
/// Audited with the events and where they will go (scheme, host and path; a query string can hold
/// a receiver's own token, so it is left out of the log), never the secret.
/// </para>
/// </summary>
public sealed class CreateWebhookSubscriptionHandler(
    IWebhookRepository webhooks,
    ISecretProtector protector,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<CreatedWebhookDto> HandleAsync(
        CreateWebhookSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var secret = WebhookSignature.NewSecret();

        var subscription = WebhookSubscription.Create(
            command.Url,
            command.Events,
            command.Description,
            protector.Protect(secret, SecretPurposes.WebhookSigning),
            staffId);

        webhooks.Add(subscription);

        var target = new Uri(subscription.Url);

        audit.Record(new AuditEvent(
            AuditActions.WebhookCreated,
            AuditResources.Webhook,
            subscription.Id,
            Details: new Dictionary<string, object?>
            {
                ["destination"] = target.GetLeftPart(UriPartial.Path),
                ["events"] = subscription.Events.ToArray(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedWebhookDto.From(subscription, secret);
    }
}
