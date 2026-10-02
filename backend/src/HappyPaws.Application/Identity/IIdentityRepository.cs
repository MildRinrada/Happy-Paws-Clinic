using HappyPaws.Domain.Access;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Identity;

/// <summary>
/// Data access for the identity aggregates. Kept as one port because sign-in reads all of them in
/// a single step — account, membership, roles — and splitting it would only add indirection.
/// </summary>
public interface IIdentityRepository
{
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>The account using a phone number, given as digits only; null when none does.</summary>
    Task<UserAccount?> FindByPhoneAsync(string phoneDigits, CancellationToken cancellationToken = default);

    /// <summary>A new login — created by accepting a staff invitation or by the bootstrap command.</summary>
    void AddAccount(UserAccount account);

    Task<StaffMembership?> FindStaffMembershipAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>The live customer record this login belongs to, or null — the account's customer membership.</summary>
    Task<string?> FindCustomerIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Roles by id, used to expand a membership into its effective scope set.</summary>
    Task<IReadOnlyList<Role>> FindRolesAsync(IReadOnlyCollection<string> roleIds, CancellationToken cancellationToken = default);

    Task<UserSession?> FindSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Looks a session up by the hash of a presented refresh token — current generation only.</summary>
    Task<UserSession?> FindSessionByRefreshTokenAsync(string refreshTokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Matches a presented token against both the current and the previous generation, so that a
    /// replayed token can be recognised as a replay rather than dismissed as a forgery.
    /// </summary>
    Task<RefreshTokenMatch?> MatchRefreshTokenAsync(string refreshTokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSession>> ListActiveSessionsAsync(string userId, CancellationToken cancellationToken = default);

    void AddSession(UserSession session);

    /// <summary>Revokes every session for the account except one — the "change password" case.</summary>
    Task RevokeOtherSessionsAsync(string userId, string exceptSessionId, DateTimeOffset now, CancellationToken cancellationToken = default);
}

/// <summary>
/// A refresh token resolved to its session.
/// </summary>
/// <param name="Session">The session the token belongs to.</param>
/// <param name="IsCurrent">
/// False when the token matched the <i>previous</i> generation — it was already rotated away, so
/// presenting it now is a replay.
/// </param>
public sealed record RefreshTokenMatch(UserSession Session, bool IsCurrent);
