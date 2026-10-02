using System.Security.Cryptography;

namespace HappyPaws.Application.Identity.Sessions;

/// <summary>
/// Generates refresh tokens and the hashes stored in their place.
/// <para>
/// The database holds only the SHA-256 hash. A leaked backup then yields no usable sessions —
/// the same reasoning as password hashing, applied to the credential that is actually long-lived.
/// SHA-256 without a work factor is right here: unlike a password, the token is 256 bits of
/// entropy, so there is nothing to brute-force and nothing to slow an attacker down for.
/// </para>
/// </summary>
public static class RefreshTokens
{
    public static string Generate() =>
        Base64Url(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
