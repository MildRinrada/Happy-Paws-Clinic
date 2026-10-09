using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.ConfigurePaymentProvider;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.ConfigureLabProvider;

/// <summary>
/// <c>PUT /staff/integrations/lab</c>:
/// <code>{ "provider": "idexx", "enabled": false, "account_id": "TH-12345", "base_url": "https://api.lab.example", "api_key": "…" }</code>
/// Partial, like the payment and LINE settings: absent fields are left alone, an empty string
/// clears, an empty body reads the settings back.
/// </summary>
public sealed record ConfigureLabProviderCommand(
    string? Provider = null,
    bool? Enabled = null,
    string? AccountId = null,
    string? BaseUrl = null,
    string? ApiKey = null,
    uint? IfMatchVersion = null)
{
    public bool IsEmpty => Provider is null && Enabled is null && AccountId is null && BaseUrl is null && ApiKey is null;
}

public sealed class ConfigureLabProviderValidator : AbstractValidator<ConfigureLabProviderCommand>
{
    public ConfigureLabProviderValidator(IntegrationPolicy policy)
    {
        RuleFor(command => command.Provider)
            .Must(provider => provider!.Trim().Length == 0 || ProviderCode.IsValid(provider.Trim()))
            .WithMessage("ชื่อแล็บต้องเป็นตัวอักษรอังกฤษพิมพ์เล็ก ตัวเลข หรือ _ 2 ถึง 30 ตัว")
            .When(command => command.Provider is not null);

        RuleFor(command => command.AccountId)
            .Must(id => id!.Trim().Length <= LabProviderSettings.AccountIdMaxLength)
            .WithMessage("รหัสบัญชียาวเกินไป")
            .When(command => command.AccountId is not null);

        RuleFor(command => command.BaseUrl)
            .Must(url => url!.Trim().Length == 0 || WebhookUrl.Problem(url, policy.AllowLocalhost) is null)
            .WithMessage(command => WebhookUrl.Problem(command.BaseUrl, policy.AllowLocalhost) ?? string.Empty)
            .When(command => command.BaseUrl is not null);

        RuleFor(command => command.ApiKey)
            .Must(ProviderSecret.IsWellFormed).WithMessage(ProviderSecret.Message)
            .When(command => command.ApiKey is not null);
    }
}

/// <summary>
/// Stores the lab provider settings. No lab connector exists; these are kept and checked for
/// shape only (see <see cref="LabProviderSettings"/>). The API key is encrypted and never
/// returned. Audited with which fields changed.
/// </summary>
public sealed class ConfigureLabProviderHandler(
    IClinicSettingsStore settings,
    ISecretProtector protector,
    IntegrationPolicy policy,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<LabSettingsDto> HandleAsync(
        ConfigureLabProviderCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        if (command.IsEmpty)
        {
            return LabSettingsDto.From(await settings.ReadAsync(
                LabProviderSettings.SingletonId, LabProviderSettings.CreateDefault, cancellationToken));
        }

        LabProviderSettings? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var record = await settings.LoadForUpdateAsync(
                LabProviderSettings.SingletonId, LabProviderSettings.CreateDefault, token);

            ConcurrencyGuard.Ensure(record, command.IfMatchVersion);

            record.Update(
                command.Provider,
                command.Enabled,
                command.AccountId,
                command.BaseUrl,
                ProviderSecret.Protect(protector, command.ApiKey, SecretPurposes.LabProvider),
                policy.AllowLocalhost);

            audit.Record(new AuditEvent(
                AuditActions.LabIntegrationConfigured,
                AuditResources.Integration,
                record.Id,
                Details: new Dictionary<string, object?>
                {
                    ["provider"] = record.Provider,
                    ["enabled"] = record.Enabled,
                    ["account_id_changed"] = command.AccountId is not null,
                    ["base_url"] = record.BaseUrl,
                    ["api_key_changed"] = command.ApiKey is not null,
                }));

            updated = record;
        }, cancellationToken);

        return LabSettingsDto.From(updated!);
    }
}
