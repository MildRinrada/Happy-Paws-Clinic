using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Identity.ResetCredentials;

/// <param name="Email">The account to reset.</param>
/// <param name="Password">The new password, or null to have one generated.</param>
public sealed record ResetCredentialsCommand(string Email, string? Password = null);

/// <summary>
/// Re-issues an account's password and second factor. An operator command, not an endpoint.
/// <para>
/// This exists because a lost phone must not be the end of a clinic's access to its own records.
/// The contract's self-service paths — <c>POST /auth/password/reset</c> and
/// <c>POST /me/2fa/enroll</c> — are not built, and even when they are, neither helps an owner who
/// can no longer produce a code: there is no platform admin to appeal to ("ไม่มี platform admin").
/// Whoever can run a command on the server already has the database, so giving them a supported
/// way to do this adds no access and removes a reason to edit rows by hand.
/// </para>
/// <para>
/// Every session is revoked and every token issued before now is refused, so whoever held the old
/// credentials is signed out by the same act that replaces them.
/// </para>
/// </summary>
public sealed class ResetCredentialsHandler(
    IIdentityRepository identity,
    OperatorCredentials credentials,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<IssuedCredentials> HandleAsync(
        ResetCredentialsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Password is { Length: > 0 and < OperatorCredentials.MinimumPasswordLength })
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["password"] = [$"รหัสผ่านต้องยาวอย่างน้อย {OperatorCredentials.MinimumPasswordLength} ตัวอักษร"],
            });
        }

        var email = command.Email.Trim().ToLowerInvariant();
        var now = clock.UtcNow;

        IssuedCredentials? issued = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var account = await identity.FindByEmailAsync(email, token)
                ?? throw new NotFoundException("account", email);

            var (hash, generated) = credentials.NewPassword(command.Password);

            account.SetPassword(hash, now);
            issued = credentials.EnrolSecondFactor(account, generated);

            await identity.RevokeOtherSessionsAsync(account.Id, string.Empty, now, token);

            audit.Record(new AuditEvent(
                AuditActions.CredentialsReset,
                AuditResources.UserAccount,
                account.Id,
                Details: new Dictionary<string, object?> { ["via"] = "operator_command" }));
        }, cancellationToken);

        return issued!;
    }
}
