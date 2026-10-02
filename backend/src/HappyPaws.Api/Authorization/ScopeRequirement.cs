using Microsoft.AspNetCore.Authorization;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Requires that the caller holds a specific <c>resource:action</c> scope.
/// One requirement per scope; a policy may carry several when an endpoint needs all of them.
/// </summary>
public sealed class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}
