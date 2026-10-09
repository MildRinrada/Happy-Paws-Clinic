using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Integrations;

/// <summary>
/// The outside laboratory the clinic sends samples to — <c>PUT /staff/integrations/lab</c>.
/// <para>
/// <b>Stored, not used.</b> There is no lab connector: lab requests and results are entered by
/// hand through <c>/staff/lab-requests</c>. These settings keep which lab, the clinic's account
/// there, the lab's API address and its key, checked for shape, for the connector that will read
/// them. <see cref="Enabled"/> changes nothing yet.
/// </para>
/// <para>
/// <see cref="BaseUrl"/> follows the webhook rule (<see cref="WebhookUrl"/>): https and not a
/// private address, because a connector would one day fetch it from inside the clinic's network.
/// <see cref="ApiKeyCiphertext"/> arrives encrypted and is never returned.
/// </para>
/// </summary>
public sealed class LabProviderSettings : Entity
{
    public const string SingletonId = "set_lab";

    public const int AccountIdMaxLength = 100;

    private LabProviderSettings()
    {
    }

    public string? Provider { get; private set; }

    public bool Enabled { get; private set; }

    /// <summary>The clinic's account or customer number at the lab. Shown back.</summary>
    public string? AccountId { get; private set; }

    public string? BaseUrl { get; private set; }

    public string? ApiKeyCiphertext { get; private set; }

    public static LabProviderSettings CreateDefault() => new() { Id = SingletonId };

    /// <summary>
    /// A partial update: null leaves a value alone, an empty string clears it.
    /// <paramref name="isDevelopment"/> lets <c>http://localhost</c> through, as for webhooks.
    /// Turning it on needs a provider.
    /// </summary>
    public void Update(
        string? provider,
        bool? enabled,
        string? accountId,
        string? baseUrl,
        string? apiKeyCiphertext,
        bool isDevelopment)
    {
        if (provider is not null)
        {
            var trimmed = provider.Trim();

            if (trimmed.Length > 0 && !ProviderCode.IsValid(trimmed))
            {
                throw new DomainException("invalid_lab_settings", "ชื่อแล็บไม่ถูกต้อง");
            }

            Provider = trimmed.Length == 0 ? null : trimmed;
        }

        if (accountId is not null)
        {
            var trimmed = accountId.Trim();

            if (trimmed.Length > AccountIdMaxLength)
            {
                throw new DomainException("invalid_lab_settings", "รหัสบัญชียาวเกินไป");
            }

            AccountId = trimmed.Length == 0 ? null : trimmed;
        }

        if (baseUrl is not null)
        {
            var trimmed = baseUrl.Trim();

            if (trimmed.Length > 0 && WebhookUrl.Problem(trimmed, isDevelopment) is { } problem)
            {
                throw new DomainException("invalid_lab_settings", problem);
            }

            BaseUrl = trimmed.Length == 0 ? null : trimmed;
        }

        if (apiKeyCiphertext is not null)
        {
            ApiKeyCiphertext = apiKeyCiphertext.Length == 0 ? null : apiKeyCiphertext;
        }

        Enabled = enabled ?? Enabled;

        if (Enabled && Provider is null)
        {
            throw new DomainException("invalid_lab_settings", "เปิดใช้ได้เมื่อระบุแล็บแล้ว");
        }
    }
}
