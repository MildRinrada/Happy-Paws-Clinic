using FluentValidation;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers.RecordConsent;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Identity.Dtos;
using HappyPaws.Application.Identity.Register;
using HappyPaws.Application.Identity.RequestOtp;
using HappyPaws.Application.Identity.Sessions;
using HappyPaws.Application.Patients;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Application.Appointments.GuestBooking;

/// <summary>
/// <c>POST /booking/guest/verify</c> — the code from the SMS, a name, and the privacy notice.
/// Not in the API reference: <c>docs/contract-extensions.md § 27</c>.
/// <para>
/// <c>DisplayName</c> and the notice are asked of everyone, because before the code is checked
/// nobody may be told whether the number already has an account. For a number that has one they
/// are not used.
/// </para>
/// </summary>
public sealed record VerifyGuestBookingCommand(
    string HoldId,
    string Phone,
    string Code,
    string DisplayName,
    bool AcceptPrivacyNotice,
    string? PolicyVersion = null,
    bool MarketingConsent = false,
    string? UserAgent = null,
    string? IpAddress = null);

public sealed class VerifyGuestBookingValidator : AbstractValidator<VerifyGuestBookingCommand>
{
    public VerifyGuestBookingValidator()
    {
        RuleFor(command => command.HoldId).NotEmpty().MaximumLength(64);

        RuleFor(command => command.Phone)
            .Must(phone => UserAccount.NormalisePhone(phone) is { Length: >= 9 and <= 15 })
            .WithMessage("เบอร์โทรต้องมี 9 ถึง 15 หลัก");

        RuleFor(command => command.Code).NotEmpty().Length(6);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(UserAccount.DisplayNameMaxLength);
        RuleFor(command => command.AcceptPrivacyNotice).Equal(true).WithMessage("กรุณายอมรับประกาศความเป็นส่วนตัวก่อนจอง");
        RuleFor(command => command.PolicyVersion).MaximumLength(32);
    }
}

/// <summary>A living pet the visitor already has at the clinic, to choose from once they are signed in.</summary>
public sealed record GuestPetDto(string Id, string Name, string Species);

/// <summary>
/// The visitor is now a signed-in customer: the session, whether this request opened their account,
/// their living pets, and the hold, now theirs to redeem at <c>POST /portal/appointments</c>.
/// </summary>
public sealed record GuestBookingVerifiedResult(
    AuthTokens Tokens,
    bool AccountCreated,
    IReadOnlyList<GuestPetDto> Pets,
    SlotHoldDto Hold);

