using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Integrations.ApiKeys;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Integrations.RevokeApiKey;

/// <summary>
/// <c>DELETE /staff/api-keys/{id}</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 22</c>.
/// </summary>
public sealed record RevokeApiKeyCommand(string KeyId);

/// <summary>
/// Revokes an API key now.
/// <para>
/// <b>The next request fails.</b> <see cref="ApiKeyAuthenticator"/> reads the key's row on every
/// request and nothing caches it, so once this commits the key's next request is <c>401</c>. A
/// request already past authentication finishes.
/// </para>
/// <para>
/// The row stays, with <c>revoked_at</c>, so the list and the audit rows the key caused still say
/// which key it was. Revoking a revoked key is <c>204</c> and writes nothing. No <c>If-Match</c>:
/// the list carries no version, because a key in use changes its row every minute
/// (<c>last_used_at</c>), and stopping a leaked key must not lose a race with the leak.
/// </para>
/// </summary>
public sealed class RevokeApiKeyHandler(
    IApiKeyRepository keys,
    ApiKeyReach reach,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task HandleAsync(RevokeApiKeyCommand command, CancellationToken cancellationToken = default)
    {
        var key = await keys.FindAsync(command.KeyId, cancellationToken);

        if (key is null || !reach.Allows(key))
        {
            throw new NotFoundException("api_key", command.KeyId);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;

            if (!await keys.RevokeAsync(key.Id, now, token))
            {
                return;
            }

            audit.Record(new AuditEvent(
                AuditActions.ApiKeyRevoked,
                AuditResources.ApiKey,
                key.Id,
                Details: new Dictionary<string, object?>
                {
                    ["name"] = key.Name,
                    ["prefix"] = key.Prefix,
                    ["created_by_staff_id"] = key.CreatedByStaffId,
                    ["was"] = key.StateAt(now),
                }));
        }, cancellationToken);
    }
}
