using FluentValidation;
using HappyPaws.Application.Access;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Staff;
using HappyPaws.Application.Staff.DeactivateStaffMember;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Application.Identity.SetUserStatus;

/// <summary><c>PATCH /staff/users/{id}/status</c> — "active · suspended · deactivated".</summary>
/// <param name="UserId">The account (<c>usr_…</c>), staff or customer.</param>
/// <param name="Status"><c>active</c>, <c>suspended</c> or <c>deactivated</c>.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record SetUserStatusCommand(string UserId, string Status, uint? IfMatchVersion = null);

public sealed class SetUserStatusValidator : AbstractValidator<SetUserStatusCommand>
{
    public SetUserStatusValidator() =>
        RuleFor(command => command.Status)
            .Must(status => SetUserStatusHandler.Parse(status) is not null)
            .WithMessage("สถานะต้องเป็น active, suspended หรือ deactivated");
}

/// <summary>An account's standing after the change.</summary>
public sealed record UserStatusDto(string UserId, uint Version, string DisplayName, string Status);

/// <summary>
/// Changes whether an account may sign in at all.
/// <para>
/// <b>Not the same as deactivating a staff member.</b> <c>POST /staff/members/{id}/deactivate</c>
/// ends someone's job here; this acts on the login itself, staff or customer, and
/// <c>suspended</c> can be lifted. <c>deactivated</c> is final through this endpoint: an account
/// closed for good is not reopened by a status flip.
/// </para>
/// <para>
/// Anything other than <c>active</c> revokes every session at once and sign-in, 2FA and refresh all
/// refuse the account. An access token already issued lives out its fifteen minutes, the same edge
/// as every other revocation here.
/// </para>
/// <para>
/// The guards of the staff module apply when the account belongs to staff: a branch-bound manager
/// acts on their own branches, nobody acts on someone holding more than they do, and the last
/// account holding <c>*</c> cannot be suspended. Nobody changes their own status.
/// </para>
/// </summary>
public sealed class SetUserStatusHandler(
    IIdentityRepository identity,
    StaffMemberAccess staffAccess,
    OwnerLockoutGuard lockout,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock,
    RequestValidator validator)
{
    public async Task<UserStatusDto> HandleAsync(SetUserStatusCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var target = Parse(command.Status)!.Value;

        if (command.UserId == currentUser.UserId)
        {
            throw new CannotDeactivateSelfException();
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var account = await identity.FindByIdAsync(command.UserId, token)
                ?? throw new NotFoundException("user", command.UserId);

            if (await identity.FindStaffMembershipAsync(account.Id, token) is { } membership)
            {
                // Branch and rank: answers 404 or 403 exactly as changing the membership would.
                await staffAccess.ForChangeAsync(membership.Id, token);
            }

            ConcurrencyGuard.Ensure(account, command.IfMatchVersion);

            var before = account.Status;

            if (before == target)
            {
                return;
            }

            var ownerCheck = await lockout.BeforeChangeAsync(token);
            var now = clock.UtcNow;

            account.SetStatus(target, now);

            if (target != UserStatus.Active)
            {
                await identity.RevokeOtherSessionsAsync(account.Id, string.Empty, now, token);
            }

            audit.Record(new AuditEvent(
                AuditActions.UserStatusChanged,
                AuditResources.UserAccount,
                account.Id,
                Details: new Dictionary<string, object?>
                {
                    ["before"] = Wire(before),
                    ["after"] = Wire(target),
                }));

            await ownerCheck.EnsureAnOwnerRemainsAsync(token);
        }, cancellationToken);

        var saved = await identity.FindByIdAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException("user", command.UserId);

        return new UserStatusDto(saved.Id, saved.Version, saved.DisplayName, Wire(saved.Status));
    }

    public static UserStatus? Parse(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "active" => UserStatus.Active,
        "suspended" => UserStatus.Suspended,
        "deactivated" => UserStatus.Deactivated,
        _ => null,
    };

    private static string Wire(UserStatus status) => status.ToString().ToLowerInvariant();
}
