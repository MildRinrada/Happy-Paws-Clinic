using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.RequestOtp;
using HappyPaws.Application.Identity.Sessions;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Identity.Register;

/// <summary>
/// <c>POST /auth/register</c> — "สมัครด้วยเบอร์โทร + OTP". The password is optional: without one
/// the customer signs in with a code each time.
/// </summary>
public sealed record RegisterCustomerCommand(
    string Phone,
    string Code,
    string DisplayName,
    string? Password = null,
    string? Email = null,
    string? UserAgent = null,
    string? IpAddress = null);

public sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerValidator()
    {
        RuleFor(command => command.Phone)
            .Must(phone => UserAccount.NormalisePhone(phone) is { Length: >= 9 and <= 15 })
            .WithMessage("เบอร์โทรต้องมี 9 ถึง 15 หลัก");

        RuleFor(command => command.Code).NotEmpty().Length(6);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(UserAccount.DisplayNameMaxLength);
        RuleFor(command => command.Password).MinimumLength(8).MaximumLength(256).When(command => command.Password is not null);
        RuleFor(command => command.Email).EmailAddress().MaximumLength(UserAccount.EmailMaxLength).When(command => command.Email is not null);
    }
}

/// <summary>
/// Opens a portal account for a phone number that has just proved itself with a code.
/// <para>
/// <b>The customer record the counter made is reused, not duplicated.</b> A customer registered at
/// the desk last year and signing up today should see last year's pets and bills, so the account is
/// linked to the existing record with the same number. Only when there is none is a new record
/// made. An invitation from <c>POST /staff/customers/{id}/invite</c> is marked accepted on the way.
/// </para>
/// <para>
/// A number that already has an account answers <c>409 contact_taken</c>. That is only said after
/// the caller has proved they hold the number, so it reveals nothing to anyone else.
/// </para>
/// </summary>
public sealed class RegisterCustomerHandler(
    IIdentityRepository identity,
    ICustomerRepository customers,
    ICustomerInvitationRepository invitations,
    OtpVerifier otp,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer,
    IAuditTrail audit,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<AuthTokens> HandleAsync(RegisterCustomerCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var phone = UserAccount.NormalisePhone(command.Phone)!;
        AuthTokens? tokens = null;

        // The code is spent on its own commit, before anything else can fail. A wrong code is an
        // attempt that must count even though this request is then refused.
        var proved = await otp.TryVerifyAsync(phone, OtpPurpose.Register, command.Code, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!proved)
        {
            throw OtpVerifier.WrongCode();
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await identity.FindByPhoneAsync(phone, token) is not null)
            {
                throw new ConflictException(Common.Errors.ErrorCodes.ContactTaken, "เบอร์นี้มีบัญชีอยู่แล้ว กรุณาเข้าสู่ระบบ");
            }

            if (command.Email is { } email && await identity.FindByEmailAsync(UserAccount.NormaliseEmail(email), token) is not null)
            {
                throw new ConflictException(Common.Errors.ErrorCodes.ContactTaken, "อีเมลนี้มีบัญชีอยู่แล้ว");
            }

            var account = UserAccount.CreateCustomer(
                command.DisplayName,
                phone,
                command.Password is null ? null : passwordHasher.Hash(command.Password),
                command.Email);

            identity.AddAccount(account);

            var (customer, linkedExisting) = await LinkCustomerAsync(account, phone, token);

            audit.Record(new AuditEvent(
                AuditActions.AccountRegistered,
                AuditResources.UserAccount,
                account.Id,
                Details: new Dictionary<string, object?>
                {
                    ["customer_id"] = customer.Id,
                    ["linked_existing_record"] = linkedExisting,
                }));

            // Saved before the session is issued: the issuer reads the customer link back to put
            // the customer id on the token.
            await unitOfWork.SaveChangesAsync(token);

            tokens = await sessionIssuer.StartAsync(account, twoFactorSatisfied: false, command.UserAgent, command.IpAddress, token);
        }, cancellationToken);

        return tokens!;
    }

    private async Task<(Customer Customer, bool LinkedExisting)> LinkCustomerAsync(
        UserAccount account,
        string phone,
        CancellationToken token)
    {
        var existing = (await customers.UnlinkedWithPhoneDigitsAsync(phone, token)).FirstOrDefault();

        if (existing is not null)
        {
            existing.LinkAccount(account.Id);

            if (await invitations.LatestForCustomerAsync(existing.Id, token) is { } invitation)
            {
                invitation.Accept(clock.UtcNow);
            }

            return (existing, true);
        }

        var created = Customer.Create(account.DisplayName, phone, account.Email, account.Id);
        customers.Add(created);
        webhooks.Enqueue(WebhookEventTypes.CustomerCreated, WebhookPayloads.Customer(created, clock.UtcNow));

        return (created, false);
    }
}
