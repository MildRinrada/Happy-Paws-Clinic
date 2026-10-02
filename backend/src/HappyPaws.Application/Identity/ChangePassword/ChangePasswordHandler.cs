using FluentValidation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Identity.StaffLogin;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Identity.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword);

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty();

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(256)
            .NotEqual(command => command.CurrentPassword)
            .WithMessage("รหัสผ่านใหม่ต้องไม่ซ้ำกับรหัสผ่านเดิม");
    }
}

/// <summary>
/// <c>POST /me/password</c> — "เปลี่ยนรหัส · เพิกถอน session อื่นทั้งหมด".
/// </summary>
public sealed class ChangePasswordHandler(
    IIdentityRepository repository,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock,
    ILogger<ChangePasswordHandler> logger)
{
    public async Task HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var userId = currentUser.UserId ?? throw new ForbiddenException("no subject on token");

        var now = clock.UtcNow;

        // Revoking the other sessions is the point of this endpoint, not a nicety: the usual
        // reason to change a password is that someone else may have it. The account is loaded
        // inside the transaction because a retry resets the change tracker — see IUnitOfWork.
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var account = await repository.FindByIdAsync(userId, token)
                ?? throw new NotFoundException("account", userId);

            if (account.PasswordHash is null
                || passwordHasher.Verify(account.PasswordHash, command.CurrentPassword) == PasswordVerification.Failed)
            {
                logger.LogWarning("Password change refused for {UserId}: current password incorrect", userId);
                throw new InvalidCredentialsException();
            }

            account.SetPassword(passwordHasher.Hash(command.NewPassword), now);

            await repository.RevokeOtherSessionsAsync(
                userId, currentUser.SessionId ?? string.Empty, now, token);
        }, cancellationToken);
    }
}
