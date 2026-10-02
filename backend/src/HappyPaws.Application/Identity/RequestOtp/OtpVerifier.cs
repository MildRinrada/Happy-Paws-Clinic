using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Application.Identity.RequestOtp;

/// <summary>
/// Spends a one-time code, or refuses. Shared by sign-up and sign-in, which differ only in the
/// purpose a code must have been sent for.
/// </summary>
public sealed class OtpVerifier(ICustomerSignInRepository signIn, IClock clock)
{
    /// <summary>
    /// True when the code is right. A wrong guess is saved as an attempt before the caller fails, so
    /// the attempt limit holds even though the request that made it is refused.
    /// </summary>
    public async Task<bool> TryVerifyAsync(
        string phoneDigits,
        OtpPurpose purpose,
        string code,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var verification = await signIn.LatestUsableAsync(phoneDigits, purpose, now, cancellationToken);

        return verification is not null
            && verification.TryConsume(OneTimeSecrets.HashCode(phoneDigits, code), now);
    }

    /// <summary>The refusal for a code that is wrong, spent or expired. One message for all three.</summary>
    public static ValidationFailedException WrongCode() =>
        new(new Dictionary<string, string[]> { ["code"] = ["รหัสไม่ถูกต้องหรือหมดอายุ กรุณาขอรหัสใหม่"] });
}
