using HappyPaws.Application.Identity;
using Microsoft.AspNetCore.Identity;

namespace HappyPaws.Infrastructure.Identity;

/// <summary>
/// Password hashing over ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> — PBKDF2
/// with a per-user salt and a versioned format.
/// <para>
/// We use Identity's hashing primitive without adopting the full Identity cookie and UI stack,
/// because the contract defines its own token, session and 2FA endpoints. Borrowing the reviewed
/// hasher is worth far more than hand-rolling one; adopting the rest would fight the contract.
/// </para>
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    /// <summary>The type argument is unused by the algorithm; the hasher never inspects the user.</summary>
    private readonly PasswordHasher<object> _hasher = new();

    private static readonly object Placeholder = new();

    public string Hash(string password) => _hasher.HashPassword(Placeholder, password);

    public PasswordVerification Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(Placeholder, hash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed,
        };
}
