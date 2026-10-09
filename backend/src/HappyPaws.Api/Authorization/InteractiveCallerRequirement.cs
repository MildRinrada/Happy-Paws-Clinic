using HappyPaws.Application.Common.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// The caller is a person, not an API key. Part of the default and fallback policies, so it guards
/// every route that has no <see cref="RequireScopeAttribute"/>: <c>/me/*</c>, sessions and logout,
/// and the routes both staff and customers use, whose rules are decided per record.
/// <para>
/// An API key acts as its creator. Without this, a key holding only <c>appointment:read</c> could
/// still change its creator's profile, list and revoke their sessions, or enrol a second factor,
/// because those routes check that someone is signed in and nothing else. With it, a key reaches
/// only routes that name a scope, and so only what its scopes say.
/// </para>
/// </summary>
public sealed class InteractiveCallerRequirement : IAuthorizationRequirement;

public sealed class InteractiveCallerAuthorizationHandler(ICurrentUser currentUser)
    : AuthorizationHandler<InteractiveCallerRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InteractiveCallerRequirement requirement)
    {
        if (currentUser.ApiKeyId is null)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
