using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.Sessions;
using HappyPaws.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Identity.StaffLogin;

/// <summary>
/// <c>POST /auth/staff/login</c> — email and password, then a second factor.
/// </summary>
public sealed class StaffLoginHandler(
    IIdentityRepository repository,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock,
    ILogger<StaffLoginHandler> logger)
{
    public async Task<StaffLoginResult> HandleAsync(StaffLoginCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var account = await repository.FindByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);

        // Verify against a dummy hash when the account is absent, so a missing account and a wrong
        // password take the same time. Returning early here would turn login into an account
        // enumeration oracle measurable over the network.
        var verification = account?.PasswordHash is { } hash
            ? passwordHasher.Verify(hash, command.Password)
            : DecoyVerify(command.Password);

        if (account is null || verification == PasswordVerification.Failed)
        {
            logger.LogWarning("Failed staff login for {Email}", command.Email);
            throw InvalidCredentials();
        }

        // Same generic failure for a suspended account: whether an address belongs to a suspended
        // employee is not something an unauthenticated caller should be able to discover.
        if (!account.CanAuthenticate)
        {
            logger.LogWarning("Login refused for {UserId} in status {Status}", account.Id, account.Status);
            throw InvalidCredentials();
        }

        var membership = await repository.FindStaffMembershipAsync(account.Id, cancellationToken);
        if (membership is not { IsActive: true })
        {
            logger.LogWarning("Login refused for {UserId}: no active staff membership", account.Id);
            throw InvalidCredentials();
        }

        if (verification == PasswordVerification.SuccessRehashNeeded)
        {
            account.SetPassword(passwordHasher.Hash(command.Password), clock.UtcNow);
        }

        var tokens = await sessionIssuer.StartAsync(
            account,
            twoFactorSatisfied: false,
            command.UserAgent,
            command.IpAddress,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Enrolled accounts get a challenge and no bearer token — the contract is explicit that
        // /auth/2fa/verify is what returns the real access token. Accounts that have not enrolled
        // sign in without one, and the 2FA authorization gate then refuses them any medical scope.
        return account.TwoFactorEnabled
            ? StaffLoginResult.ChallengeRequired(tokens.RefreshToken)
            : StaffLoginResult.Completed(tokens);
    }

    /// <summary>Burns comparable CPU on a fixed hash so the "no such account" path is not faster.</summary>
    private PasswordVerification DecoyVerify(string password)
    {
        const string Decoy =
            "AQAAAAIAAYagAAAAEEqQZ0kX3Yq5b0m3aQm3LQ1nJXbY3xQmVQ0m3aQm3LQ1nJXbY3xQmVQ0m3aQm3LQ1nJQ==";

        try
        {
            return passwordHasher.Verify(Decoy, password);
        }
        catch (FormatException)
        {
            return PasswordVerification.Failed;
        }
    }

    private static AppException InvalidCredentials() => new InvalidCredentialsException();
}

/// <summary>
/// Deliberately says nothing about which half was wrong, or whether the account exists at all.
/// </summary>
public sealed class InvalidCredentialsException()
    : AppException(ErrorCodes.Unauthenticated, "อีเมลหรือรหัสผ่านไม่ถูกต้อง", HttpStatus.Unauthorized)
{
}
