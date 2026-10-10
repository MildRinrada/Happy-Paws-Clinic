using HappyPaws.Domain.Articles;

namespace HappyPaws.Domain.Reviews;

/// <summary>
/// The name a published review is shown under (<c>docs/contract-extensions.md § 36</c>).
/// <para>
/// The public page never shows a surname, a phone number, an email or a pet's name. A customer who
/// chooses <see cref="ReviewNameChoice.FirstName"/> is shown as the first word of the name the clinic
/// has for them, without a title in front of it: "คุณสมชาย ใจดี" and "นายสมชาย ใจดี" are both
/// "สมชาย". Everyone else, and anyone whose name leaves nothing once the title is taken off, is
/// "ลูกค้า".
/// </para>
/// <para>
/// The name is worked out once, when the review is written, and kept with it: the customer saw
/// what would be shown before they sent it, so a later change to their record does not change what
/// they agreed to.
/// </para>
/// </summary>
public static class ReviewNames
{
    /// <summary>What an anonymous review, or one with no usable first name, is shown as.</summary>
    public const string Anonymous = "ลูกค้า";

    /// <summary>A first name longer than this is cut: it is a label under a card, not a record.</summary>
    public const int MaxLength = 40;

    /// <summary>Titles taken off the front of a name, longest first so "นางสาว" is not read as "นาง".</summary>
    private static readonly string[] ThaiTitles = ["นางสาว", "ด.ช.", "ด.ญ.", "น.ส.", "นาง", "นาย", "คุณ", "ดร."];

    private static readonly string[] EnglishTitles = ["Mrs.", "Miss", "Mr.", "Ms.", "Dr.", "Mrs", "Mr", "Ms", "Dr"];

    /// <summary>The name kept with a review: the first name for <see cref="ReviewNameChoice.FirstName"/>, null otherwise.</summary>
    public static string? PublicNameFor(ReviewNameChoice choice, string? customerName) =>
        choice == ReviewNameChoice.FirstName ? FirstNameOf(customerName) : null;

    /// <summary>What the public page prints for a kept name.</summary>
    public static string Display(string? publicName) =>
        string.IsNullOrWhiteSpace(publicName) ? Anonymous : publicName;

    /// <summary>The first word of a name without its title, or null when nothing is left.</summary>
    public static string? FirstNameOf(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var text = WithoutTitle(ArticleText.CleanLine(name));
        var first = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        if (string.IsNullOrEmpty(first))
        {
            return null;
        }

        return first.Length > MaxLength ? first[..MaxLength] : first;
    }

    private static string WithoutTitle(string name)
    {
        foreach (var title in ThaiTitles)
        {
            if (!name.StartsWith(title, StringComparison.Ordinal))
            {
                continue;
            }

            var rest = name[title.Length..];

            // A title and nothing else is no name.
            if (rest.Trim().Length == 0)
            {
                return string.Empty;
            }

            // Written with a space ("คุณ สมชาย"), or straight on ("คุณสมชาย") when what follows can
            // start a Thai word. "คุณากร" is a name, not คุณ + "ากร": า cannot begin a word.
            if (rest.StartsWith(' ') || (rest.Length > 0 && StartsThaiWord(rest[0])))
            {
                return rest.TrimStart();
            }
        }

        foreach (var title in EnglishTitles)
        {
            if (name.StartsWith(title + " ", StringComparison.OrdinalIgnoreCase))
            {
                return name[title.Length..].TrimStart();
            }
        }

        return name;
    }

    /// <summary>A Thai consonant, or a vowel written before its consonant (เ แ โ ใ ไ).</summary>
    private static bool StartsThaiWord(char character) =>
        character is (>= 'ก' and <= 'ฮ') or (>= 'เ' and <= 'ไ');
}
