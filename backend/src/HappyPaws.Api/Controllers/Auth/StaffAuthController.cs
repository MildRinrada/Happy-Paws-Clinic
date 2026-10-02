using HappyPaws.Api.Contracts.Identity;
using HappyPaws.Api.Http;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.StaffLogin;
using HappyPaws.Application.Identity.VerifyTwoFactor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Auth;

/// <summary>
/// Staff sign-in: email and password, then a second factor.
/// <para>
/// The controller binds, delegates and maps. The rules — timing-equalised credential checks,
/// membership status, challenge lifetime — live in the handlers, where they can be tested without
/// an HTTP stack.
/// </para>
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class StaffAuthController(IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// <c>POST /auth/staff/login</c>. Returns a 2FA challenge when the account is enrolled, or a
    /// completed session when it is not.
    /// </summary>
    [HttpPost("staff/login")]
    [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<TwoFactorChallengeResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] StaffLoginRequest request,
        [FromServices] StaffLoginHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new StaffLoginCommand(
                request.Email,
                request.Password,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        if (result.RequiresTwoFactor)
        {
            // No cookie and no bearer token yet: a challenge must not be usable as a credential
            // anywhere in the system.
            return Ok(new TwoFactorChallengeResponse(true, result.TwoFactorChallenge!, ["totp"]));
        }

        return Completed(result.Tokens!);
    }

    /// <summary><c>POST /auth/2fa/verify</c> — the step that returns the real access token.</summary>
    [HttpPost("2fa/verify")]
    [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyTwoFactor(
        [FromBody] VerifyTwoFactorRequest request,
        [FromServices] VerifyTwoFactorHandler handler,
        CancellationToken cancellationToken)
    {
        var tokens = await handler.HandleAsync(
            new VerifyTwoFactorCommand(request.Challenge, request.Code), cancellationToken);

        return Completed(tokens);
    }

    private IActionResult Completed(AuthTokens tokens)
    {
        RefreshTokenCookie.Write(
            Response,
            tokens.RefreshToken,
            DateTimeOffset.UtcNow.Add(Domain.Identity.UserSession.Lifetime),
            secure: !environment.IsDevelopment());

        return Ok(AuthTokensResponse.From(tokens));
    }
}
