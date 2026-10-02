using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Identity;

/// <summary>
/// A person who can sign in. Not a role and not a customer record.
/// <para>
/// The contract is explicit that one account can be both staff and customer in this clinic
/// ("บัญชีเดียวเป็นทั้งลูกค้าและพนักงานได้"), so memberships hang off the account rather than the
/// account being one or the other.
/// </para>
/// </summary>
public sealed partial class UserAccount : Entity
{
    private UserAccount()
    {
    }

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Staff sign in with this. Null for a customer-only account, which uses a phone number.</summary>
    public string? Email { get; private set; }

    /// <summary>Normalised to digits so that lookup and duplicate detection are reliable.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>PBKDF2 hash. Null when the account has no password yet — an invited staff member before they accept.</summary>
    public string? PasswordHash { get; private set; }

    public UserStatus Status { get; private set; } = UserStatus.Active;

    public TwoFactorMethod TwoFactorMethod { get; private set; } = TwoFactorMethod.None;

    /// <summary>Encrypted at rest by the infrastructure layer. Never leaves the server after enrolment.</summary>
    public string? TwoFactorSecret { get; private set; }

    public bool TwoFactorEnabled => TwoFactorMethod != TwoFactorMethod.None && TwoFactorSecret is not null;

    /// <summary>
    /// Everything issued before this instant is refused, whatever its own expiry says.
    /// Moving it forward is how "revoke every other session" is enforced without hunting down
    /// individual tokens.
    /// </summary>
    public DateTimeOffset? CredentialsInvalidatedAt { get; private set; }

    public static UserAccount CreateStaff(string displayName, string email, string passwordHash)
    {
        return new UserAccount
        {
            Id = EntityId.New(EntityId.User),
            DisplayName = displayName,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
        };
    }

    /// <summary>
    /// <c>POST /auth/register</c> — "สมัครด้วยเบอร์โทร + OTP". The phone has been proved by a code
    /// sent to it; the password is optional, because a customer may prefer to sign in with a code
    /// every time.
    /// </summary>
    public static UserAccount CreateCustomer(string displayName, string phoneDigits, string? passwordHash, string? email = null)
    {
        var account = new UserAccount
        {
            Id = EntityId.New(EntityId.User),
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
        };

        account.UpdateProfile(displayName: displayName);
        account.ChangePhone(phoneDigits);

        if (!string.IsNullOrWhiteSpace(email))
        {
            account.ChangeEmail(email);
        }

        return account;
    }

    /// <summary>
    /// Whether this account may complete a sign-in. Suspended and deactivated accounts are refused
    /// even when the password is correct.
    /// </summary>
    public bool CanAuthenticate => Status == UserStatus.Active;

    public void SetPassword(string passwordHash, DateTimeOffset changedAt)
    {
        PasswordHash = passwordHash;

        // The contract requires a password change to revoke every other session. Stamping the
        // account is what makes that immediate rather than best-effort.
        CredentialsInvalidatedAt = changedAt;
    }

    public void EnrolTwoFactor(TwoFactorMethod method, string secret)
    {
        TwoFactorMethod = method;
        TwoFactorSecret = secret;
    }

    /// <summary>
    /// <c>PATCH /staff/users/{id}/status</c> — "active · suspended · deactivated". Suspended is a
    /// pause and can be lifted; deactivated is the end of the account and cannot be undone here.
    /// Leaving active stamps the account so every session it holds stops being refreshed.
    /// </summary>
    public void SetStatus(UserStatus status, DateTimeOffset changedAt)
    {
        if (Status == UserStatus.Deactivated && status != UserStatus.Deactivated)
        {
            throw new StateConflictException(nameof(UserAccount), "deactivated", status.ToString().ToLowerInvariant());
        }

        Status = status;

        if (status != UserStatus.Active)
        {
            CredentialsInvalidatedAt = changedAt;
        }
    }

    /// <summary>True when a token or session issued at <paramref name="issuedAt"/> is still honoured.</summary>
    public bool IsCredentialCurrent(DateTimeOffset issuedAt) =>
        CredentialsInvalidatedAt is null || issuedAt >= CredentialsInvalidatedAt;
}
