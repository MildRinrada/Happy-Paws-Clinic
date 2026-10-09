using System.Security.Claims;
using System.Text.Encodings.Web;
using HappyPaws.Api.Errors;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Integrations.ApiKeys;
using HappyPaws.Domain.Integrations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Authenticates a machine client by its API key: <c>Authorization: Bearer hp_…</c>, the same
/// header a person's access token uses (the contract defines no other), told apart by the
/// <c>hp_</c> prefix (see <c>AuthenticationExtensions</c>).
/// <para>
/// The principal it builds has the shape of a staff token: the creator's account and staff
/// membership, the key's effective scopes and branches (<see cref="ApiKeyAuthenticator"/>), and a
/// <c>key</c> claim naming the key. It never carries the 2FA claim; an API key cannot hold a scope
/// that needs it. The <c>key</c> claim is what keeps a key off every route that is for people
/// (<see cref="InteractiveCallerRequirement"/>), partitions its rate limit, and marks its audit rows.
/// </para>
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiKeyAuthenticator authenticator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>The presented key, or null when the request does not carry one.</summary>
    public static string? Presented(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();

        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[BearerPrefix.Length..].Trim();

        return token.StartsWith(ApiKey.KeyPrefix, StringComparison.Ordinal) ? token : null;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Presented(Request) is not { } presented)
        {
            return AuthenticateResult.NoResult();
        }

        var identity = await authenticator.AuthenticateAsync(presented, Context.RequestAborted);

        if (identity is null)
        {
            return AuthenticateResult.Fail("invalid, expired or revoked API key");
        }

        var claims = new List<Claim>
        {
            new(ClinicClaimTypes.Subject, identity.UserId),
            new(ClinicClaimTypes.StaffId, identity.StaffId),
            new(ClinicClaimTypes.ApiKey, identity.KeyId),
        };

        claims.AddRange(identity.BranchIds.Select(branch => new Claim(ClinicClaimTypes.Branch, branch)));
        claims.AddRange(identity.Scopes.Select(scope => new Claim(ClinicClaimTypes.Scope, scope)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name, ClinicClaimTypes.Subject, null));

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        ApiErrorWriter.WriteAsync(
            Context,
            HttpStatus.Unauthorized,
            ErrorCodes.Unauthenticated,
            "API key ไม่ถูกต้อง หมดอายุ หรือถูกยกเลิกแล้ว",
            messageEn: "The API key is invalid, expired or revoked.");
}
