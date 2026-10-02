using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Identity.Capabilities;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Application.Identity.Sessions;

/// <summary>
/// Turns an authenticated account into a session and a pair of tokens.
/// <para>
/// Shared by sign-in, 2FA verification and refresh so that all three produce identically shaped
/// tokens. If refresh built its claims separately, a scope change would apply on login but not on
/// refresh — and a revoked permission would keep working for up to 30 days.
/// </para>
/// </summary>
public sealed class SessionIssuer(
    IIdentityRepository repository,
    EffectiveScopeResolver scopeResolver,
    IAccessTokenIssuer tokenIssuer,
    IClock clock)
{
    /// <summary>How long a login may sit un-verified before the second factor must be re-requested.</summary>
    public static readonly TimeSpan TwoFactorChallengeLifetime = TimeSpan.FromMinutes(5);

    /// <summary>Starts a session for a freshly authenticated account.</summary>
    public async Task<AuthTokens> StartAsync(
        UserAccount account,
        bool twoFactorSatisfied,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = RefreshTokens.Generate();

        var session = UserSession.Start(
            account.Id,
            RefreshTokens.Hash(refreshToken),
            clock.UtcNow,
            userAgent,
            ipAddress,
            twoFactorSatisfied);

        repository.AddSession(session);

        return await IssueAsync(account, session, refreshToken, cancellationToken);
    }

    /// <summary>Rotates an existing session onto a new refresh token and mints a fresh access token.</summary>
    public async Task<AuthTokens> RotateAsync(
        UserAccount account,
        UserSession session,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = RefreshTokens.Generate();
        session.Rotate(RefreshTokens.Hash(refreshToken), clock.UtcNow);

        return await IssueAsync(account, session, refreshToken, cancellationToken);
    }

    private async Task<AuthTokens> IssueAsync(
        UserAccount account,
        UserSession session,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var membership = await repository.FindStaffMembershipAsync(account.Id, cancellationToken);

        // The other half of "บัญชีเดียวเป็นทั้งลูกค้าและพนักงาน": the customer record this login
        // belongs to, if any. Read on every issue, like the staff membership, so a merge or a newly
        // linked record takes effect at the next refresh.
        var customerId = await repository.FindCustomerIdAsync(account.Id, cancellationToken);
        var scopes = await scopeResolver.ResolveAsync(membership, customerId, cancellationToken);

        var staffId = membership is { IsActive: true } ? membership.Id : null;
        var branches = membership?.BranchIds ?? [];

        var issued = tokenIssuer.Issue(new AccessTokenRequest(
            UserId: account.Id,
            SessionId: session.Id,
            StaffId: staffId,
            CustomerId: customerId,
            BranchIds: branches,
            Scopes: [.. scopes],
            TwoFactorSatisfied: session.TwoFactorSatisfied));

        return new AuthTokens(
            issued.Token,
            issued.ExpiresAt,
            refreshToken,
            session.Id,
            session.TwoFactorSatisfied,
            new SessionCapabilities(staffId, customerId, [.. scopes], [.. branches]));
    }
}
