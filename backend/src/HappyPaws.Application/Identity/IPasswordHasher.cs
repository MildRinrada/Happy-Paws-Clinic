namespace HappyPaws.Application.Identity;

/// <summary>
/// Password hashing and verification. Implemented over ASP.NET Core Identity's
/// <c>PasswordHasher</c> (PBKDF2 with a per-user salt) in the Infrastructure layer.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerification Verify(string hash, string password);
}

public enum PasswordVerification
{
    Failed,
    Success,

    /// <summary>Correct, but hashed with outdated parameters — re-hash and store on the way through.</summary>
    SuccessRehashNeeded,
}
