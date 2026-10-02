using HappyPaws.Api.Contracts.Identity;
using HappyPaws.Application.Identity.ResetPassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Auth;

/// <summary>Forgotten passwords, for staff and customers alike.</summary>
[ApiController]
[Route("auth/password")]
[AllowAnonymous]
public sealed class PasswordController : ControllerBase
{
    /// <summary>
    /// <c>POST /auth/password/reset</c> — "ลิงก์อายุ 30 นาที ใช้ครั้งเดียว".
    /// <para>
    /// With <c>identifier</c>: <c>202 link_sent</c>, always. With <c>token</c> and
    /// <c>new_password</c>: <c>200 reset</c>, and every session the account had is ended.
    /// </para>
    /// </summary>
    /// <param name="request">An identifier, or a token and the new password.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("reset")]
    [ProducesResponseType<ResetPasswordResultDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ResetPasswordResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reset(
        [FromBody] ResetPasswordRequest request,
        [FromServices] ResetPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ResetPasswordCommand(request.Identifier, request.Token, request.NewPassword),
            cancellationToken);

        return result.Status == "reset" ? Ok(result) : Accepted(result);
    }
}
