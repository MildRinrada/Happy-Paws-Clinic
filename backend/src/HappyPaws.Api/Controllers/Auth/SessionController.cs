using HappyPaws.Api.Contracts.Identity;
using HappyPaws.Api.Http;
using HappyPaws.Application.Identity.Logout;
using HappyPaws.Application.Identity.RefreshSession;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Auth;

/// <summary>
/// Session lifetime: refresh and logout.
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class SessionController(IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// <c>POST /auth/refresh</c> — rotates the refresh token on every call, per the contract.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthTokensResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensResponse>> Refresh(
        [FromServices] RefreshSessionHandler handler,
        CancellationToken cancellationToken)
    {
        // The token comes from the httpOnly cookie, never from the body: a body-supplied token
        // would have to be readable by script, which defeats the cookie flag.
        var refreshToken = RefreshTokenCookie.Read(Request) ?? string.Empty;

        var tokens = await handler.HandleAsync(new RefreshSessionCommand(refreshToken), cancellationToken);

        RefreshTokenCookie.Write(
            Response,
            tokens.RefreshToken,
            DateTimeOffset.UtcNow.Add(Domain.Identity.UserSession.Lifetime),
            secure: !environment.IsDevelopment());

        return Ok(AuthTokensResponse.From(tokens));
    }

    /// <summary><c>POST /auth/logout</c> — one session, or every device.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        [FromServices] LogoutHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new LogoutCommand(RefreshTokenCookie.Read(Request), request.AllDevices), cancellationToken);

        // Cleared unconditionally: even if the session was already gone, the browser should not
        // keep presenting a dead cookie.
        RefreshTokenCookie.Clear(Response, secure: !environment.IsDevelopment());

        return NoContent();
    }
}
