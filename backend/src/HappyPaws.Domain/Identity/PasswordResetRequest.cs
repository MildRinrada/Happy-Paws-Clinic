using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Identity;

/// <summary>
/// A link to set a new password — <c>POST /auth/password/reset</c>, "ลิงก์อายุ 30 นาที ใช้ครั้งเดียว".
/// <para>
/// The token in the link is kept only as a hash, like a refresh token: a copy of the database is not
/// a set of working reset links. Asking again does not cancel an earlier link; each lives its thirty
/// minutes or until used, and using any one of them ends the others' purpose because the password
/// they would reset has already changed.
/// </para>
/// </summary>
public sealed class PasswordResetRequest : Entity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private PasswordResetRequest()
    {
    }

    public string UserId { get; private set; } = string.Empty;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public static PasswordResetRequest Issue(string userId, string tokenHash, DateTimeOffset now) =>
        new()
        {
            Id = EntityId.New(EntityId.PasswordReset),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = now + Lifetime,
        };

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && now < ExpiresAt;

    public void MarkUsed(DateTimeOffset now)
    {
        if (!IsUsable(now))
        {
            throw new StateConflictException(nameof(PasswordResetRequest), UsedAt is null ? "expired" : "used", "reset");
        }

        UsedAt = now;
    }
}
