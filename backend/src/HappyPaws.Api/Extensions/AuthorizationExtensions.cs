using HappyPaws.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace HappyPaws.Api.Extensions;

/// <summary>
/// Scope- and 2FA-based authorization.
/// </summary>
public static class AuthorizationExtensions
{
    public static IServiceCollection AddClinicAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, ScopePolicyProvider>();

        // Scoped, not singleton: both handlers read the per-request ICurrentUser.
        services.AddScoped<IAuthorizationHandler, ScopeAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, TwoFactorAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, InteractiveCallerAuthorizationHandler>();
        services.AddScoped<IAuthorizationMiddlewareResultHandler, ScopeAuthorizationResultHandler>();

        services.AddAuthorization(options =>
        {
            // Everything is protected unless an endpoint opts out with [AllowAnonymous].
            // This inverts the failure mode: forgetting an attribute locks an endpoint down
            // instead of leaving it open. The endpoints that legitimately opt out are listed in
            // docs/authorization.md.
            //
            // Both also refuse API keys: a route with no scope on it is for people (InteractiveCallerRequirement).
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new InteractiveCallerRequirement())
                .Build();

            options.DefaultPolicy = options.FallbackPolicy;
        });

        return services;
    }
}
