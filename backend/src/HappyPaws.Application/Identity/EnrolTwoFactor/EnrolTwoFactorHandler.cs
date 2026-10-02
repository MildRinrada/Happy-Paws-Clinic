using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;

namespace HappyPaws.Application.Identity.EnrolTwoFactor;

/// <summary><c>POST /me/2fa/enroll</c>. Called twice: first with the password, then with a code.</summary>
/// <param name="CurrentPassword">Required on the first call.</param>
/// <param name="Code">A code from the authenticator app; its presence makes this the second call.</param>
public sealed record EnrolTwoFactorCommand(string? CurrentPassword = null, string? Code = null);

public sealed class EnrolTwoFactorValidator : AbstractValidator<EnrolTwoFactorCommand>
{
    public EnrolTwoFactorValidator()
    {
        RuleFor(command => command.CurrentPassword).MaximumLength(256);
        RuleFor(command => command.Code).Length(6, 8).Matches("^[0-9]+$").When(command => command.Code is not null);
    }
}

/// <summary>
/// What a call returned. The first call carries <c>secret</c> and <c>otpauth_uri</c>; the second
/// carries <c>recovery_codes</c>, shown this once.
/// </summary>
/// <param name="Status"><c>confirmation_required</c> after the first call, <c>enabled</c> after the second.</param>
/// <param name="Secret">The TOTP secret to type into the app.</param>
/// <param name="OtpAuthUri">The same, as a QR code's content.</param>
/// <param name="RecoveryCodes">One-time codes for when the app is lost.</param>
public sealed record TwoFactorEnrolmentDto(
    string Status,
    string? Secret = null,
    string? OtpAuthUri = null,
    IReadOnlyList<string>? RecoveryCodes = null)
{
    public const string ConfirmationRequired = "confirmation_required";
    public const string Enabled = "enabled";
}

/// <summary>
/// Sets up the signed-in account's own second factor — "คืน TOTP secret และ recovery codes".
/// <para>
/// <b>Two calls, like accepting an invitation</b> (<c>docs/assumptions.md § 30</c>). The first
/// returns a secret; the second proves it with a code and only then switches it on. Turning on a
/// factor the person cannot produce would lock them out at their next sign-in, so it is never on
/// until it has worked once.
/// </para>
/// <para>
/// <b>The first call asks for the password.</b> The second factor is what stands between a stolen
/// password and the medical records; a stolen access token alone must not be able to plant one.
/// Replacing a factor that already works also needs a session that passed it.
/// </para>
/// <para>
/// When it succeeds, this session counts as having passed the second factor, which it just did.
/// Without that its next refresh would be refused, since an account with 2FA cannot refresh a
/// session that never passed it. Other sessions of the account keep their old standing.
/// </para>
/// </summary>
public sealed class EnrolTwoFactorHandler(
    IIdentityRepository identity,
    IPasswordHasher passwordHasher,
    ITwoFactorService twoFactor,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<TwoFactorEnrolmentDto> HandleAsync(
        EnrolTwoFactorCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var userId = currentUser.UserId ?? throw new ForbiddenException("no subject on token");
        TwoFactorEnrolmentDto? result = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var account = await identity.FindByIdAsync(userId, token)
                ?? throw new NotFoundException("account", userId);

            result = command.Code is null
                ? Begin(account, command.CurrentPassword)
                : await CompleteAsync(account, command.Code, token);
        }, cancellationToken);

        return result!;
    }

    private TwoFactorEnrolmentDto Begin(Domain.Identity.UserAccount account, string? currentPassword)
    {
        CurrentPassword.Ensure(passwordHasher, account, currentPassword);

        if (account.TwoFactorEnabled && !currentUser.TwoFactorSatisfied)
        {
            throw new ForbiddenException("replacing a second factor needs a session that passed the current one");
        }

        var label = account.Email ?? account.Id;
        var secret = account.BeginTwoFactorEnrolment(() => twoFactor.Enrol(label).Secret);

        return new TwoFactorEnrolmentDto(
            TwoFactorEnrolmentDto.ConfirmationRequired, secret, twoFactor.OtpAuthUri(label, secret));
    }

    private async Task<TwoFactorEnrolmentDto> CompleteAsync(
        Domain.Identity.UserAccount account,
        string code,
        CancellationToken cancellationToken)
    {
        var pending = account.PendingTwoFactorSecret
            ?? throw new StateConflictException("two_factor_enrolment", "not_started", "confirm");

        if (!twoFactor.VerifyCode(pending, code, clock.UtcNow))
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["code"] = ["รหัสไม่ถูกต้องหรือหมดอายุ"] });
        }

        var replaced = account.TwoFactorEnabled;
        var codes = RecoveryCodes.Generate();

        account.CompleteTwoFactorEnrolment([.. codes.Select(RecoveryCodes.Hash)]);

        if (currentUser.SessionId is { } sessionId
            && await identity.FindSessionAsync(sessionId, cancellationToken) is { } session)
        {
            session.MarkTwoFactorSatisfied();
        }

        audit.Record(new AuditEvent(
            AuditActions.TwoFactorEnrolled,
            AuditResources.UserAccount,
            account.Id,
            Details: new Dictionary<string, object?> { ["method"] = "totp", ["replaced"] = replaced }));

        return new TwoFactorEnrolmentDto(TwoFactorEnrolmentDto.Enabled, RecoveryCodes: codes);
    }
}
