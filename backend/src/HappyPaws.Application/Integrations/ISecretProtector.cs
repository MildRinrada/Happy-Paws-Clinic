namespace HappyPaws.Application.Integrations;

/// <summary>
/// Encrypts a secret the system must be able to read back: a webhook signing secret, a payment
/// provider's secret key, a lab's API key, a calendar link's token.
/// <para>
/// Those cannot be stored as a hash, because the server needs the value itself to sign a delivery
/// or call the provider. Encrypting them with keys kept outside the database (ASP.NET Core Data
/// Protection, see <c>docs/development.md</c>) means a copy of the database alone yields none of
/// them. Each kind of secret has its own <see cref="SecretPurposes">purpose</see>, so a ciphertext
/// copied from one column into another does not decrypt there.
/// </para>
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext, string purpose);

    /// <summary>The plaintext, or throws when the keys that encrypted it are gone or the value was tampered with.</summary>
    string Unprotect(string ciphertext, string purpose);
}

/// <summary>The purpose strings that keep each kind of secret apart.</summary>
public static class SecretPurposes
{
    public const string WebhookSigning = "HappyPaws.Integrations.WebhookSigning.v1";
    public const string PaymentProvider = "HappyPaws.Integrations.PaymentProvider.v1";
    public const string LabProvider = "HappyPaws.Integrations.LabProvider.v1";
    public const string CalendarFeed = "HappyPaws.Integrations.CalendarFeed.v1";
}
