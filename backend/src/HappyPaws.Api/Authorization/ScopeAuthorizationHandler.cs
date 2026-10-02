using HappyPaws.Application.Common.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Evaluates a <see cref="ScopeRequirement"/> against the scopes on the access token.
/// <para>
/// The wildcard rules live in <c>Domain.Access.ScopeSet</c>, not here, so the security boundary
/// and every other consumer of scopes agree on what <c>consult:*</c> means.
/// </para>
/// <para>
/// This is the enforcing gate for scope-level access only. Ownership and branch checks are
/// resource-level and belong to the Application layer, because they need the data. Both are
/// required; neither substitutes for the other.
/// </para>
/// </summary>
public sealed class ScopeAuthorizationHandler(ICurrentUser currentUser)
    : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        if (currentUser.Scopes.Allows(requirement.Scope))
        {
            context.Succeed(requirement);
        }

        // Deliberately no context.Fail(): leaving the requirement unmet lets
        // ScopeAuthorizationResultHandler answer 403 insufficient_scope and name what was missing,
        // which the contract requires. Calling Fail() would short-circuit that.
        return Task.CompletedTask;
    }
}
