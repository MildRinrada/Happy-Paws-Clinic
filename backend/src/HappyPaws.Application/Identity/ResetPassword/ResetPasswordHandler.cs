using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Identity.ResetPassword;

/// <summary>
/// <c>POST /auth/password/reset</c> — "ลิงก์อายุ 30 นาที ใช้ครั้งเดียว". One endpoint, called twice,
/// like accepting an invitation: first with <c>Identifier</c> (an email or phone) to be sent a link,
/// then with the link's <c>Token</c> and a <c>NewPassword</c>.
/// </summary>
public sealed record ResetPasswordCommand(string? Identifier = null, string? Token = null, string? NewPassword = null);

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(command => command)
            .Must(command => (command.Identifier is not null) != (command.Token is not null))
            .WithName("token")
            .WithMessage("ส่ง identifier เพื่อขอลิงก์ หรือ token คู่กับ new_password เพื่อตั้งรหัสใหม่");

        RuleFor(command => command.Identifier).MaximumLength(256);

        RuleFor(command => command.NewPassword)
            .NotEmpty().MinimumLength(8).MaximumLength(256)
            .When(command => command.Token is not null);
    }
}

/// <summary>What happened: <c>link_sent</c> after the first call, <c>reset</c> after the second.</summary>
public sealed record ResetPasswordResultDto(string Status);

/// <summary>
/// Sends a reset link, then honours it.
/// <para>
/// <b>Asking always answers <c>link_sent</c></b>, whether or not the email or number belongs to
/// anyone: the honest answer would tell a stranger who is a customer here. A link goes by email
/// when the account has one, otherwise by SMS. At most three links per account per ten minutes; the
/// rest are quietly not sent.
/// </para>
/// <para>
/// <b>Using the link</b> sets the password and ends every session the account has, everywhere,
/// because the reason someone resets a password is often that someone else knows it. A link that is
/// wrong, used or past its thirty minutes is <c>422</c> on <c>token</c>, one message for all three.
/// A second factor is untouched: a reset is not a way around the authenticator.
/// </para>
/// </summary>
public sealed class ResetPasswordHandler(
    IIdentityRepository identity,
    ICustomerSignInRepository signIn,
    IPasswordResetSender sender,
    IPasswordHasher passwordHasher,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock,
    ILogger<ResetPasswordHandler> logger)
{
    public const int MaxLinksPerWindow = 3;

    public async Task<ResetPasswordResultDto> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        return command.Token is { } token
            ? await ResetAsync(token, command.NewPassword!, cancellationToken)
            : await SendLinkAsync(command.Identifier!, cancellationToken);
    }

    private async Task<ResetPasswordResultDto> SendLinkAsync(string identifier, CancellationToken cancellationToken)
    {
        var sent = new ResetPasswordResultDto("link_sent");
        var account = await FindAsync(identifier.Trim(), cancellationToken);
        var now = clock.UtcNow;

        if (account is not { CanAuthenticate: true })
        {
            logger.LogInformation("Password reset asked for an identifier with no usable account");
            return sent;
        }

        if (await signIn.ResetsRequestedSinceAsync(account.Id, now - OtpSettings.Window, cancellationToken) >= MaxLinksPerWindow)
        {
            logger.LogWarning("Password reset links for {UserId} are over the limit; not sending another", account.Id);
            return sent;
        }

        var token = OneTimeSecrets.NewToken();
        var request = PasswordResetRequest.Issue(account.Id, OneTimeSecrets.HashToken(token), now);

        signIn.AddResetRequest(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sender.SendAsync(
            new PasswordResetMessage(account.Id, account.Email, account.Email is null ? account.PhoneNumber : null, token, request.ExpiresAt),
            cancellationToken);

        return sent;
    }

    private async Task<ResetPasswordResultDto> ResetAsync(string token, string newPassword, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var now = clock.UtcNow;
            var request = await signIn.FindResetAsync(OneTimeSecrets.HashToken(token), ct);

            if (request is null || !request.IsUsable(now)
                || await identity.FindByIdAsync(request.UserId, ct) is not { CanAuthenticate: true } account)
            {
                throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["token"] = ["ลิงก์ไม่ถูกต้อง ใช้ไปแล้ว หรือหมดอายุ กรุณาขอลิงก์ใหม่"],
                });
            }

            request.MarkUsed(now);

            // SetPassword stamps the account, which is what ends every existing session.
            account.SetPassword(passwordHasher.Hash(newPassword), now);

            audit.Record(new AuditEvent(AuditActions.PasswordReset, AuditResources.UserAccount, account.Id));
        }, cancellationToken);

        return new ResetPasswordResultDto("reset");
    }

    private async Task<UserAccount?> FindAsync(string identifier, CancellationToken cancellationToken) =>
        identifier.Contains('@', StringComparison.Ordinal)
            ? await identity.FindByEmailAsync(UserAccount.NormaliseEmail(identifier), cancellationToken)
            : UserAccount.NormalisePhone(identifier) is { } phone
                ? await identity.FindByPhoneAsync(phone, cancellationToken)
                : null;
}
