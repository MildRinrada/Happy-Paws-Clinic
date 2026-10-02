using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Identity;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Staff.AcceptInvitation;

/// <summary>
/// <c>POST /staff/members/invitations/{id}/accept</c> — "ผู้ถูกเชิญตั้งรหัสผ่านและ 2FA".
/// </summary>
/// <param name="InvitationId">From the link's path.</param>
/// <param name="Token">From the link. The only credential the caller has.</param>
/// <param name="Password">The password being set. Required on the completing call.</param>
/// <param name="Code">A code from the authenticator app. Sending it is what completes the acceptance.</param>
public sealed record AcceptInvitationCommand(
    string InvitationId,
    string Token,
    string? Password = null,
    string? Code = null);

public sealed class AcceptInvitationValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(128);

        When(command => command.Code is not null, () =>
        {
            RuleFor(command => command.Code).NotEmpty().Length(6, 8).Matches("^[0-9]+$");

            // The same bounds POST /me/password applies.
            RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
        });
    }
}

/// <summary>
/// What accepting returned.
/// <para>
/// Two shapes on one endpoint, told apart by <c>Status</c>. <c>two_factor_setup_required</c>
/// carries the secret to load into an authenticator app; <c>accepted</c> carries the ids of what
/// was created. Neither carries a session — the new member signs in through
/// <c>POST /auth/staff/login</c> like everyone else.
/// </para>
/// </summary>
public sealed record AcceptInvitationResult(
    string Status,
    string Email,
    string? UserId = null,
    string? StaffId = null,
    TwoFactorSetupDto? TwoFactor = null)
{
    public const string SetupRequired = "two_factor_setup_required";
    public const string Accepted = "accepted";
}

/// <param name="Secret">Base32, for typing in by hand when a camera is not to hand.</param>
/// <param name="OtpAuthUri">What the QR code encodes.</param>
public sealed record TwoFactorSetupDto(string Secret, string OtpAuthUri);

/// <summary>
/// Turns an invitation into a login.
/// <para>
/// <b>Two calls, deliberately.</b> The first, with only the token, returns a TOTP secret. The
/// second, with a password and a code generated from that secret, creates the account. Doing it in
/// one call would mean returning a secret and enrolling it in the same breath, so an invitee who
/// lost that single response — closed the tab, dropped the connection — would own an account
/// protected by a second factor they never received, with no endpoint that could help them. This
/// way the second factor is on the account only once it has been shown to work.
/// </para>
/// <para>
/// Every way the link can be wrong answers the same <c>404</c>: unknown id, wrong token, expired,
/// already used. An anonymous caller is told nothing about which invitations exist.
/// </para>
/// <para>
/// No actor is recorded on the audit row. The caller has no token; the invitation id in the row is
/// what ties the new account to whoever issued the invitation.
/// </para>
/// </summary>
public sealed class AcceptInvitationHandler(
    IStaffInvitationRepository invitations,
    IStaffRepository staff,
    IIdentityRepository identity,
    IPasswordHasher passwordHasher,
    ITwoFactorService twoFactor,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock,
    ILogger<AcceptInvitationHandler> logger)
{
    public async Task<AcceptInvitationResult> HandleAsync(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var now = clock.UtcNow;
        AcceptInvitationResult? result = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var invitation = await invitations.FindTrackedByIdAsync(command.InvitationId, token);

            if (invitation is null
                || !InvitationTokens.Matches(command.Token, invitation.TokenHash)
                || !invitation.IsLiveAt(now))
            {
                logger.LogWarning("Refused staff invitation {InvitationId}", command.InvitationId);
                throw new NotFoundException("staff_invitation", command.InvitationId);
            }

            result = command.Code is null
                ? BeginSetup(invitation)
                : await CompleteAsync(invitation, command.Password!, command.Code, now, token);
        }, cancellationToken);

        return result!;
    }

    private AcceptInvitationResult BeginSetup(StaffInvitation invitation)
    {
        var secret = invitation.BeginTwoFactorSetup(() => twoFactor.Enrol(invitation.Email).Secret);

        return new AcceptInvitationResult(
            AcceptInvitationResult.SetupRequired,
            invitation.Email,
            TwoFactor: new TwoFactorSetupDto(secret, twoFactor.OtpAuthUri(invitation.Email, secret)));
    }

    private async Task<AcceptInvitationResult> CompleteAsync(
        StaffInvitation invitation,
        string password,
        string code,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (invitation.PendingTwoFactorSecret is not { } secret || !twoFactor.VerifyCode(secret, code, now))
        {
            // 422 rather than 401: the caller is who the link says they are, and has mistyped six
            // digits or skipped the setup call. Pointing at the field is the useful answer.
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["code"] = ["รหัสยืนยันไม่ถูกต้อง"] });
        }

        if (await identity.FindByEmailAsync(invitation.Email, cancellationToken) is not null)
        {
            // Created by another route between the invitation and now. The unique index on email
            // would refuse the insert anyway; this says why.
            throw new StateConflictException(nameof(StaffInvitation), "superseded", "accept");
        }

        var account = UserAccount.CreateStaff(invitation.DisplayName, invitation.Email, passwordHasher.Hash(password));
        account.EnrolTwoFactor(TwoFactorMethod.Totp, secret);

        var membership = StaffMembership.Create(
            account.Id,
            invitation.StaffType,
            invitation.RoleIds,
            invitation.BranchIds,
            invitation.LicenseNo);

        identity.AddAccount(account);
        staff.Add(membership);
        invitation.Accept(account.Id, now);

        audit.Record(new AuditEvent(
            AuditActions.StaffInvitationAccepted,
            AuditResources.StaffMember,
            membership.Id,
            Details: new Dictionary<string, object?>
            {
                ["invitation_id"] = invitation.Id,
                ["user_id"] = account.Id,
                ["invited_by_staff_id"] = invitation.InvitedByStaffId,
            }));

        return new AcceptInvitationResult(
            AcceptInvitationResult.Accepted, invitation.Email, account.Id, membership.Id);
    }
}
