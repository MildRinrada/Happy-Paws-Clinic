using HappyPaws.Api.Contracts.Identity;
using HappyPaws.Api.Http;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.Login;
using HappyPaws.Application.Identity.Register;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Auth;

/// <summary>
/// Sign-up and the shared sign-in. Staff have their own route as well
/// (<see cref="StaffAuthController"/>); this one serves customers first and anyone second.
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// <c>POST /auth/register</c> — "สมัครด้วยเบอร์โทร + OTP". Signs the new customer straight in.
    /// <c>409 contact_taken</c> when the number already has an account.
    /// </summary>
    /// <param name="request">The phone, the code sent to it, and a name.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("register")]
    [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] RegisterCustomerHandler handler,
        CancellationToken cancellationToken)
    {
        var tokens = await handler.HandleAsync(
            new RegisterCustomerCommand(
                request.Phone,
                request.Code,
                request.DisplayName,
                request.Password,
                request.Email,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        WriteCookie(tokens);

        return StatusCode(StatusCodes.Status201Created, AuthTokensResponse.From(tokens));
    }

    /// <summary>
    /// <c>POST /auth/login</c> — "ทั้งสองฝั่ง คืน capabilities ของบัญชีในคลินิกนี้".
    /// <para>
    /// A phone with a one-time code, or an email or phone with a password. An account with a second
    /// factor gets the same challenge as staff sign-in, finished at <c>POST /auth/2fa/verify</c>.
    /// </para>
    /// </summary>
    /// <param name="request">Phone and code, or identifier and password.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("login")]
    [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<TwoFactorChallengeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] LoginHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new LoginCommand(
                request.Phone,
                request.Code,
                request.Identifier,
                request.Password,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        if (result.RequiresTwoFactor)
        {
            return Ok(new TwoFactorChallengeResponse(true, result.TwoFactorChallenge!, ["totp"]));
        }

        WriteCookie(result.Tokens!);

        return Ok(AuthTokensResponse.From(result.Tokens!));
    }

    private void WriteCookie(AuthTokens tokens) =>
        RefreshTokenCookie.Write(
            Response,
            tokens.RefreshToken,
            DateTimeOffset.UtcNow.Add(Domain.Identity.UserSession.Lifetime),
            secure: !environment.IsDevelopment());
}
