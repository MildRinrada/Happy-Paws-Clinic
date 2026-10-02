using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Materialises an authorization policy for any <c>scope:&lt;resource&gt;:&lt;action&gt;</c> policy
/// name produced by <see cref="RequireScopeAttribute"/>, falling back to the default provider for
/// everything else.
/// <para>
/// Without this, each of the contract's ~70 scopes would need a hand-written <c>AddPolicy</c> line
/// in <c>Program.cs</c>, and adding an endpoint would mean editing a central registry — the kind of
/// file everyone forgets to update.
/// </para>
/// <para>
/// Every generated policy also carries <see cref="TwoFactorRequirement"/>, so the 2FA rule cannot
/// be forgotten on an endpoint that happens to guard a medical scope.
/// </para>
/// </summary>
public sealed class ScopePolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequireScopeAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var scope = policyName[RequireScopeAttribute.PolicyPrefix.Length..];

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new ScopeRequirement(scope), new TwoFactorRequirement())
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
