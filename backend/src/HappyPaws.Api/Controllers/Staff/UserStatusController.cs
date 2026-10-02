using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Identity;
using HappyPaws.Api.Http;
using HappyPaws.Application.Identity.SetUserStatus;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Whether an account may sign in — "สถานะบัญชี" in the Account และ Access box.
/// <para>
/// <c>user:manage</c> is a sensitive scope, so the caller's session must have passed 2FA.
/// </para>
/// </summary>
[ApiController]
[Route("staff/users")]
public sealed class UserStatusController : ControllerBase
{
    /// <summary><c>PATCH /staff/users/{id}/status</c> — "active · suspended · deactivated".</summary>
    /// <param name="id">The account id (<c>usr_…</c>).</param>
    /// <param name="request">The new status.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPatch("{id}/status")]
    [RequireScope(Scopes.User.Manage)]
    [ProducesResponseType<UserStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserStatusDto>> SetStatus(
        string id,
        [FromBody] SetUserStatusRequest request,
        [FromServices] SetUserStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var status = await handler.HandleAsync(
            new SetUserStatusCommand(id, request.Status, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(status.Version);

        return Ok(status);
    }
}
