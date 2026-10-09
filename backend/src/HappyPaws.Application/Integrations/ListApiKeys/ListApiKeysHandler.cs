using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Integrations.ApiKeys;
using HappyPaws.Application.Integrations.Dtos;

namespace HappyPaws.Application.Integrations.ListApiKeys;

/// <summary>
/// <c>GET /staff/api-keys</c> — the keys the caller can reach (<see cref="ApiKeyReach"/>), newest
/// first, revoked and expired ones included with their <c>status</c>. Not in the API reference;
/// see <c>docs/contract-extensions.md § 22</c>. The prefix identifies a key; the key and its hash
/// are never in the answer.
/// </summary>
public sealed class ListApiKeysHandler(IApiKeyRepository keys, ApiKeyReach reach, IClock clock)
{
    public async Task<IReadOnlyList<ApiKeyDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        return [.. (await keys.ListAsync(cancellationToken)).Where(reach.Allows).Select(key => ApiKeyDto.From(key, now))];
    }
}
