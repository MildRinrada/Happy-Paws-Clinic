using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Staff;
using HappyPaws.Application.Staff.AcceptInvitation;
using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Application.Staff.InviteStaffMember;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// Inviting someone onto the staff, and their accepting.
/// <para>
/// The two routes have opposite callers. Inviting needs <c>staff:manage</c>; accepting is done by
/// someone who has no account yet, so it is <c>[AllowAnonymous]</c> and verifies the link's token
/// itself. It is one of the endpoints the fallback policy does not protect — see
/// <c>docs/authentication.md § 5</c>.
/// </para>
/// </summary>
[ApiController]
[Route("staff/members/invitations")]
public sealed class StaffInvitationsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/members/invitations</c> — "ส่งอีเมล/SMS เชิญ · ลิงก์อายุ 7 วัน".
    /// <para>
    /// The response describes the invitation and never contains the link: that goes to the
    /// invitee only. Inviting an address that already has an open invitation re-issues it and
    /// kills the old link.
    /// </para>
    /// </summary>
    /// <param name="request">Who, as what, with which roles at which branches.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [RequireScope(Scopes.Staff.Manage)]
    [ProducesResponseType<StaffMemberDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<StaffMemberDto> Invite(
        [FromBody] InviteStaffMemberRequest request,
        [FromServices] InviteStaffMemberHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new InviteStaffMemberCommand(
                request.Email,
                request.DisplayName,
                request.StaffType,
                request.Roles,
                request.Branches,
                request.LicenseNo),
            cancellationToken);

    /// <summary>
    /// <c>POST /staff/members/invitations/{id}/accept</c> — "ผู้ถูกเชิญตั้งรหัสผ่านและ 2FA".
    /// <para>
    /// Called twice. With the token alone it answers <c>two_factor_setup_required</c> and a TOTP
    /// secret; with a password and a code from the authenticator app it answers <c>accepted</c>.
    /// No session is issued either time — the new member then signs in the ordinary way.
    /// </para>
    /// <para>
    /// A link that is unknown, wrong, expired or already used answers the same <c>404</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Invitation id, from the link.</param>
    /// <param name="request">The link's token, and on the second call a password and a code.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/accept")]
    [AllowAnonymous]
    [ProducesResponseType<AcceptInvitationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<AcceptInvitationResult> Accept(
        string id,
        [FromBody] AcceptInvitationRequest request,
        [FromServices] AcceptInvitationHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new AcceptInvitationCommand(id, request.Token, request.Password, request.Code),
            cancellationToken);
}
