using System.IdentityModel.Tokens.Jwt;
using System.Text;
using HappyPaws.Api.Authorization;
using HappyPaws.Api.Errors;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HappyPaws.Api.Extensions;

/// <summary>
/// Bearer token validation for this clinic instance: a person's JWT access token, a machine
/// client's API key (<c>hp_…</c>), or a waiting-room screen's display token (<c>hpdsp_…</c>,
/// <c>docs/contract-extensions.md § 30</c>). All arrive as <c>Authorization: Bearer</c>; a policy
/// scheme looks at the token's prefix and forwards to the one that can read it.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>The default scheme: picks JWT or API key per request.</summary>
    public const string ClinicScheme = "Clinic";

    public const string ApiKeyScheme = "ApiKey";

    /// <summary>A waiting-room screen's token, <c>hpdsp_…</c> (<c>docs/contract-extensions.md § 30</c>).</summary>
    public const string DisplayScheme = "DisplayToken";

    public static IServiceCollection AddClinicAuthentication(this IServiceCollection services)
    {
        // Stops the handler renaming inbound claims (sub -> ClaimTypes.NameIdentifier and friends).
        // With mapping on, CurrentUser would look for "sub" and find nothing, producing a caller
        // with no id and no scopes — which fails open in exactly the places that matter.
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services
            .AddAuthentication(ClinicScheme)
            .AddPolicyScheme(ClinicScheme, "JWT, API key or display token", options => options.ForwardDefaultSelector = context =>
                DisplayTokenAuthenticationHandler.Presented(context.Request) is not null
                    ? DisplayScheme
                    : ApiKeyAuthenticationHandler.Presented(context.Request) is not null
                        ? ApiKeyScheme
                        : JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyScheme, displayName: null, _ => { })
            .AddScheme<AuthenticationSchemeOptions, DisplayTokenAuthenticationHandler>(DisplayScheme, displayName: null, _ => { });

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.SaveToken = false;

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

                    // Pinned so a token cannot arrive claiming "alg": "none" or a different
                    // algorithm than the one we sign with.
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),

                    NameClaimType = ClinicClaimTypes.Subject,
                };

                // Policy failures are answered by ScopeAuthorizationResultHandler. This covers the
                // paths that challenge before authorization runs — a malformed or expired token —
                // so those also return the shared envelope rather than an empty 401.
                bearer.Events = new JwtBearerEvents
                {
                    OnChallenge = async challengeContext =>
                    {
                        challengeContext.HandleResponse();

                        await ApiErrorWriter.WriteAsync(
                            challengeContext.HttpContext,
                            HttpStatus.Unauthorized,
                            ErrorCodes.Unauthenticated,
                            "กรุณาเข้าสู่ระบบใหม่",
                            messageEn: "Authentication required or token expired.");
                    },
                };
            });

        return services;
    }
}
