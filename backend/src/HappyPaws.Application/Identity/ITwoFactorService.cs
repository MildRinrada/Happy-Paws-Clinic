namespace HappyPaws.Application.Identity;

/// <summary>
/// TOTP enrolment and verification (RFC 6238).
/// </summary>
public interface ITwoFactorService
{
    /// <summary>Generates a new shared secret plus the <c>otpauth://</c> URI a authenticator app scans.</summary>
    TwoFactorEnrolment Enrol(string accountLabel);

    /// <summary>
    /// The <c>otpauth://</c> URI for a secret that already exists — so a setup screen shown twice
    /// encodes the same secret both times instead of minting a second one.
    /// </summary>
    string OtpAuthUri(string accountLabel, string secret);

    /// <summary>
    /// Verifies a submitted code against the secret.
    /// <para>
    /// Implementations must accept a small window of adjacent time steps for clock drift, and must
    /// compare in constant time — a timing-variable comparison leaks the code digit by digit.
    /// </para>
    /// </summary>
    bool VerifyCode(string secret, string code, DateTimeOffset now);
}

/// <param name="Secret">Base32 shared secret. Shown to the user once, then stored encrypted.</param>
/// <param name="OtpAuthUri">What the QR code encodes.</param>
/// <param name="RecoveryCodes">Single-use fallbacks for a lost device.</param>
public sealed record TwoFactorEnrolment(string Secret, string OtpAuthUri, IReadOnlyList<string> RecoveryCodes);
