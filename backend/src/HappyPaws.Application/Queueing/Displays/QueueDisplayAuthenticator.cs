using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.Displays;

/// <summary>What a valid display token is: one screen at one branch.</summary>
public sealed record QueueDisplayIdentity(string DisplayId, string BranchId);

/// <summary>
/// Turns a presented <c>hpdsp_…</c> token into the screen it belongs to, or null.
/// <para>
/// Reads the row on every request and caches nothing, so a revoked or expired display is refused
/// on its very next request. Every failure looks the same to the caller: unknown token, not yet
/// claimed, revoked, past its end. <c>docs/contract-extensions.md § 30</c>.
/// </para>
/// </summary>
public sealed class QueueDisplayAuthenticator(IQueueDisplayRepository displays, IClock clock)
{
    public async Task<QueueDisplayIdentity?> AuthenticateAsync(string presented, CancellationToken cancellationToken = default)
    {
        if (!QueueDisplay.LooksLikeToken(presented))
        {
            return null;
        }

        var display = await displays.FindByTokenHashAsync(QueueDisplay.Hash(presented), cancellationToken);
        var now = clock.UtcNow;

        if (display is null || !display.TokenMatches(presented) || !display.IsUsableAt(now))
        {
            return null;
        }

        if (display.NeedsLastSeenUpdate(now))
        {
            await displays.TouchAsync(display.Id, now, cancellationToken);
        }

        return new QueueDisplayIdentity(display.Id, display.BranchId);
    }
}
