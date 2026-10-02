using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Identity;

/// <summary>
/// One signed-in device, and the rotating refresh token family behind it.
/// <para>
/// Sessions are rows rather than just signed tokens because the contract requires them to be
/// listed and revoked individually (<c>GET /me/sessions</c>,
/// <c>DELETE /me/sessions/{id}</c>). Revocation has to actually take effect; a self-contained
/// token that cannot be withdrawn would make those endpoints decorative.
/// </para>
/// </summary>
public sealed class UserSession : Entity
{
    /// <summary>Contract: refresh tokens live 30 days.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private UserSession()
    {
    }

    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// SHA-256 of the current refresh token. Only the hash is stored: a leaked database backup
    /// must not hand over working sessions.
    /// </summary>
    public string RefreshTokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// The hash this session rotated away from.
    /// <para>
    /// Without it, reuse detection is impossible: once <see cref="RefreshTokenHash"/> is
    /// overwritten, a replayed old token looks exactly like a random forgery, so the theft goes
    /// unnoticed and the real session keeps working. Keeping one generation of history is what
    /// makes "this token was already spent" an observable event rather than a guess.
    /// </para>
    /// </summary>
    public string? PreviousRefreshTokenHash { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Set when the session was killed because an already-rotated token was replayed.</summary>
    public bool RevokedForReuse { get; private set; }

    /// <summary>Shown in the device list so a user can recognise which session to revoke.</summary>
    public string? UserAgent { get; private set; }

    public string? IpAddress { get; private set; }

    public DateTimeOffset LastUsedAt { get; private set; }

    /// <summary>
    /// Whether the caller cleared a second factor on this session. Carried here, not just in the
    /// access token, so that a refresh cannot silently upgrade a half-authenticated session.
    /// </summary>
    public bool TwoFactorSatisfied { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    public static UserSession Start(
        string userId,
        string refreshTokenHash,
        DateTimeOffset now,
        string? userAgent,
        string? ipAddress,
        bool twoFactorSatisfied)
    {
        return new UserSession
        {
            Id = EntityId.New(EntityId.Session),
            UserId = userId,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = now.Add(Lifetime),
            LastUsedAt = now,
            UserAgent = Truncate(userAgent, 256),
            IpAddress = ipAddress,
            TwoFactorSatisfied = twoFactorSatisfied,
        };
    }

    /// <summary>Rotates to a new refresh token, per the contract's "rotate ทุกครั้ง".</summary>
    public void Rotate(string newRefreshTokenHash, DateTimeOffset now)
    {
        PreviousRefreshTokenHash = RefreshTokenHash;
        RefreshTokenHash = newRefreshTokenHash;
        LastUsedAt = now;

        // Sliding expiry: an actively used device stays signed in, an abandoned one ages out.
        ExpiresAt = now.Add(Lifetime);
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }

    /// <summary>
    /// Kills the session because an already-rotated refresh token was presented.
    /// <para>
    /// Replay of a rotated token means two parties hold it — the legitimate device and whoever
    /// copied it. There is no way to tell which one is calling, so the only safe response is to
    /// end the session and make both re-authenticate.
    /// </para>
    /// </summary>
    public void RevokeForTokenReuse(DateTimeOffset now)
    {
        RevokedAt ??= now;
        RevokedForReuse = true;
    }

    public void MarkTwoFactorSatisfied() => TwoFactorSatisfied = true;

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
