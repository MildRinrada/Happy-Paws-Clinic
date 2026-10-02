using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Auditing;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.Sessions;
using HappyPaws.Application.Identity.StaffLogin;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Identity.VerifyTwoFactor;

/// <param name="Challenge">The opaque value returned by <c>/auth/staff/login</c>.</param>
/// <param name="Code">TOTP code from the authenticator app, or one of the account's recovery codes.</param>
public sealed record VerifyTwoFactorCommand(string Challenge, string Code);

public sealed class VerifyTwoFactorValidator : AbstractValidator<VerifyTwoFactorCommand>
{
    public VerifyTwoFactorValidator()
    {
        RuleFor(command => command.Challenge).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Code)
            .NotEmpty()
            .Must(code => (code.Length is >= 6 and <= 8 && code.All(char.IsAsciiDigit)) || RecoveryCodes.LooksLikeOne(code))
            .WithMessage("รหัสต้องเป็นตัวเลข 6 หลักจากแอป หรือรหัสกู้คืนรูปแบบ xxxxx-xxxxx");
    }
}

/// <summary>
/// <c>POST /auth/2fa/verify</c> — the step that returns the real access token.
/// </summary>
public sealed class VerifyTwoFactorHandler(
    IIdentityRepository repository,
    ITwoFactorService twoFactor,
    SessionIssuer sessionIssuer,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock,
    ILogger<VerifyTwoFactorHandler> logger)
{
    public async Task<AuthTokens> HandleAsync(VerifyTwoFactorCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var session = await repository.FindSessionByRefreshTokenAsync(
            RefreshTokens.Hash(command.Challenge), cancellationToken);

        var now = clock.UtcNow;

        // A challenge is a session that has not cleared its second factor yet. One that already
        // has must not be re-usable here, or a captured challenge would stay live for 30 days.
        if (session is null || !session.IsActive(now) || session.TwoFactorSatisfied)
        {
            throw new InvalidCredentialsException();
        }

        // Bound the challenge window independently of the session's own expiry.
        if (now - session.CreatedAt > SessionIssuer.TwoFactorChallengeLifetime)
        {
            session.Revoke(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        var account = await repository.FindByIdAsync(session.UserId, cancellationToken);
        if (account is null || !account.CanAuthenticate || account.TwoFactorSecret is null)
        {
            throw new InvalidCredentialsException();
        }

        if (RecoveryCodes.LooksLikeOne(command.Code))
        {
            // Spent on use, so a code read off someone's desk works once at most.
            if (!account.TryUseRecoveryCode(RecoveryCodes.Hash(command.Code)))
            {
                logger.LogWarning("Failed recovery-code sign-in for {UserId}", account.Id);
                throw new InvalidCredentialsException();
            }

            logger.LogWarning("Recovery code used for {UserId}; {Left} left", account.Id, account.RecoveryCodeHashes.Count);

            audit.Record(new AuditEvent(
                AuditActions.RecoveryCodeUsed,
                AuditResources.UserAccount,
                account.Id,
                Details: new Dictionary<string, object?> { ["codes_left"] = account.RecoveryCodeHashes.Count }));
        }
        else if (!twoFactor.VerifyCode(account.TwoFactorSecret, command.Code, now))
        {
            logger.LogWarning("Failed 2FA verification for {UserId}", account.Id);
            throw new InvalidCredentialsException();
        }

        session.MarkTwoFactorSatisfied();

        // Rotating here means the challenge value is spent the moment it is redeemed.
        var tokens = await sessionIssuer.RotateAsync(account, session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return tokens;
    }
}
