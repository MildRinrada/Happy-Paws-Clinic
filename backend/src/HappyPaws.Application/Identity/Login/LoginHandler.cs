using FluentValidation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.RequestOtp;
using HappyPaws.Application.Identity.Sessions;
using HappyPaws.Application.Identity.StaffLogin;
using HappyPaws.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Identity.Login;

/// <summary>
/// <c>POST /auth/login</c> — "ทั้งสองฝั่ง คืน capabilities ของบัญชีในคลินิกนี้".
/// <para>
/// Two ways in. <c>Phone</c> with <c>Code</c> signs in with a one-time code from
/// <c>POST /auth/otp/request</c>; <c>Identifier</c> (an email or a phone number) with
/// <c>Password</c> signs in with a password. Exactly one of the two.
/// </para>
/// </summary>
public sealed record LoginCommand(
    string? Phone = null,
    string? Code = null,
    string? Identifier = null,
    string? Password = null,
    string? UserAgent = null,
    string? IpAddress = null);

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(command => command)
            .Must(command => (command.Code is not null) != (command.Password is not null))
            .WithName("code")
            .WithMessage("ส่ง phone คู่กับ code หรือ identifier คู่กับ password อย่างใดอย่างหนึ่ง");

        RuleFor(command => command.Phone).NotEmpty().When(command => command.Code is not null);
        RuleFor(command => command.Identifier).NotEmpty().MaximumLength(256).When(command => command.Password is not null);
        RuleFor(command => command.Password).MaximumLength(256);
    }
}

/// <summary>
/// The shared sign-in, for whoever holds the account: a customer, a member of staff, or someone who
/// is both.
/// <para>
/// <b>A second factor still applies.</b> An account with one enrolled gets the same challenge as
/// <c>POST /auth/staff/login</c> gives, whichever way it came in: a code to the phone proves the
/// phone, not the authenticator. Accounts without one, which is every customer, are signed in at
/// once, and the answer carries the token's capabilities.
/// </para>
/// <para>
/// Every failure is the same <c>401</c>, whether the account is missing, suspended, has no password,
/// or the code or password is wrong. Telling them apart would tell a stranger which numbers are
/// customers.
/// </para>
/// </summary>
public sealed class LoginHandler(
    IIdentityRepository identity,
    OtpVerifier otp,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock,
    ILogger<LoginHandler> logger)
{
    public async Task<StaffLoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var account = command.Code is not null
            ? await WithCodeAsync(command, cancellationToken)
            : await WithPasswordAsync(command, cancellationToken);

        if (account is null || !account.CanAuthenticate)
        {
            logger.LogWarning("Failed sign-in through /auth/login");
            throw new InvalidCredentialsException();
        }

        var tokens = await sessionIssuer.StartAsync(account, twoFactorSatisfied: false, command.UserAgent, command.IpAddress, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account.TwoFactorEnabled
            ? StaffLoginResult.ChallengeRequired(tokens.RefreshToken)
            : StaffLoginResult.Completed(tokens);
    }

    private async Task<UserAccount?> WithCodeAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        if (UserAccount.NormalisePhone(command.Phone) is not { } phone)
        {
            return null;
        }

        var proved = await otp.TryVerifyAsync(phone, OtpPurpose.Login, command.Code!, cancellationToken);

        // The attempt is saved whether or not it was right, so the code's guess limit holds.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return proved ? await identity.FindByPhoneAsync(phone, cancellationToken) : null;
    }

    private async Task<UserAccount?> WithPasswordAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var identifier = command.Identifier!.Trim();

        var account = identifier.Contains('@', StringComparison.Ordinal)
            ? await identity.FindByEmailAsync(UserAccount.NormaliseEmail(identifier), cancellationToken)
            : UserAccount.NormalisePhone(identifier) is { } phone
                ? await identity.FindByPhoneAsync(phone, cancellationToken)
                : null;

        if (account?.PasswordHash is not { } hash)
        {
            // Same cost as a real check, so "no such account" is not measurably faster.
            passwordHasher.Hash(command.Password!);
            return null;
        }

        var verification = passwordHasher.Verify(hash, command.Password!);

        if (verification == PasswordVerification.Failed)
        {
            return null;
        }

        if (verification == PasswordVerification.SuccessRehashNeeded)
        {
            account.SetPassword(passwordHasher.Hash(command.Password!), clock.UtcNow);
        }

        return account;
    }
}
