using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Identity;

/// <summary>
/// What someone may change about themselves — <c>PATCH /me</c>, "ชื่อ อีเมล เบอร์ ภาษา การแจ้งเตือน".
/// </summary>
public sealed partial class UserAccount
{
    public const int DisplayNameMaxLength = 200;
    public const int EmailMaxLength = 256;

    /// <summary>The languages the system speaks. Thai is the default and the contract's own.</summary>
    public static readonly IReadOnlyList<string> Languages = ["th", "en"];

    /// <summary><c>th</c> or <c>en</c>: the language messages to this person are written in.</summary>
    public string Language { get; private set; } = "th";

    /// <summary>Whether this person wants messages by email.</summary>
    public bool NotifyByEmail { get; private set; } = true;

    /// <summary>Whether this person wants messages by SMS.</summary>
    public bool NotifyBySms { get; private set; } = true;

    /// <summary>Whether this person wants messages through LINE.</summary>
    public bool NotifyByLine { get; private set; } = true;

    /// <summary>Changes the name and language. A null argument leaves that one alone.</summary>
    public void UpdateProfile(string? displayName = null, string? language = null)
    {
        if (displayName is not null)
        {
            var name = displayName.Trim();

            if (name.Length is 0 or > DisplayNameMaxLength)
            {
                throw Invalid($"ชื่อต้องมี 1 ถึง {DisplayNameMaxLength} ตัวอักษร");
            }

            DisplayName = name;
        }

        if (language is not null)
        {
            var code = language.Trim().ToLowerInvariant();

            Language = Languages.Contains(code)
                ? code
                : throw Invalid($"ภาษาต้องเป็น {string.Join(" หรือ ", Languages)}");
        }
    }

    /// <summary>Changes the sign-in email. Checking it is free, and that the person proved who they are, is the use case's job.</summary>
    public void ChangeEmail(string email)
    {
        var normalised = NormaliseEmail(email);
        var at = normalised.IndexOf('@');

        // The shape only: something before and after one @. Whether mail arrives is not knowable here.
        Email = normalised.Length <= EmailMaxLength && at > 0 && at < normalised.Length - 1 && !normalised.Contains(' ')
            ? normalised
            : throw Invalid("อีเมลไม่ถูกต้อง");
    }

    /// <summary>Changes the phone number, kept as digits only. An empty value removes it.</summary>
    public void ChangePhone(string? phone)
    {
        var digits = NormalisePhone(phone);

        PhoneNumber = digits is null || digits.Length is >= 9 and <= 15
            ? digits
            : throw Invalid("เบอร์โทรต้องมี 9 ถึง 15 หลัก");
    }

    /// <summary>Which channels this person accepts. A null argument leaves that channel alone.</summary>
    public void SetNotificationChannels(bool? email = null, bool? sms = null, bool? line = null)
    {
        NotifyByEmail = email ?? NotifyByEmail;
        NotifyBySms = sms ?? NotifyBySms;
        NotifyByLine = line ?? NotifyByLine;
    }

    /// <summary>Lower-case and trimmed, the form email is stored and looked up in.</summary>
    public static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Digits only, or null when there are none.</summary>
    public static string? NormalisePhone(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

        return digits.Length == 0 ? null : digits;
    }

    private static DomainException Invalid(string message) => new("invalid_profile", message);
}
