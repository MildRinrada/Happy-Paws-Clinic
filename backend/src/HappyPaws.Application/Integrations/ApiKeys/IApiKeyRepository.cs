using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.ApiKeys;

public interface IApiKeyRepository
{
    void Add(ApiKey key);

    /// <summary>The key with this public prefix, untracked; null when none.</summary>
    Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>The key with this id, untracked; null when none.</summary>
    Task<ApiKey?> FindAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>Every key, revoked and expired ones included, newest first.</summary>
    Task<IReadOnlyList<ApiKey>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets <c>revoked_at</c> when it is not set yet, in one statement inside the caller's
    /// transaction; true when this call revoked it. Not through the tracked entity: a key in use
    /// writes <c>last_used_at</c> every minute, and a version check would make revoking a key that
    /// is being abused fail exactly when it matters.
    /// </summary>
    Task<bool> RevokeAsync(string keyId, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>Writes <c>last_used_at</c> directly, outside any unit of work, so a read request does not grow a transaction.</summary>
    Task TouchAsync(string keyId, DateTimeOffset at, CancellationToken cancellationToken = default);
}
