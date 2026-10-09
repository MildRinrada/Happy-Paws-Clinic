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

/// <summary>
/// A key as <c>GET /staff/api-keys</c> shows it (<c>docs/contract-extensions.md § 22</c>): enough to
/// recognise it and decide whether to revoke it. Never the key, never its hash.
/// <see cref="Status"/> is <c>active</c>, <c>expired</c> or <c>revoked</c>.
/// </summary>
public sealed record ApiKeyDto(
    string Id,
    string Name,
    string Prefix,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> BranchIds,
    string Status,
    string CreatedByStaffId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt)
{
    public static ApiKeyDto From(ApiKey key, DateTimeOffset now) =>
        new(
            key.Id,
            key.Name,
            ApiKey.KeyPrefix + key.Prefix,
            key.Scopes,
            key.BranchIds,
            key.StateAt(now),
            key.CreatedByStaffId,
            key.CreatedAt,
            key.ExpiresAt,
            key.LastUsedAt,
            key.RevokedAt);
}
