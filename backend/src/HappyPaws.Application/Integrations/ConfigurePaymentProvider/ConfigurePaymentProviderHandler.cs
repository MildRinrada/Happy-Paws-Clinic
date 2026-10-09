using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.ConfigurePaymentProvider;

/// <summary>
/// <c>PUT /staff/integrations/payment</c>:
/// <code>
/// { "provider": "opn", "mode": "test", "enabled": false, "public_key": "pkey_test_…",
///   "secret_key": "skey_test_…", "webhook_secret": "…" }
/// </code>
/// Every field is optional and an absent one is left alone, so a secret does not have to be typed
/// again to switch the mode. An empty string clears a value. An empty body writes nothing and reads
/// the settings back, because the contract has no <c>GET</c> (as for LINE).
/// </summary>
public sealed record ConfigurePaymentProviderCommand(
    string? Provider = null,
    string? Mode = null,
    bool? Enabled = null,
    string? PublicKey = null,
    string? SecretKey = null,
    string? WebhookSecret = null,
    uint? IfMatchVersion = null)
{
    public bool IsEmpty =>
        Provider is null && Mode is null && Enabled is null && PublicKey is null && SecretKey is null && WebhookSecret is null;
}

public sealed class ConfigurePaymentProviderValidator : AbstractValidator<ConfigurePaymentProviderCommand>
{
    public ConfigurePaymentProviderValidator()
    {
        RuleFor(command => command.Provider)
            .Must(provider => provider!.Trim().Length == 0 || ProviderCode.IsValid(provider.Trim()))
            .WithMessage("ชื่อผู้ให้บริการต้องเป็นตัวอักษรอังกฤษพิมพ์เล็ก ตัวเลข หรือ _ 2 ถึง 30 ตัว")
            .When(command => command.Provider is not null);

        RuleFor(command => command.Mode)
            .Must(mode => WireEnum.TryParse<PaymentProviderMode>(mode, out _))
            .WithMessage($"mode ต้องเป็น {WireEnum.Names<PaymentProviderMode>()}")
            .When(command => command.Mode is not null);

        RuleFor(command => command.PublicKey)
            .Must(key => key!.Trim().Length <= PaymentProviderSettings.PublicKeyMaxLength && !key.Trim().Any(char.IsWhiteSpace))
            .WithMessage("public key ไม่ถูกต้อง")
            .When(command => command.PublicKey is not null);

        RuleFor(command => command.SecretKey)
            .Must(ProviderSecret.IsWellFormed).WithMessage(ProviderSecret.Message)
            .When(command => command.SecretKey is not null);

        RuleFor(command => command.WebhookSecret)
            .Must(ProviderSecret.IsWellFormed).WithMessage(ProviderSecret.Message)
            .When(command => command.WebhookSecret is not null);
    }
}

/// <summary>
/// Stores the payment provider settings. There is no gateway behind them: the settings are kept
/// and checked for shape only (see <see cref="PaymentProviderSettings"/>).
/// <para>
/// Secrets are encrypted before they are stored and are never returned. Audited with which fields
/// changed, never their values.
/// </para>
/// </summary>
public sealed class ConfigurePaymentProviderHandler(
    IClinicSettingsStore settings,
    ISecretProtector protector,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<PaymentSettingsDto> HandleAsync(
        ConfigurePaymentProviderCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        if (command.IsEmpty)
        {
            return PaymentSettingsDto.From(await settings.ReadAsync(
                PaymentProviderSettings.SingletonId, PaymentProviderSettings.CreateDefault, cancellationToken));
        }

        PaymentProviderSettings? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var record = await settings.LoadForUpdateAsync(
                PaymentProviderSettings.SingletonId, PaymentProviderSettings.CreateDefault, token);

            ConcurrencyGuard.Ensure(record, command.IfMatchVersion);

            WireEnum.TryParse<PaymentProviderMode>(command.Mode, out var mode);

            record.Update(
                command.Provider,
                command.Mode is null ? null : mode,
                command.Enabled,
                command.PublicKey,
                ProviderSecret.Protect(protector, command.SecretKey, SecretPurposes.PaymentProvider),
                ProviderSecret.Protect(protector, command.WebhookSecret, SecretPurposes.PaymentProvider));

            audit.Record(new AuditEvent(
                AuditActions.PaymentIntegrationConfigured,
                AuditResources.Integration,
                record.Id,
                Details: new Dictionary<string, object?>
                {
                    ["provider"] = record.Provider,
                    ["mode"] = WireEnum.Of(record.Mode),
                    ["enabled"] = record.Enabled,
                    ["public_key_changed"] = command.PublicKey is not null,
                    ["secret_key_changed"] = command.SecretKey is not null,
                    ["webhook_secret_changed"] = command.WebhookSecret is not null,
                }));

            updated = record;
        }, cancellationToken);

        return PaymentSettingsDto.From(updated!);
    }
}

/// <summary>A provider credential as typed: no spaces, at most 500 characters; empty clears.</summary>
internal static class ProviderSecret
{
    public const int MaxLength = 500;

    public const string Message = "ค่าความลับต้องไม่มีช่องว่างและยาวไม่เกิน 500 ตัวอักษร";

    public static bool IsWellFormed(string? value) =>
        value is not null && value.Trim().Length <= MaxLength && !value.Trim().Any(char.IsWhiteSpace);

    /// <summary>Null stays null (leave alone), empty stays empty (clear), anything else is encrypted.</summary>
    public static string? Protect(ISecretProtector protector, string? value, string purpose)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length == 0 ? string.Empty : protector.Protect(trimmed, purpose);
    }
}
