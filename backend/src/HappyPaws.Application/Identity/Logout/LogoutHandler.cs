using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Identity.Sessions;

namespace HappyPaws.Application.Identity.Logout;

/// <param name="RefreshToken">Identifies the session to end when the caller has no access token left.</param>
/// <param name="AllDevices">The contract allows revoking one session or every one.</param>
public sealed record LogoutCommand(string? RefreshToken, bool AllDevices);

/// <summary>
/// <c>POST /auth/logout</c> — "เพิกถอน session เดียวหรือทุกอุปกรณ์".
/// </summary>
public sealed class LogoutHandler(
    IIdentityRepository repository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var session = command.RefreshToken is { Length: > 0 } token
            ? await repository.FindSessionByRefreshTokenAsync(RefreshTokens.Hash(token), cancellationToken)
            : null;

        // Logout is idempotent by design: an already-ended session, an expired cookie or a second
        // click must all succeed quietly. Reporting "no such session" would tell an attacker
        // holding a stale token whether it was ever real.
        if (session is not null)
        {
            session.Revoke(now);

            if (command.AllDevices)
            {
                await repository.RevokeOtherSessionsAsync(session.UserId, session.Id, now, cancellationToken);
            }
        }
        else if (command.AllDevices && currentUser.ApiKeyId is null && currentUser.UserId is { } userId)
        {
            // Not for an API key: it acts as its creator, but ending the creator's sessions on every
            // device is not something a machine client may do in their name.
            await repository.RevokeOtherSessionsAsync(userId, exceptSessionId: string.Empty, now, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
