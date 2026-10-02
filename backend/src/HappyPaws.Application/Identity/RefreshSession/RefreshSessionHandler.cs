using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.Sessions;
using HappyPaws.Application.Identity.StaffLogin;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Identity.RefreshSession;

public sealed record RefreshSessionCommand(string RefreshToken);

/// <summary>
/// <c>POST /auth/refresh</c> — rotating refresh, per the contract's "rotate refresh token ทุกครั้ง".
/// </summary>
public sealed class RefreshSessionHandler(
    IIdentityRepository repository,
    SessionIssuer sessionIssuer,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RefreshSessionHandler> logger)
{
    public async Task<AuthTokens> HandleAsync(RefreshSessionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            throw new InvalidCredentialsException();
        }

        var now = clock.UtcNow;
        var match = await repository.MatchRefreshTokenAsync(
            RefreshTokens.Hash(command.RefreshToken), cancellationToken);

        if (match is null)
        {
            // Matches neither the current nor the previous generation: forged, or so old that the
            // session has already been cleaned up. Nothing to revoke.
            logger.LogWarning("Refresh presented with an unknown token");
            throw new InvalidCredentialsException();
        }

        var session = match.Session;

        if (!match.IsCurrent)
        {
            // The token was already rotated away, yet someone is still presenting it. That means
            // two parties hold it — the real device and whoever copied it — and there is no way to
            // tell which one is calling now. The only safe response is to end the session and make
            // both sign in again.
            logger.LogWarning("Refresh token reuse detected on session {SessionId}", session.Id);
            session.RevokeForTokenReuse(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (!session.IsActive(now))
        {
            logger.LogWarning("Refresh attempted on inactive session {SessionId}", session.Id);
            throw new InvalidCredentialsException();
        }

        var account = await repository.FindByIdAsync(session.UserId, cancellationToken);

        // Status and credential changes take effect on the next refresh at the latest — a
        // suspended employee cannot keep renewing a session for 30 days.
        if (account is null || !account.CanAuthenticate || !account.IsCredentialCurrent(session.LastUsedAt))
        {
            session.Revoke(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        // A session that has not cleared its second factor is a 2FA challenge, and the value
        // /auth/staff/login hands out as that challenge is this session's refresh token. Refreshing
        // it here would mint an access token from a password alone. Only /auth/2fa/verify may
        // turn a challenge into a session; this refuses without revoking, so the real user can
        // still finish signing in.
        if (account.TwoFactorEnabled && !session.TwoFactorSatisfied)
        {
            logger.LogWarning("Refresh attempted on session {SessionId} before its second factor", session.Id);
            throw new InvalidCredentialsException();
        }

        var tokens = await sessionIssuer.RotateAsync(account, session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return tokens;
    }
}
