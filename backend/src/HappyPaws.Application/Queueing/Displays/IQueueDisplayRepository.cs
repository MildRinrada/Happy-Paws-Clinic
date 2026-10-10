using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Queueing.Displays;

/// <summary>Persistence port for waiting-room screens (<c>docs/contract-extensions.md § 30</c>).</summary>
public interface IQueueDisplayRepository
{
    void Add(QueueDisplay display);

    /// <summary>The display with this id, untracked; null when none.</summary>
    Task<QueueDisplay?> FindAsync(string displayId, CancellationToken cancellationToken = default);

    /// <summary>The display whose token has this hash, untracked; null when none.</summary>
    Task<QueueDisplay?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>The display whose unused setup code has this hash, untracked; null when none.</summary>
    Task<QueueDisplay?> FindBySetupCodeHashAsync(string codeHash, CancellationToken cancellationToken = default);

    /// <summary>Every display, revoked ones included, newest first; only these branches when given.</summary>
    Task<IReadOnlyList<QueueDisplay>> ListAsync(
        IReadOnlyCollection<string>? branchIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Trades the setup code for the token in one conditional statement: sets the token hash and
    /// clears the code only while the code is still this one and no token is set. True when this
    /// call won; two TVs typing the same code at once get one token between them.
    /// </summary>
    Task<bool> ClaimAsync(
        string displayId,
        string codeHash,
        string tokenHash,
        DateTimeOffset at,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets <c>revoked_at</c> when it is not set yet, and clears an unused setup code, in one
    /// statement inside the caller's transaction; true when this call revoked it. No version check,
    /// for the reason API keys have none: a screen in use writes <c>last_seen_at</c> every minute.
    /// </summary>
    Task<bool> RevokeAsync(string displayId, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>Writes <c>last_seen_at</c> directly, outside any unit of work.</summary>
    Task TouchAsync(string displayId, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>Whether the display's token still works now: one cheap read, for a long-lived stream.</summary>
    Task<bool> IsUsableAsync(string displayId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
