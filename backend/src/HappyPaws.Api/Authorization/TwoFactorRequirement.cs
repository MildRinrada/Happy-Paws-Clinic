using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Authorization;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Requires that the access token was minted after a successful 2FA step.
/// </summary>
public sealed class TwoFactorRequirement : IAuthorizationRequirement;

/// <summary>
/// Enforces the contract's rule that staff holding medical-record-level scopes must have passed
/// 2FA.
/// <para>
/// The condition is checked against the <b>scopes on the token</b>, not against a role name or
/// <c>staff_type</c>. A clinic owner who invents a "senior nurse" role and grants it
/// <c>record:write</c> inherits the 2FA requirement automatically — which is the whole point of
/// making roles data rather than code.
/// </para>
/// </summary>
public sealed class TwoFactorAuthorizationHandler(ICurrentUser currentUser)
    : AuthorizationHandler<TwoFactorRequirement>
{
    /// <summary>
    /// Scopes that read or write clinical and financial records. Holding any of them means the
    /// session must be second-factor authenticated.
    /// </summary>
    private static readonly IReadOnlyList<string> SensitiveScopes = SecondFactorScopes.All;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TwoFactorRequirement requirement)
    {
        if (currentUser.TwoFactorSatisfied || !HoldsSensitiveScope())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private bool HoldsSensitiveScope()
    {
        var scopes = currentUser.Scopes;
        return SensitiveScopes.Any(scopes.Allows);
    }
}
