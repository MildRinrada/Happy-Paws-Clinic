using System.Security.Cryptography;
using System.Text;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Application.Identity;

/// <summary>Persistence port for one-time codes and password-reset links.</summary>
public interface ICustomerSignInRepository
{
    void AddVerification(PhoneVerification verification);

    /// <summary>The newest code for this number and purpose that can still be used, tracked.</summary>
    Task<PhoneVerification?> LatestUsableAsync(
        string phoneDigits,
        OtpPurpose purpose,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>How many codes this number has been sent since a moment — the rate limit's count.</summary>
    Task<int> CodesSentSinceAsync(string phoneDigits, DateTimeOffset since, CancellationToken cancellationToken = default);

    void AddResetRequest(PasswordResetRequest request);

    /// <summary>The reset request a link's token belongs to, tracked.</summary>
    Task<PasswordResetRequest?> FindResetAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<int> ResetsRequestedSinceAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken = default);
}

/// <summary>
/// Sends a one-time code by SMS. The only adapter today writes to the log instead; see
/// <c>LoggingOtpSender</c>.
/// </summary>
public interface IOtpSender
{
    Task SendAsync(string phoneDigits, string code, OtpPurpose purpose, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
}

/// <summary>A password-reset link to deliver: by email when the account has one, otherwise by SMS.</summary>
public sealed record PasswordResetMessage(string UserId, string? Email, string? PhoneDigits, string Token, DateTimeOffset ExpiresAt);

/// <summary>Delivers a password-reset link. The only adapter today writes to the log; see <c>LoggingPasswordResetSender</c>.</summary>
public interface IPasswordResetSender
{
    Task SendAsync(PasswordResetMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// <c>RateLimits:OtpPerPhonePer10Minutes</c> — "rate limit 3 ครั้ง/เบอร์/10 นาที". Bound from
/// configuration by the Infrastructure layer.
/// </summary>
public sealed record OtpSettings(int PerPhonePer10Minutes = 3)
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
}

/// <summary>How codes and link tokens are made and kept.</summary>
public static class OneTimeSecrets
{
    /// <summary>Six digits, uniformly random.</summary>
    public static string NewCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The stored form of a code. Salted with the number, so the same six digits sent to two people
    /// are two different hashes.
    /// </summary>
    public static string HashCode(string phoneDigits, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{phoneDigits}:{code.Trim()}")));

    /// <summary>A link token: 32 random bytes, URL-safe.</summary>
    public static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
}
