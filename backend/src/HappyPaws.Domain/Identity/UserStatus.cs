namespace HappyPaws.Domain.Identity;

/// <summary>
/// Account status, as set by <c>PATCH /staff/users/{id}/status</c>.
/// </summary>
public enum UserStatus
{
    /// <summary>May authenticate normally.</summary>
    Active,

    /// <summary>Temporarily barred — existing sessions are revoked, the account is not erased.</summary>
    Suspended,

    /// <summary>
    /// Permanently disabled. The row stays so that the name on past medical records and audit
    /// entries still resolves; the contract is explicit that deactivating a staff member keeps
    /// their name in existing records.
    /// </summary>
    Deactivated,
}
