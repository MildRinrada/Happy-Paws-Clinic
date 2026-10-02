using FluentValidation;
using HappyPaws.Application.Access;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Staff.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Staff;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Staff.InviteStaffMember;

/// <summary>
/// <c>POST /staff/members/invitations</c> — "ส่งอีเมล/SMS เชิญ · ลิงก์อายุ 7 วัน".
/// </summary>
/// <param name="Email">Where the link goes, and what the account will sign in with.</param>
/// <param name="DisplayName">The person's name as it will appear on records they sign.</param>
/// <param name="StaffType">Their position — a label, never an access decision.</param>
/// <param name="Roles">Role names, as <c>GET /me</c> prints them.</param>
/// <param name="Branches">Branch ids they will work at.</param>
/// <param name="LicenseNo">Professional licence number, for vets.</param>
public sealed record InviteStaffMemberCommand(
    string Email,
    string DisplayName,
    string StaffType,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string>? Branches = null,
    string? LicenseNo = null);

public sealed class InviteStaffMemberValidator : AbstractValidator<InviteStaffMemberCommand>
{
    public InviteStaffMemberValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.StaffType).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Roles).NotEmpty().WithMessage("ต้องระบุ role อย่างน้อยหนึ่งรายการ");
        RuleFor(command => command.LicenseNo).MaximumLength(64);
    }
}

/// <summary>
/// Invites someone to join the clinic's staff.
/// <para>
/// Nothing the invitee can use exists until they accept: no account, no membership, no session.
/// What is written is the offer — who, as what, with which roles at which branches — and the hash
/// of a link that expires in seven days.
/// </para>
/// <para>
/// Inviting an address that already has an open invitation re-issues it. The old link dies, the
/// new one is sent, and there is still one row to answer "have we invited them". That is also the
/// way to correct a role chosen by mistake before anyone accepts.
/// </para>
/// <para>
/// <b>The link goes out through a port with no real adapter behind it yet.</b> Until an email
/// channel exists the message is logged rather than delivered — see
/// <c>LoggingStaffInvitationSender</c>.
/// </para>
/// </summary>
public sealed class InviteStaffMemberHandler(
    IStaffInvitationRepository invitations,
    IIdentityRepository identity,
    StaffAssignmentResolver assignments,
    ScopeGrantPolicy grants,
    IStaffInvitationSender sender,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock,
    ILogger<InviteStaffMemberHandler> logger)
{
    public async Task<StaffMemberDto> HandleAsync(
        InviteStaffMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var invitedBy = currentUser.RequireStaffId();
        var email = command.Email.Trim().ToLowerInvariant();
        var now = clock.UtcNow;

        // Read-only lookups that feed a decision, not a write — safe outside the transaction.
        var roles = await assignments.RolesAsync(command.Roles, cancellationToken);
        var branchIds = await assignments.BranchesAsync(command.Branches ?? [], cancellationToken);

        grants.EnsureCallerHolds(roles.SelectMany(role => role.Scopes));

        var token = InvitationTokens.Generate();
        StaffInvitation? invitation = null;

        await unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            if (await identity.FindByEmailAsync(email, transaction) is not null)
            {
                // An address that already signs in here is not invited a second time. Rehiring
                // someone deactivated is a change to their existing account, not a new one.
                throw new StaffAlreadyExistsException();
            }

            var roleIds = roles.Select(role => role.Id).ToArray();
            var open = await invitations.FindOpenByEmailAsync(email, transaction);

            if (open is null)
            {
                invitation = StaffInvitation.Send(
                    email,
                    command.DisplayName,
                    command.StaffType,
                    roleIds,
                    branchIds,
                    command.LicenseNo,
                    InvitationTokens.Hash(token),
                    invitedBy,
                    now);

                invitations.Add(invitation);
            }
            else
            {
                open.Reissue(
                    command.DisplayName,
                    command.StaffType,
                    roleIds,
                    branchIds,
                    command.LicenseNo,
                    InvitationTokens.Hash(token),
                    invitedBy,
                    now);

                invitation = open;
            }

            audit.Record(new AuditEvent(
                AuditActions.StaffInvited,
                AuditResources.StaffInvitation,
                invitation.Id,
                Details: new Dictionary<string, object?>
                {
                    ["email"] = invitation.Email,
                    ["staff_type"] = invitation.StaffType,
                    ["roles"] = roles.Select(role => role.Name).ToArray(),
                    ["branches"] = branchIds,
                    ["expires_at"] = invitation.ExpiresAt,
                }));
        }, cancellationToken);

        await DeliverAsync(invitation!, token, cancellationToken);

        return StaffMemberDto.From(invitation!, roles.ToDictionary(role => role.Id, role => role.Name));
    }

    private async Task DeliverAsync(StaffInvitation invitation, string token, CancellationToken cancellationToken)
    {
        try
        {
            await sender.SendAsync(
                new StaffInvitationMessage(
                    invitation.Id, token, invitation.Email, invitation.DisplayName, invitation.ExpiresAt),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The invitation is recorded either way, and inviting again re-issues it. Failing the
            // request would leave the row written and the caller believing nothing happened.
            logger.LogError(exception, "Staff invitation {InvitationId} could not be delivered", invitation.Id);
        }
    }
}

/// <summary>
/// <c>409 staff_already_exists</c> — the address already belongs to an account in this clinic.
/// Not in the API reference's error list.
/// </summary>
public sealed class StaffAlreadyExistsException : AppException
{
    public StaffAlreadyExistsException()
        : base("staff_already_exists", "อีเมลนี้มีบัญชีในคลินิกอยู่แล้ว", HttpStatus.Conflict)
    {
        MessageEn = "An account with this email address already exists in this clinic.";
        Field = "email";
    }
}
