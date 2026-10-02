using HappyPaws.Api.Contracts.Identity;
using HappyPaws.Api.Http;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Identity.ChangePassword;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.EnrolTwoFactor;
using HappyPaws.Application.Identity.GetMe;
using HappyPaws.Application.Identity.UpdateProfile;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Account;

/// <summary>
/// The signed-in account's own profile and credentials.
/// <para>
/// No <c>[RequireScope]</c>: these endpoints are about the caller themself, and every account —
/// staff or customer — may read and change its own profile. The fallback policy still requires
/// authentication.
/// </para>
/// </summary>
[ApiController]
[Route("me")]
public sealed class MeController : ControllerBase
{
    /// <summary><c>GET /me</c> — profile, memberships, scopes and account status.</summary>
    [HttpGet]
    [ProducesResponseType<MeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MeDto>> Get(
        [FromServices] GetMeHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));

    /// <summary>
    /// <c>GET /me/capabilities</c> — what the UI should offer this account.
    /// <para>
    /// A UX affordance, not a security boundary. The frontend hides what a cashier cannot do; the
    /// backend still refuses it if they call the endpoint directly.
    /// </para>
    /// </summary>
    [HttpGet("capabilities")]
    public ActionResult<object> Capabilities([FromServices] ICurrentUser currentUser) => Ok(new
    {
        scopes = currentUser.Scopes.ToArray(),
        branches = currentUser.BranchIds,
        two_factor_satisfied = currentUser.TwoFactorSatisfied,
    });

    /// <summary><c>POST /me/password</c> — changes the password and revokes every other session.</summary>
    [HttpPost("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ChangePasswordHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// <c>PATCH /me</c> — "ชื่อ อีเมล เบอร์ ภาษา การแจ้งเตือน". Returns the profile as <c>GET /me</c> does.
    /// A new email or phone needs <c>current_password</c>.
    /// </summary>
    /// <param name="request">The fields to change.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="me">Reads the profile back.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch]
    [ProducesResponseType<MeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MeDto>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] UpdateProfileHandler handler,
        [FromServices] GetMeHandler me,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new UpdateProfileCommand(
                request.DisplayName,
                request.Email,
                request.Phone,
                request.Language,
                request.Notifications?.Email,
                request.Notifications?.Sms,
                request.Notifications?.Line,
                request.CurrentPassword,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        var profile = await me.HandleAsync(cancellationToken);
        Response.Headers.ETag = IfMatch.ToETag(profile.Version);

        return Ok(profile);
    }

    /// <summary>
    /// <c>POST /me/2fa/enroll</c> — "คืน TOTP secret และ recovery codes". Two calls: the first, with
    /// the password, returns the secret; the second, with a code from the app, switches it on and
    /// returns the recovery codes, shown this once.
    /// </summary>
    /// <param name="request">The password, or the code.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("2fa/enroll")]
    [ProducesResponseType<TwoFactorEnrolmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TwoFactorEnrolmentDto>> EnrolTwoFactor(
        [FromBody] EnrolTwoFactorRequest request,
        [FromServices] EnrolTwoFactorHandler handler,
        CancellationToken cancellationToken)
    {
        // Secrets and recovery codes are credentials; no intermediary may keep a copy.
        Response.Headers.CacheControl = "no-store";

        return Ok(await handler.HandleAsync(
            new EnrolTwoFactorCommand(request.CurrentPassword, request.Code), cancellationToken));
    }
}