/// <summary>
/// The second half of booking without signing up first: prove the number, become a customer, and
/// sign in. The booking itself is then the ordinary <c>POST /portal/appointments</c>, with its
/// <c>Idempotency-Key</c>, the hold and either a pet the customer has or <c>new_pet</c>.
/// <para>
/// <b>The phone number is the identity.</b> A number with a customer login signs in to it, so a
/// returning owner books under their own record and sees their pets. A number the counter knows but
/// that has no login gets a login linked to that record, exactly as sign-up does
/// (<see cref="CustomerRecordLinker"/>). A new number gets a login and a new record.
/// </para>
/// <para>
/// <b>Nothing about the number is said before the code is right.</b> The hold is checked first
/// (it is the visitor's own, from <c>/start</c>, and says nothing about any account), then the code
/// is spent on its own commit, as at sign-up, so a wrong guess counts even though the request is
/// refused. Only after that is the number looked up.
/// </para>
/// <para>
/// <b>Staff numbers are refused</b> with <c>409 contact_taken</c>. Signing a member of staff in by an
/// SMS code here would hand out a staff session without their password or second factor, and a
/// booking made under a staff login has no customer to belong to. A login that is suspended or has a
/// second factor is refused the same way: it signs in at <c>/login</c>, not here.
/// </para>
/// <para>
/// <b>Consent is recorded when the login is opened here</b>, as rows in the consent ledger with
/// source <c>portal</c> and the notice version shown: <c>service_messages</c> granted (the visitor
/// accepted the notice that says the clinic will message them about this visit) and
/// <c>marketing</c> granted only when they ticked it. Nothing is withdrawn by leaving the box empty,
/// so a counter record's earlier paper consent is never overwritten. A returning customer's consents
/// are left as they are.
/// </para>
/// </summary>
public sealed class VerifyGuestBookingHandler(
    ISlotHoldRepository holds,
    IIdentityRepository identity,
    CustomerRecordLinker linker,
    OtpVerifier otp,
    SessionIssuer sessionIssuer,
    RecordConsentHandler consents,
    IPetRepository pets,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<GuestBookingVerifiedResult> HandleAsync(
        VerifyGuestBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var phone = UserAccount.NormalisePhone(command.Phone)!;

        // Checked before the code is spent, so a lapsed hold costs the visitor a new time, not a new
        // code as well.
        EnsureClaimable(
            await holds.FindByIdAsync(command.HoldId, cancellationToken) ?? throw new NotFoundException("hold", command.HoldId),
            phone);

        var proved = await otp.TryVerifyAsync(phone, OtpPurpose.Booking, command.Code, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!proved)
        {
            throw OtpVerifier.WrongCode();
        }

        GuestBookingVerifiedResult? result = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var (account, customerId, signedUp) = await CustomerForAsync(phone, command.DisplayName, token);

            var hold = await holds.FindByIdAsync(command.HoldId, token)
                ?? throw new NotFoundException("hold", command.HoldId);

            hold.ClaimForCustomer(phone, customerId, clock.UtcNow);

            // Saved before the session is issued: the issuer reads the customer link back to put
            // the customer id on the token.
            await unitOfWork.SaveChangesAsync(token);

            if (signedUp)
            {
                await RecordConsentAsync(customerId, command, token);
            }

            var tokens = await sessionIssuer.StartAsync(account, twoFactorSatisfied: false, command.UserAgent, command.IpAddress, token);

            var living = (await pets.ForCustomerAsync(customerId, token))
                .Where(pet => !pet.IsDeceased)
                .Select(pet => new GuestPetDto(pet.Id, pet.Name, pet.Species))
                .ToList();

            result = new GuestBookingVerifiedResult(tokens, signedUp, living, SlotHoldDto.From(hold));
        }, cancellationToken);

        return result!;
    }

    private void EnsureClaimable(SlotHold hold, string phone)
    {
        if (hold.HeldByCustomerId != SlotHold.GuestHolderFor(phone))
        {
            throw new DomainException("hold_not_yours", "การจองค้างนี้เป็นของผู้ใช้อื่น");
        }

        if (!hold.IsHoldingAt(clock.UtcNow))
        {
            throw new DomainException("hold_expired", "การจองค้างหมดอายุแล้ว กรุณาเลือกเวลาใหม่");
        }
    }

    /// <summary>
    /// The login and customer record a proved number belongs to, opening them when there are none.
    /// <c>SignedUp</c> is true when this request gave the login its customer record.
    /// </summary>
    private async Task<(UserAccount Account, string CustomerId, bool SignedUp)> CustomerForAsync(
        string phone,
        string displayName,
        CancellationToken cancellationToken)
    {
        var account = await identity.FindByPhoneAsync(phone, cancellationToken);

        if (account is not null)
        {
            if (await identity.FindStaffMembershipAsync(account.Id, cancellationToken) is not null)
            {
                throw new ConflictException(
                    ErrorCodes.ContactTaken,
                    "เบอร์นี้ใช้กับบัญชีพนักงานของคลินิก จึงจองผ่านหน้านี้ไม่ได้ กรุณาโทรหาคลินิก หรือใช้เบอร์อื่น");
            }

            if (!account.CanAuthenticate || account.TwoFactorEnabled)
            {
                throw new ConflictException(
                    ErrorCodes.ContactTaken,
                    "บัญชีของเบอร์นี้ต้องเข้าสู่ระบบก่อนจอง กรุณาเข้าสู่ระบบ หรือติดต่อคลินิก");
            }

            if (await identity.FindCustomerIdAsync(account.Id, cancellationToken) is { } customerId)
            {
                return (account, customerId, false);
            }
        }
        else
        {
            account = UserAccount.CreateCustomer(displayName, phone, passwordHash: null);
            identity.AddAccount(account);
        }

        var (customer, linkedExisting) = await linker.LinkAsync(account, phone, cancellationToken);

        audit.Record(new AuditEvent(
            AuditActions.AccountRegistered,
            AuditResources.UserAccount,
            account.Id,
            Details: new Dictionary<string, object?>
            {
                ["customer_id"] = customer.Id,
                ["linked_existing_record"] = linkedExisting,
                ["via"] = "guest_booking",
            }));

        return (account, customer.Id, true);
    }

    private Task RecordConsentAsync(string customerId, VerifyGuestBookingCommand command, CancellationToken cancellationToken)
    {
        var decisions = new Dictionary<string, bool> { ["service_messages"] = true };

        if (command.MarketingConsent)
        {
            decisions["marketing"] = true;
        }

        return consents.HandleAsync(
            new RecordConsentCommand(customerId, decisions, ConsentSource.Portal, command.PolicyVersion?.Trim() is { Length: > 0 } version ? version : null),
            cancellationToken);
    }
}
