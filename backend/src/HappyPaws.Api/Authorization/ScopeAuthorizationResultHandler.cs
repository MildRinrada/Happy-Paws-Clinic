using HappyPaws.Api.Errors;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Turns an authorization failure into the shared error envelope instead of ASP.NET Core's empty
/// 401/403 body.
/// <para>
/// The contract requires a 403 to name the scopes that were missing
/// ("พร้อมบอก scope ที่ขาด"), which the framework's default result cannot do. It also requires the
/// 401/403 distinction to be honest: 401 means "your token is expired or revoked, refresh it",
/// 403 means "your token is fine but you may not do this". The frontend branches on exactly that
/// difference — it retries a refresh on 401 and shows a permission message on 403 — so blurring
/// them produces an infinite refresh loop.
/// </para>
/// </summary>
public sealed class ScopeAuthorizationResultHandler(ICurrentUser currentUser) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged || (authorizeResult.Forbidden && !currentUser.IsAuthenticated))
        {
            await ApiErrorWriter.WriteAsync(
                context,
                HttpStatus.Unauthorized,
                ErrorCodes.Unauthenticated,
                "กรุณาเข้าสู่ระบบใหม่",
                messageEn: "Authentication required or token expired.");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var missingScopes = policy.Requirements
                .OfType<ScopeRequirement>()
                .Select(requirement => requirement.Scope)
                .Where(scope => !currentUser.Scopes.Allows(scope))
                .ToArray();

            if (missingScopes.Length > 0)
            {
                await ApiErrorWriter.WriteAsync(
                    context,
                    HttpStatus.Forbidden,
                    ErrorCodes.InsufficientScope,
                    "สิทธิ์ไม่เพียงพอสำหรับการดำเนินการนี้",
                    messageEn: "The token is missing one or more required scopes.",
                    details: new Dictionary<string, object> { ["missing_scopes"] = missingScopes });
                return;
            }

            // Scopes were fine, so the refusal came from another requirement on the policy —
            // today that means 2FA. Say so, because "forbidden" alone leaves the user with no
            // action to take, whereas this tells them to enrol.
            if (policy.Requirements.OfType<TwoFactorRequirement>().Any())
            {
                await ApiErrorWriter.WriteAsync(
                    context,
                    HttpStatus.Forbidden,
                    ErrorCodes.Forbidden,
                    "ต้องยืนยันตัวตนสองขั้นตอนก่อนใช้งานส่วนนี้",
                    messageEn: "Two-factor authentication is required for this scope level.",
                    details: new Dictionary<string, object> { ["required"] = "two_factor" });
                return;
            }
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
