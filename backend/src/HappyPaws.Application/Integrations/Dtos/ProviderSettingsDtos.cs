using HappyPaws.Application.Common;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.Dtos;

/// <summary>
/// <c>PUT /staff/integrations/payment</c>. <b>No secret is ever in here</b>: <c>HasSecretKey</c>
/// and <c>HasWebhookSecret</c> say whether one is stored. <c>Connected</c> is always false,
/// because no gateway connector exists; the settings are stored, not used.
/// </summary>
public sealed record PaymentSettingsDto(
    uint Version,
    string? Provider,
    string Mode,
    bool Enabled,
    string? PublicKey,
    bool HasSecretKey,
    bool HasWebhookSecret,
    bool Connected,
    DateTimeOffset? UpdatedAt)
{
    public static PaymentSettingsDto From(PaymentProviderSettings settings) =>
        new(
            settings.Version,
            settings.Provider,
            WireEnum.Of(settings.Mode),
            settings.Enabled,
            settings.PublicKey,
            settings.SecretKeyCiphertext is not null,
            settings.WebhookSecretCiphertext is not null,
            Connected: false,
            settings.UpdatedAt ?? (settings.CreatedAt == default ? null : settings.CreatedAt));
}

/// <summary>
/// <c>PUT /staff/integrations/lab</c>. The API key is never returned, only <c>HasApiKey</c>.
/// <c>Connected</c> is always false: no lab connector exists yet.
/// </summary>
public sealed record LabSettingsDto(
    uint Version,
    string? Provider,
    bool Enabled,
    string? AccountId,
    string? BaseUrl,
    bool HasApiKey,
    bool Connected,
    DateTimeOffset? UpdatedAt)
{
    public static LabSettingsDto From(LabProviderSettings settings) =>
        new(
            settings.Version,
            settings.Provider,
            settings.Enabled,
            settings.AccountId,
            settings.BaseUrl,
            settings.ApiKeyCiphertext is not null,
            Connected: false,
            settings.UpdatedAt ?? (settings.CreatedAt == default ? null : settings.CreatedAt));
}
