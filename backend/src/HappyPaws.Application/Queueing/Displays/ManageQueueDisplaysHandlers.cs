using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Queueing.Displays;

/// <summary>
/// <c>GET /staff/queue/displays?branch=</c> — the screens at the caller's branches, newest first,
/// revoked ones included with their <c>status</c>. Never a token, a code or a hash. Not in the API
/// reference; see <c>docs/contract-extensions.md § 30</c>.
/// </summary>
public sealed class ListQueueDisplaysHandler(IQueueDisplayRepository displays, BranchGuard branchGuard, IClock clock)
{
    public async Task<IReadOnlyList<QueueDisplayDto>> HandleAsync(
        string? branchId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string>? branches = branchGuard.VisibleBranchIds;

        if (branchId is { Length: > 0 })
        {
            branchGuard.EnsureCanAccess(branchId);
            branches = [branchId];
        }

        var now = clock.UtcNow;

        return [.. (await displays.ListAsync(branches, cancellationToken)).Select(display => QueueDisplayDto.From(display, now))];
    }
}

/// <summary>
/// <c>DELETE /staff/queue/displays/{id}</c> — stops a screen now. Not in the API reference; see
/// <c>docs/contract-extensions.md § 30</c>.
/// <para>
/// Authentication reads the display's row on every request, so once this commits the screen's next
/// board read is <c>401</c>, and an open stream is closed at its next heartbeat (20 seconds at most).
/// An unused setup code dies with it. The row stays, so the list still says which screen it was.
/// Revoking a revoked display is <c>204</c> and writes nothing; a display at a branch the caller
/// does not work at is <c>404</c>, as if it did not exist.
/// </para>
/// </summary>
public sealed class RevokeQueueDisplayHandler(
    IQueueDisplayRepository displays,
    BranchGuard branchGuard,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task HandleAsync(string displayId, CancellationToken cancellationToken = default)
    {
        var display = await displays.FindAsync(displayId, cancellationToken);

        if (display is null || !branchGuard.CanAccess(display.BranchId))
        {
            throw new NotFoundException("queue_display", displayId);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;

            if (!await displays.RevokeAsync(display.Id, now, token))
            {
                return;
            }

            audit.Record(new AuditEvent(
                AuditActions.QueueDisplayRevoked,
                AuditResources.QueueDisplay,
                display.Id,
                BranchId: display.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["name"] = display.Name,
                    ["was"] = display.StateAt(now),
                    ["last_seen_at"] = display.LastSeenAt,
                }));
        }, cancellationToken);
    }
}
