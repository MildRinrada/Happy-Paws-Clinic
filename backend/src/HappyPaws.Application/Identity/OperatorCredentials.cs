using System.Security.Cryptography;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Application.Identity;

/// <summary>
/// A password and a second factor, freshly issued to an account by an operator command.
/// <para>
/// Returned once, to be printed on the terminal that ran the command, and held nowhere else in
/// readable form: the password is stored as a hash and nothing in the API ever returns the TOTP
/// secret again.
/// </para>
/// </summary>
/// <param name="UserId">The account.</param>
/// <param name="Email">What it signs in with.</param>
/// <param name="GeneratedPassword">Set only when the operator did not supply one.</param>
/// <param name="TotpSecret">Base32, for typing into an authenticator app by hand.</param>
/// <param name="OtpAuthUri">The same secret as a QR-code payload.</param>
public sealed record IssuedCredentials(
    string UserId,
    string Email,
    string? GeneratedPassword,
    string TotpSecret,
    string OtpAuthUri);

/// <summary>
/// Issues an account's credentials from the server's own command line — never from a request.
/// <para>
/// Shared by the two operator commands that need it: bootstrapping the first owner, and resetting
/// an account that has lost its second factor. Both set a password and enrol a new TOTP secret in
/// one step, because a staff account holding clinical scopes is refused by the two-factor gate
/// until it has one.
/// </para>
/// </summary>
public sealed class OperatorCredentials(
    IPasswordHasher passwordHasher,
    ITwoFactorService twoFactor)
{
    /// <summary>The bound <c>POST /me/password</c> applies.</summary>
    public const int MinimumPasswordLength = 8;

    /// <summary>Hashes the supplied password, or a generated one, for a new account.</summary>
    public (string Hash, string? Generated) NewPassword(string? supplied)
    {
        var generated = string.IsNullOrEmpty(supplied) ? GeneratePassword() : null;

        return (passwordHasher.Hash(generated ?? supplied!), generated);
    }

    /// <summary>Enrols a new TOTP secret on the account and reports what the operator must pass on.</summary>
    public IssuedCredentials EnrolSecondFactor(UserAccount account, string? generatedPassword)
    {
        var enrolment = twoFactor.Enrol(account.Email!);

        account.EnrolTwoFactor(TwoFactorMethod.Totp, enrolment.Secret);

        return new IssuedCredentials(
            account.Id, account.Email!, generatedPassword, enrolment.Secret, enrolment.OtpAuthUri);
    }

    /// <summary>144 bits from the system generator, in an alphabet that survives being read aloud or pasted.</summary>
    private static string GeneratePassword() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(18)).Replace('+', '-').Replace('/', '_');
}
