using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Identity;

namespace HappyPaws.Application.Identity;

/// <summary>
/// "Prove it is still you": the current password, asked again before a change that could hand the
/// account to someone else (a new sign-in email, a new second factor).
/// <para>
/// A wrong or missing password answers <c>422</c> on <c>current_password</c>, not <c>401</c>. The
/// session is fine; it is the request that lacks proof, and a <c>401</c> would make a client
/// think it had been signed out.
/// </para>
/// </summary>
public static class CurrentPassword
{
    public static void Ensure(IPasswordHasher passwordHasher, UserAccount account, string? currentPassword)
    {
        if (account.PasswordHash is null)
        {
            throw Refused("บัญชีนี้ยังไม่มีรหัสผ่าน");
        }

        if (string.IsNullOrEmpty(currentPassword))
        {
            throw Refused("ต้องใส่รหัสผ่านปัจจุบัน");
        }

        if (passwordHasher.Verify(account.PasswordHash, currentPassword) == PasswordVerification.Failed)
        {
            throw Refused("รหัสผ่านปัจจุบันไม่ถูกต้อง");
        }
    }

    private static ValidationFailedException Refused(string message) =>
        new(new Dictionary<string, string[]> { ["current_password"] = [message] });
}
