using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Integrations;

public enum PaymentProviderMode
{
    Test,
    Live,
}

/// <summary>
/// The clinic's online payment provider — <c>PUT /staff/integrations/payment</c>.
/// <para>
/// <b>Stored, not used.</b> There is no payment gateway in this system: a card payment is
/// "the card terminal approved it and the slip number is in <c>reference</c>". These settings
/// keep the provider, the mode and the credentials, checked for shape, ready for the connector
/// that does not exist yet. Turning <see cref="Enabled"/> on changes nothing at the cash desk.
/// </para>
/// <para>
/// <b>Secrets are write-only.</b> <see cref="SecretKeyCiphertext"/> and
/// <see cref="WebhookSecretCiphertext"/> are encrypted with the API's Data Protection keys before
/// they reach this class, and no endpoint returns them; the answer says only whether each is set.
/// </para>
/// </summary>
public sealed class PaymentProviderSettings : Entity
{
    public const string SingletonId = "set_payment";

    public const int PublicKeyMaxLength = 200;

    private PaymentProviderSettings()
    {
    }

    public string? Provider { get; private set; }

    public PaymentProviderMode Mode { get; private set; } = PaymentProviderMode.Test;

    public bool Enabled { get; private set; }

    /// <summary>The publishable key or merchant id. Not a secret; shown back.</summary>
    public string? PublicKey { get; private set; }

    public string? SecretKeyCiphertext { get; private set; }

    public string? WebhookSecretCiphertext { get; private set; }

    public static PaymentProviderSettings CreateDefault() => new() { Id = SingletonId };

    /// <summary>
    /// A partial update: null leaves a value alone, an empty string clears it. Secrets arrive
    /// already encrypted. Turning it on needs a provider and a secret key.
    /// </summary>
    public void Update(
        string? provider,
        PaymentProviderMode? mode,
        bool? enabled,
        string? publicKey,
        string? secretKeyCiphertext,
        string? webhookSecretCiphertext)
    {
        if (provider is not null)
        {
            var trimmed = provider.Trim();

            if (trimmed.Length > 0 && !ProviderCode.IsValid(trimmed))
            {
                throw new DomainException("invalid_payment_settings", "ชื่อผู้ให้บริการไม่ถูกต้อง");
            }

            Provider = trimmed.Length == 0 ? null : trimmed;
        }

        Mode = mode ?? Mode;

        if (publicKey is not null)
        {
            var trimmed = publicKey.Trim();

            if (trimmed.Length > PublicKeyMaxLength || trimmed.Any(char.IsWhiteSpace))
            {
                throw new DomainException("invalid_payment_settings", "public key ไม่ถูกต้อง");
            }

            PublicKey = trimmed.Length == 0 ? null : trimmed;
        }

        if (secretKeyCiphertext is not null)
        {
            SecretKeyCiphertext = secretKeyCiphertext.Length == 0 ? null : secretKeyCiphertext;
        }

        if (webhookSecretCiphertext is not null)
        {
            WebhookSecretCiphertext = webhookSecretCiphertext.Length == 0 ? null : webhookSecretCiphertext;
        }

        Enabled = enabled ?? Enabled;

        if (Enabled && (Provider is null || SecretKeyCiphertext is null))
        {
            throw new DomainException("invalid_payment_settings", "เปิดใช้ได้เมื่อระบุผู้ให้บริการและ secret key แล้ว");
        }
    }
}
