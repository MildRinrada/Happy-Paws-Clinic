using FluentValidation;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Identity.RequestOtp;

/// <summary><c>POST /auth/otp/request</c> — "rate limit 3 ครั้ง/เบอร์/10 นาที". <c>Purpose</c> is <c>register</c> or <c>login</c>.</summary>
public sealed record RequestOtpCommand(string Phone, string Purpose);

public sealed class RequestOtpValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpValidator()
    {
        RuleFor(command => command.Phone)
            .Must(phone => UserAccount.NormalisePhone(phone) is { Length: >= 9 and <= 15 })
            .WithMessage("เบอร์โทรต้องมี 9 ถึง 15 หลัก");

        RuleFor(command => command.Purpose)
            .Must(purpose => WireEnum.TryParse<OtpPurpose>(purpose, out _))
            .WithMessage($"purpose ต้องเป็น {WireEnum.Names<OtpPurpose>()}");
    }
}

/// <summary>When the code that was (or would have been) sent stops working.</summary>
public sealed record OtpRequestedDto(DateTimeOffset ExpiresAt);

/// <summary>
/// Sends a six-digit code to a phone.
/// <para>
/// <b>The answer is the same whether or not the number has an account.</b> A code for
/// <c>login</c> is only actually sent to a number that belongs to someone, so the clinic does not
/// pay to text strangers; but the caller cannot tell, because "no such customer" would turn this
/// endpoint into a directory of the clinic's clients. A code for <c>register</c> is always sent:
/// proving the number is the first step of signing up, and an existing account is reported only
/// after that proof, at <c>POST /auth/register</c>.
/// </para>
/// <para>
/// The contract's limit, three codes per number per ten minutes, is counted in the database per
/// number, which is what it says. Over it is <c>429 rate_limited</c>, whether or not a code would
/// have been sent, for the same reason as above.
/// </para>
/// </summary>
public sealed class RequestOtpHandler(
    ICustomerSignInRepository signIn,
    IIdentityRepository identity,
    IOtpSender sender,
    OtpSettings settings,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock,
    ILogger<RequestOtpHandler> logger)
{
    public async Task<OtpRequestedDto> HandleAsync(RequestOtpCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var phone = UserAccount.NormalisePhone(command.Phone)!;
        WireEnum.TryParse<OtpPurpose>(command.Purpose, out var purpose);
        var now = clock.UtcNow;

        if (await signIn.CodesSentSinceAsync(phone, now - OtpSettings.Window, cancellationToken) >= settings.PerPhonePer10Minutes)
        {
            throw new RateLimitedException("ขอรหัสบ่อยเกินไป กรุณารอสักครู่แล้วลองใหม่");
        }

        var code = OneTimeSecrets.NewCode();
        var verification = PhoneVerification.Issue(phone, purpose, OneTimeSecrets.HashCode(phone, code), now);

        // Recorded either way, so the rate limit counts requests, not deliveries.
        signIn.AddVerification(verification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var deliver = purpose == OtpPurpose.Register
            || await identity.FindByPhoneAsync(phone, cancellationToken) is { CanAuthenticate: true };

        if (deliver)
        {
            await sender.SendAsync(phone, code, purpose, verification.ExpiresAt, cancellationToken);
        }
        else
        {
            logger.LogInformation("Login code for an unknown number was not sent");
        }

        return new OtpRequestedDto(verification.ExpiresAt);
    }
}
