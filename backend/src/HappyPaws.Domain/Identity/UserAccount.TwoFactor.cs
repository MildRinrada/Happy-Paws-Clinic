using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Identity;

/// <summary>
/// Setting up one's own second factor — <c>POST /me/2fa/enroll</c>, "คืน TOTP secret และ recovery codes".
/// </summary>
public sealed partial class UserAccount
{
    private List<string> _recoveryCodeHashes = [];

    /// <summary>
    /// A TOTP secret shown to the person and not proven yet. It replaces the active one only when
    /// a code from it verifies, so someone who closes the page before scanning the QR code has
    /// lost nothing, and a second factor that already works keeps working until then.
    /// </summary>
    public string? PendingTwoFactorSecret { get; private set; }

    /// <summary>SHA-256 of each unused recovery code. The codes themselves are shown once and never stored.</summary>
    public IReadOnlyList<string> RecoveryCodeHashes => _recoveryCodeHashes;

    /// <summary>
    /// Starts setting up a second factor. Asking again returns the same pending secret, so a page
    /// refresh does not invalidate a QR code already scanned.
    /// </summary>
    public string BeginTwoFactorEnrolment(Func<string> newSecret) => PendingTwoFactorSecret ??= newSecret();

    /// <summary>
    /// Makes the pending secret the account's second factor, replacing any earlier one along with
    /// every recovery code issued for it.
    /// </summary>
    /// <param name="recoveryCodeHashes">Hashes of the fresh recovery codes.</param>
    public void CompleteTwoFactorEnrolment(IReadOnlyList<string> recoveryCodeHashes)
    {
        var secret = PendingTwoFactorSecret
            ?? throw new StateConflictException(nameof(UserAccount), "no_pending_two_factor", "complete_two_factor");

        EnrolTwoFactor(TwoFactorMethod.Totp, secret);
        PendingTwoFactorSecret = null;
        _recoveryCodeHashes = [.. recoveryCodeHashes];
    }

    /// <summary>Spends a recovery code. True when it was one of this account's unused codes; each works once.</summary>
    public bool TryUseRecoveryCode(string codeHash) => _recoveryCodeHashes.Remove(codeHash);
}
