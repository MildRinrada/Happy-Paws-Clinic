using System.Security.Cryptography;
using System.Text;
using HappyPaws.Application.Identity;

namespace HappyPaws.Infrastructure.Identity;

/// <summary>
/// TOTP (RFC 6238) enrolment and verification.
/// <para>
/// Implemented directly rather than pulled in as a dependency: the algorithm is thirty lines of
/// HMAC and a modulus, and it is easier to review here than to audit a transitive package that
/// guards every medical-record endpoint in the system.
/// </para>
/// </summary>
public sealed class TotpService : ITwoFactorService
{
    private const int Digits = 6;
    private const int PeriodSeconds = 30;

    /// <summary>
    /// Accept the neighbouring steps as well as the current one — roughly ±30 seconds. Enough for
    /// a phone whose clock has drifted, small enough that a shoulder-surfed code expires quickly.
    /// </summary>
    private const int DriftSteps = 1;

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public TwoFactorEnrolment Enrol(string accountLabel)
    {
        var secret = ToBase32(RandomNumberGenerator.GetBytes(20));

        var recoveryCodes = Enumerable.Range(0, 8)
            .Select(_ => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(5)))
            .ToArray();

        return new TwoFactorEnrolment(secret, OtpAuthUri(accountLabel, secret), recoveryCodes);
    }

    public string OtpAuthUri(string accountLabel, string secret) =>
        $"otpauth://totp/{Uri.EscapeDataString(accountLabel)}"
        + $"?secret={secret}&issuer={Uri.EscapeDataString("Happy Paws Clinic")}"
        + $"&digits={Digits}&period={PeriodSeconds}";

    /// <summary>
    /// The counterpart of <see cref="VerifyCode"/>: the code an authenticator app would show at a
    /// given instant.
    /// <para>
    /// Public so that tests and diagnostics drive the same RFC 6238 implementation the server
    /// verifies against, instead of a second copy that could agree with itself while both are
    /// wrong. Not on <see cref="ITwoFactorService"/> — nothing in the request path needs it.
    /// </para>
    /// </summary>
    public string GenerateCode(string secret, DateTimeOffset at) =>
        Compute(FromBase32(secret), at.ToUnixTimeSeconds() / PeriodSeconds);

    public bool VerifyCode(string secret, string code, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        byte[] key;
        try
        {
            key = FromBase32(secret);
        }
        catch (FormatException)
        {
            return false;
        }

        var step = now.ToUnixTimeSeconds() / PeriodSeconds;
        var matched = false;

        // Every candidate is evaluated even after a match, so verification takes the same time
        // whether the code was right on the first step or wrong on all of them.
        for (var offset = -DriftSteps; offset <= DriftSteps; offset++)
        {
            var candidate = Compute(key, step + offset);
            matched |= CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(candidate),
                Encoding.ASCII.GetBytes(code.PadRight(candidate.Length).AsSpan(0, candidate.Length).ToString()));
        }

        return matched;
    }

    private static string Compute(byte[] key, long step)
    {
        Span<byte> counter = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counter[i] = (byte)(step & 0xFF);
            step >>= 8;
        }

        Span<byte> hash = stackalloc byte[20];
        HMACSHA1.HashData(key, counter, hash);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        return (binary % (int)Math.Pow(10, Digits)).ToString().PadLeft(Digits, '0');
    }

    private static string ToBase32(ReadOnlySpan<byte> bytes)
    {
        var builder = new StringBuilder();
        var bitBuffer = 0;
        var bitCount = 0;

        foreach (var b in bytes)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;

            while (bitCount >= 5)
            {
                bitCount -= 5;
                builder.Append(Base32Alphabet[(bitBuffer >> bitCount) & 0x1F]);
            }
        }

        if (bitCount > 0)
        {
            builder.Append(Base32Alphabet[(bitBuffer << (5 - bitCount)) & 0x1F]);
        }

        return builder.ToString();
    }

    private static byte[] FromBase32(string value)
    {
        var bytes = new List<byte>(value.Length * 5 / 8);
        var bitBuffer = 0;
        var bitCount = 0;

        foreach (var c in value.TrimEnd('=').ToUpperInvariant())
        {
            var index = Base32Alphabet.IndexOf(c, StringComparison.Ordinal);
            if (index < 0)
            {
                throw new FormatException("Secret is not valid base32.");
            }

            bitBuffer = (bitBuffer << 5) | index;
            bitCount += 5;

            if (bitCount >= 8)
            {
                bitCount -= 8;
                bytes.Add((byte)((bitBuffer >> bitCount) & 0xFF));
            }
        }

        return [.. bytes];
    }
}
