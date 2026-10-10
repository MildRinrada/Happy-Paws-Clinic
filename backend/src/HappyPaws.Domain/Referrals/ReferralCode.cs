using System.Security.Cryptography;
using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Referrals;

/// <summary>
/// A customer's own referral code, the part after <c>/r/</c> in the link they share
/// (<c>docs/contract-extensions.md § 39</c>). One per customer, never changed, made the first time
/// the customer opens the referral page.
/// <para>
/// <b>Eight characters people can read aloud.</b> The alphabet leaves out 0, O, 1, I and L, which
/// are misread in print and over the phone. 31 characters in 8 places is about 850,000 million
/// codes, so a guessed code is almost never someone's, and the public check is rate-limited on top.
/// </para>
/// </summary>
public sealed class ReferralCode : Entity
{
    public const int Length = 8;

    /// <summary>Digits 2 to 9 and the letters A to Z without I, L and O.</summary>
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    private ReferralCode()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    /// <summary>A new code for a customer, from the cryptographic random source.</summary>
    public static ReferralCode For(string customerId) => new()
    {
        Id = EntityId.New(EntityId.ReferralCode),
        CustomerId = customerId,
        Code = Generate(),
    };

    /// <summary>Eight characters, each drawn evenly from <see cref="Alphabet"/>.</summary>
    public static string Generate()
    {
        Span<char> chars = stackalloc char[Length];

        for (var i = 0; i < Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    /// <summary>
    /// The code as typed, in the form it is stored: upper case, without spaces or dashes. Null when
    /// what is left is not eight characters of the alphabet, so a malformed code never reaches the
    /// database.
    /// </summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 32)
        {
            return null;
        }

        var cleaned = new string([.. value.Where(c => c is not (' ' or '-')).Select(char.ToUpperInvariant)]);

        return cleaned.Length == Length && cleaned.All(c => Alphabet.Contains(c, StringComparison.Ordinal)) ? cleaned : null;
    }
}
