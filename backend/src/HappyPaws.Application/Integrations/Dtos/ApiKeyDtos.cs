using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.Dtos;

/// <summary>
/// The answer to <c>POST /staff/api-keys</c>. <see cref="Key"/> is the whole key, shown here once
/// and stored nowhere. Send it as <c>Authorization: Bearer hp_…</c>.
/// </summary>
public sealed record CreatedApiKeyDto(
    string Id,
    string Name,
    string Key,
    string Prefix,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> BranchIds,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt)
{
    public static CreatedApiKeyDto From(ApiKey key, string plaintext) =>
        new(
            key.Id,
            key.Name,
            plaintext,
            ApiKey.KeyPrefix + key.Prefix,
            key.Scopes,
            key.BranchIds,
            key.ExpiresAt,
            key.CreatedAt);
}
