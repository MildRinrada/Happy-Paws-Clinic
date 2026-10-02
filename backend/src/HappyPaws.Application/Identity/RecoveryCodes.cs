using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HappyPaws.Application.Identity;

/// <summary>
/// One-time codes for signing in when the authenticator app is gone — "recovery codes" in
/// <c>POST /me/2fa/enroll</c>.
/// <para>
/// Ten hex characters in two groups (<c>3f9a2-c81d0</c>), 40 bits each: enough that guessing one
/// within the sign-in rate limit is hopeless, short enough to type from paper. Only a SHA-256 of
/// each is kept. Unlike a password, a code is random and single-use, so a slow hash adds nothing.
/// </para>
/// </summary>
public static partial class RecoveryCodes
{
    public const int Count = 8;

    /// <summary>Fresh codes, as the person will see them.</summary>
    public static IReadOnlyList<string> Generate()
    {
        var codes = new string[Count];

        for (var index = 0; index < Count; index++)
        {
            var hex = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(5));
            codes[index] = $"{hex[..5]}-{hex[5..]}";
        }

        return codes;
    }

    /// <summary>True when the text has a recovery code's shape, so sign-in knows which check to make.</summary>
    public static bool LooksLikeOne(string text) => Shape().IsMatch(Normalise(text));

    /// <summary>The stored form of a code, however the person typed it.</summary>
    public static string Hash(string code) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Normalise(code))));

    private static string Normalise(string code) => code.Trim().ToLowerInvariant();

    [GeneratedRegex("^[0-9a-f]{5}-[0-9a-f]{5}$")]
    private static partial Regex Shape();
}
