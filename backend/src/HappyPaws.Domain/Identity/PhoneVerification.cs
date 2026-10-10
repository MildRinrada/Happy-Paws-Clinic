using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Identity;

/// <summary>What a one-time code was sent for. A code for one cannot be spent on the other.</summary>
public enum OtpPurpose
{
    Register,
    Login,

    /// <summary>
    /// Booking without signing up first (<c>POST /booking/guest/start</c>). Sent to every number,
    /// like <see cref="Register"/>, because the visitor may or may not have an account and must not
    /// be told which before the code proves the number. Not accepted by <c>POST /auth/otp/request</c>.
    /// See <c>docs/contract-extensions.md § 27</c>.
    /// </summary>
    Booking,
}

/// <summary>
/// A one-time code sent to a phone — <c>POST /auth/otp/request</c>.
/// <para>
/// Only the hash of the code is kept. A code lives five minutes, is spent by its first correct use,
/// and dies after five wrong guesses: six digits are a million possibilities, so one code falls to
/// guessing about once in two hundred thousand tries, and the contract's three codes per number per
/// ten minutes keeps the tries slow.
/// </para>
/// </summary>
public sealed class PhoneVerification : Entity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    public const int MaxAttempts = 5;

    private PhoneVerification()
    {
    }

    public string PhoneDigits { get; private set; } = string.Empty;

    public OtpPurpose Purpose { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public static PhoneVerification Issue(string phoneDigits, OtpPurpose purpose, string codeHash, DateTimeOffset now) =>
        new()
        {
            Id = EntityId.New(EntityId.PhoneVerification),
            PhoneDigits = phoneDigits,
            Purpose = purpose,
            CodeHash = codeHash,
            ExpiresAt = now + Lifetime,
        };

    public bool IsUsable(DateTimeOffset now) => ConsumedAt is null && now < ExpiresAt && Attempts < MaxAttempts;

    /// <summary>
    /// Spends the code when it matches. Every attempt counts, right or wrong, so a code cannot be
    /// guessed at more than <see cref="MaxAttempts"/> times.
    /// </summary>
    public bool TryConsume(string codeHash, DateTimeOffset now)
    {
        if (!IsUsable(now))
        {
            return false;
        }

        Attempts++;

        if (!string.Equals(CodeHash, codeHash, StringComparison.Ordinal))
        {
            return false;
        }

        ConsumedAt = now;
        return true;
    }
}
