using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Identity.Dtos;

namespace HappyPaws.Application.Identity.ManageSessions;

/// <summary>
/// <c>GET /me/sessions</c> — the devices this account is signed in on.
/// </summary>
public sealed class ListSessionsHandler(IIdentityRepository repository, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<SessionDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException("no subject on token");
        var currentSessionId = currentUser.SessionId;

        var sessions = await repository.ListActiveSessionsAsync(userId, cancellationToken);

        return
        [
            .. sessions.Select(session => new SessionDto(
                session.Id,
                session.UserAgent,
                session.IpAddress,
                session.CreatedAt,
                session.LastUsedAt,
                session.ExpiresAt,
                Current: session.Id == currentSessionId))
        ];
    }
}

/// <summary>
/// <c>DELETE /me/sessions/{id}</c> — "เตะอุปกรณ์ที่หายออกจากระบบ".
/// </summary>
public sealed class RevokeSessionHandler(
    IIdentityRepository repository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task HandleAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException("no subject on token");

        var session = await repository.FindSessionAsync(sessionId, cancellationToken);

        // 404 rather than 403 when the session belongs to somebody else: session ids are opaque,
        // and confirming that one exists would let a caller probe for other people's devices.
        if (session is null || session.UserId != userId)
        {
            throw new NotFoundException("session", sessionId);
        }

        session.Revoke(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
