using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Application.Identity.UpdateProfile;

/// <summary><c>PATCH /me</c> — "ชื่อ อีเมล เบอร์ ภาษา การแจ้งเตือน". An absent field is left alone.</summary>
/// <param name="DisplayName">The name shown everywhere, including on records.</param>
/// <param name="Email">A new sign-in email.</param>
/// <param name="Phone">A new phone number; an empty string removes it.</param>
/// <param name="Language"><c>th</c> or <c>en</c>.</param>
/// <param name="NotifyByEmail">Accept messages by email.</param>
/// <param name="NotifyBySms">Accept messages by SMS.</param>
/// <param name="NotifyByLine">Accept messages through LINE.</param>
/// <param name="CurrentPassword">Required when the email or phone changes.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record UpdateProfileCommand(
    string? DisplayName = null,
    string? Email = null,
    string? Phone = null,
    string? Language = null,
    bool? NotifyByEmail = null,
    bool? NotifyBySms = null,
    bool? NotifyByLine = null,
    string? CurrentPassword = null,
    uint? IfMatchVersion = null);

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(command => command.DisplayName).MaximumLength(UserAccount.DisplayNameMaxLength);
        RuleFor(command => command.Email).MaximumLength(UserAccount.EmailMaxLength);
        RuleFor(command => command.Phone).MaximumLength(32);
        RuleFor(command => command.CurrentPassword).MaximumLength(256);
    }
}

/// <summary>
/// Changes the signed-in account's own details.
/// <para>
/// <b>Email and phone need the current password.</b> Email is how staff sign in and phone is how
/// customers will. Someone holding a stolen access token for fifteen minutes must not be able to
/// point the account at an address they control and reset their way in from there. Name, language
/// and notification choices take no password: nothing about signing in hangs on them.
/// </para>
/// <para>
/// An email or phone already on another account answers <c>409 contact_taken</c>. Both are
/// unique because each identifies a person at the login prompt.
/// </para>
/// <para>
/// The notification choices are stored and shown in <c>GET /me</c>; the message dispatcher does not
/// consult them yet (see <c>docs/features/notifications.md</c>).
/// </para>
/// </summary>
public sealed class UpdateProfileHandler(
    IIdentityRepository identity,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var userId = currentUser.UserId ?? throw new ForbiddenException("no subject on token");

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var account = await identity.FindByIdAsync(userId, token)
                ?? throw new NotFoundException("account", userId);

            ConcurrencyGuard.Ensure(account, command.IfMatchVersion);

            var changed = new List<string>();
            var details = new Dictionary<string, object?>();

            var newEmail = command.Email is { } email ? UserAccount.NormaliseEmail(email) : null;
            var emailChanges = newEmail is not null && newEmail != account.Email;
            var newPhone = command.Phone is null ? null : UserAccount.NormalisePhone(command.Phone);
            var phoneChanges = command.Phone is not null && newPhone != account.PhoneNumber;

            if (emailChanges || phoneChanges)
            {
                EnsureCurrentPassword(account, command.CurrentPassword);
            }

            if (emailChanges)
            {
                if (await identity.FindByEmailAsync(newEmail!, token) is { } other && other.Id != account.Id)
                {
                    throw Taken("email", "อีเมลนี้มีบัญชีอื่นใช้อยู่แล้ว");
                }

                details["email_before"] = account.Email;
                account.ChangeEmail(newEmail!);
                details["email_after"] = account.Email;
                changed.Add("email");
            }

            if (phoneChanges)
            {
                if (newPhone is not null
                    && await identity.FindByPhoneAsync(newPhone, token) is { } other
                    && other.Id != account.Id)
                {
                    throw Taken("phone", "เบอร์นี้มีบัญชีอื่นใช้อยู่แล้ว");
                }

                account.ChangePhone(newPhone);
                changed.Add("phone");
            }

            account.UpdateProfile(command.DisplayName, command.Language);

            if (command.DisplayName is not null)
            {
                changed.Add("display_name");
            }

            if (command.Language is not null)
            {
                changed.Add("language");
            }

            if (command.NotifyByEmail is not null || command.NotifyBySms is not null || command.NotifyByLine is not null)
            {
                account.SetNotificationChannels(command.NotifyByEmail, command.NotifyBySms, command.NotifyByLine);
                changed.Add("notifications");
            }

            if (changed.Count > 0)
            {
                details["fields"] = changed.ToArray();

                audit.Record(new AuditEvent(
                    AuditActions.AccountProfileUpdated, AuditResources.UserAccount, account.Id, Details: details));
            }
        }, cancellationToken);
    }

    private void EnsureCurrentPassword(UserAccount account, string? currentPassword) =>
        CurrentPassword.Ensure(passwordHasher, account, currentPassword);

    private static ConflictException Taken(string field, string message) =>
        new(ErrorCodes.ContactTaken, message) { Field = field, MessageEn = "Already used by another account." };
}
