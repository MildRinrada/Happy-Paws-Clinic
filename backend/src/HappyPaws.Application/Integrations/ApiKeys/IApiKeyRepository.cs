using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.ApiKeys;

public interface IApiKeyRepository
{
    void Add(ApiKey key);

    /// <summary>The key with this public prefix, untracked; null when none.</summary>
    Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>Writes <c>last_used_at</c> directly, outside any unit of work, so a read request does not grow a transaction.</summary>
    Task TouchAsync(string keyId, DateTimeOffset at, CancellationToken cancellationToken = default);
}
