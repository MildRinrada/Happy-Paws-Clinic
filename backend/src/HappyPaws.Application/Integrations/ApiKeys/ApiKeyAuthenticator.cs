using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Identity.Capabilities;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Integrations.ApiKeys;

/// <summary>Who a valid API key acts as, and with exactly what.</summary>
/// <param name="KeyId">The key's id (<c>key_…</c>), carried on every audit row it causes.</param>
/// <param name="UserId">The account that created the key.</param>
/// <param name="StaffId">The staff membership that created the key.</param>
/// <param name="Scopes">The key's scopes that its creator still holds.</param>
/// <param name="BranchIds">The branches it may act in; empty means every branch, and only when its creator is not bound to branches.</param>
public sealed record ApiKeyIdentity(
    string KeyId,
    string UserId,
    string StaffId,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> BranchIds);

/// <summary>
/// Turns a presented <c>hp_…</c> key into the identity it acts as, or null.
/// <para>
/// Every request re-checks the creator: an account that can no longer sign in, or a staff
/// membership that was deactivated, ends the key. The key's scopes are cut down to those the
/// creator holds today, and its branches to the creator's branches, so a key can never do more
/// than the person who made it could do right now. If that leaves a branch-bound creator's key
/// with no branch at all, the key is refused rather than allowed to fall through to "every branch".
/// </para>
/// <para>
/// All failures look the same to the caller (null, then <c>401</c>): unknown prefix, wrong secret,
/// expired, revoked, creator gone.
/// </para>
/// </summary>
public sealed class ApiKeyAuthenticator(
    IApiKeyRepository keys,
    IIdentityRepository identity,
    EffectiveScopeResolver scopeResolver,
    IClock clock)
{
    public async Task<ApiKeyIdentity?> AuthenticateAsync(string presented, CancellationToken cancellationToken = default)
    {
        if (ApiKey.PrefixOf(presented) is not { } prefix)
        {
            return null;
        }

        var key = await keys.FindByPrefixAsync(prefix, cancellationToken);
        var now = clock.UtcNow;

        if (key is null || !key.Matches(presented) || !key.IsUsableAt(now))
        {
            return null;
        }

        var account = await identity.FindByIdAsync(key.CreatedByUserId, cancellationToken);
        if (account is not { CanAuthenticate: true })
        {
            return null;
        }

        var membership = await identity.FindStaffMembershipAsync(account.Id, cancellationToken);
        if (membership is not { IsActive: true } || membership.Id != key.CreatedByStaffId)
        {
            return null;
        }

        var held = await scopeResolver.ResolveAsync(membership, customerId: null, cancellationToken);
        var scopes = key.Scopes.Where(scope => ApiKeyScopeRules.MayCarry(scope) && held.Allows(scope)).ToList();

        IReadOnlyList<string> branches;

        if (held.IsUnrestricted || membership.BranchIds.Count == 0)
        {
            branches = key.BranchIds;
        }
        else
        {
            branches = key.BranchIds.Count == 0
                ? membership.BranchIds
                : [.. key.BranchIds.Intersect(membership.BranchIds, StringComparer.Ordinal)];

            if (branches.Count == 0)
            {
                return null;
            }
        }

        if (key.NeedsLastUsedUpdate(now))
        {
            await keys.TouchAsync(key.Id, now, cancellationToken);
        }

        return new ApiKeyIdentity(key.Id, account.Id, membership.Id, scopes, branches);
    }
}
