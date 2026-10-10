using System.Globalization;
using System.Text.RegularExpressions;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Domain.Reviews;

/// <summary>
/// What a review and the clinic's reply may say (<c>docs/contract-extensions.md § 36</c>).
/// <para>
/// Plain text, cleaned the way articles are (<see cref="ArticleText.CleanBody"/>): control and
/// bidirectional characters out, line breaks made <c>\n</c>, one blank line at most between
/// paragraphs. The page draws it as text, so nothing in it can run; on top of that, anything that
/// looks like an HTML tag is refused when it is written, as the privacy notice does, because a
/// review that arrives with markup in it was not typed by a customer about their visit.
/// </para>
/// </summary>
public static partial class ReviewText
{
    public const int MinRating = 1;
    public const int MaxRating = 5;

    /// <summary>The longest review, after cleaning.</summary>
    public const int MaxLength = 1_000;

    /// <summary>The longest reply from the clinic, after cleaning.</summary>
    public const int ReplyMaxLength = 1_000;

    public static bool IsRating(int? rating) => rating is >= MinRating and <= MaxRating;

    /// <summary>The text as it is kept: cleaned, and null when nothing is left.</summary>
    public static string? Clean(string? text)
    {
        if (text is null)
        {
            return null;
        }

        var cleaned = ArticleText.CleanBody(text);

        return cleaned.Length == 0 ? null : cleaned;
    }

    /// <summary>
    /// Why a review's text cannot be kept, in Thai, or null when it can. An empty text is allowed:
    /// a score alone is a review.
    /// </summary>
    public static string? Problem(string? text) => ProblemOf(Clean(text), MaxLength, required: false);

    /// <summary>Why a reply cannot be kept, or null. A reply must say something.</summary>
    public static string? ReplyProblem(string? text) => ProblemOf(Clean(text), ReplyMaxLength, required: true);

    private static string? ProblemOf(string? cleaned, int maxLength, bool required)
    {
        if (cleaned is null)
        {
            return required ? "ต้องพิมพ์ข้อความ" : null;
        }

        if (cleaned.Length > maxLength)
        {
            return $"ข้อความยาวได้ไม่เกิน {maxLength.ToString("N0", CultureInfo.InvariantCulture)} ตัวอักษร";
        }

        return HtmlTag().IsMatch(cleaned) ? "ข้อความต้องเป็นตัวอักษรธรรมดา ไม่มีแท็ก HTML" : null;
    }

    /// <summary>
    /// An opening or closing tag: <c>&lt;b&gt;</c>, <c>&lt;/p&gt;</c>, <c>&lt;a href=…&gt;</c>, <c>&lt;!--</c>.
    /// "อุณหภูมิ &lt; 39" and "&lt;3" are not, because a tag name starts with a letter right after the bracket.
    /// </summary>
    [GeneratedRegex(@"</?[A-Za-z][A-Za-z0-9-]*(\s[^<>]*)?/?>|<!--", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTag();
}
