using System.Security.Claims;
using System.Text.Encodings.Web;
using HappyPaws.Api.Errors;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Queueing.Displays;
using HappyPaws.Domain.Queueing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Authenticates a waiting-room screen by its token: <c>Authorization: Bearer hpdsp_…</c>
/// (<c>docs/contract-extensions.md § 30</c>).
/// <para>
/// <b>Two routes, decided here.</b> On a route without <see cref="AcceptsDisplayTokenAttribute"/>
/// the token is refused outright (<c>401</c>), before any authorization policy, scope check or
/// <c>[AllowAnonymous]</c> handling sees a principal. It is never "a signed-in caller with no
/// scopes" anywhere else, so it cannot fall into a route that only checks that someone is signed in.
/// </para>
/// <para>
/// <b>The principal.</b> A <c>dsp</c> claim naming the screen and one <c>brn</c> claim for its
/// branch. No subject, no staff or customer membership, no scope, no 2FA claim.
/// </para>
/// </summary>
public sealed class DisplayTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    QueueDisplayAuthenticator authenticator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>The presented display token, or null when the request does not carry one.</summary>
    public static string? Presented(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();

        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[BearerPrefix.Length..].Trim();

        return token.StartsWith(QueueDisplay.TokenPrefix, StringComparison.Ordinal) ? token : null;
    }

    /// <summary>Whether the route being served is one a display token may open.</summary>
    public static bool RouteAccepts(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<AcceptsDisplayTokenAttribute>() is not null;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Presented(Request) is not { } presented)
        {
            return AuthenticateResult.NoResult();
        }

        if (!RouteAccepts(Context))
        {
            return AuthenticateResult.Fail("a display token is not accepted on this route");
        }

        var identity = await authenticator.AuthenticateAsync(presented, Context.RequestAborted);

        if (identity is null)
        {
            return AuthenticateResult.Fail("invalid, expired or revoked display token");
        }

        var claims = new List<Claim>
        {
            new(ClinicClaimTypes.Display, identity.DisplayId),
            new(ClinicClaimTypes.Branch, identity.BranchId),
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name, ClinicClaimTypes.Display, null));

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        ApiErrorWriter.WriteAsync(
            Context,
            HttpStatus.Unauthorized,
            ErrorCodes.Unauthenticated,
            "token ของจอไม่ถูกต้อง ถูกยกเลิก หมดอายุ หรือใช้กับเส้นนี้ไม่ได้",
            messageEn: "The display token is invalid, revoked or expired, or not accepted on this route.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        ApiErrorWriter.WriteAsync(
            Context,
            HttpStatus.Forbidden,
            ErrorCodes.Forbidden,
            "จอนี้ใช้เส้นนี้ไม่ได้",
            messageEn: "A display may not use this route.");
}
