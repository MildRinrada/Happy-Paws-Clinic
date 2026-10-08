namespace HappyPaws.Domain.Notifications;

/// <summary>
/// How an address is written into the delivery log: enough to tell which one was used, not enough
/// to copy it.
/// </summary>
public static class RecipientMask
{
    /// <summary><c>081-234-5678</c> → <c>***5678</c>.</summary>
    public static string Phone(string phone)
    {
        var digits = new string([.. phone.Where(char.IsDigit)]);
        return $"***{digits[^Math.Min(4, digits.Length)..]}";
    }

    /// <summary><c>somchai@mail.com</c> → <c>s***@mail.com</c>.</summary>
    public static string Email(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);

        return at <= 0 ? "***" : $"{email[0]}***{email[at..]}";
    }

    /// <summary>A LINE user id (<c>U</c> and 32 hex digits) → <c>U***a1b2</c>.</summary>
    public static string Line(string lineUserId) =>
        lineUserId.Length <= 5 ? "***" : $"{lineUserId[0]}***{lineUserId[^4..]}";
}
