using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HappyPaws.Domain.Articles;

/// <summary>
/// What is cleaned out of an article's text before it is kept
/// (<c>docs/contract-extensions.md § 26</c>).
/// <para>
/// The body is plain text and is rendered as text everywhere: the API answers JSON, and the public
/// site puts each paragraph in a <c>&lt;p&gt;</c> as a text node, so <c>&lt;script&gt;</c> in an
/// article is shown as those characters and never runs. Angle brackets are therefore <b>not</b>
/// removed: "อุณหภูมิ &lt; 39 องศา" is a sentence a vet writes. What is removed is what has no
/// business in text a stranger reads:
/// </para>
/// <list type="bullet">
/// <item>control characters other than line breaks (a NUL also cannot be stored by PostgreSQL),</item>
/// <item>bidirectional overrides and isolates, which make text display in an order other than the
/// one it is stored in, the trick behind a link that reads as one address and goes to another,</item>
/// <item>a byte-order mark, and half of a broken surrogate pair.</item>
/// </list>
/// <para>
/// Line breaks are made <c>\n</c>, trailing spaces on a line are dropped, and more than one blank
/// line in a row becomes one, because a blank line is what separates paragraphs and two mean
/// nothing more. The zero-width space stays: Thai writers use it to mark where a line may wrap.
/// </para>
/// </summary>
public static partial class ArticleText
{
    /// <summary>Roughly how many characters of Thai prose a reader gets through in a minute.</summary>
    public const int CharactersPerMinute = 800;

    /// <summary>The body: paragraphs separated by one blank line, nothing that is not text.</summary>
    public static string CleanBody(string body)
    {
        var text = Strip(body.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n'), keepNewLines: true);
        text = TrailingSpaces().Replace(text, "\n");
        text = ExtraBlankLines().Replace(text, "\n\n");

        return text.Trim();
    }

    /// <summary>A title, summary or byline: one line, single spaces, nothing that is not text.</summary>
    public static string CleanLine(string line) =>
        Whitespace().Replace(Strip(line, keepNewLines: false), " ").Trim();

    /// <summary>Reading time for a body, 1 to 120 minutes, for when the writer does not give one.</summary>
    public static int EstimateReadingMinutes(string body) =>
        Math.Clamp((int)Math.Ceiling(body.Length / (double)CharactersPerMinute), 1, 120);

    private static string Strip(string text, bool keepNewLines)
    {
        var builder = new StringBuilder(text.Length);

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];

            if (char.IsHighSurrogate(character))
            {
                if (index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                {
                    builder.Append(character).Append(text[index + 1]);
                    index++;
                }

                continue;
            }

            if (char.IsLowSurrogate(character) || IsUnwanted(character))
            {
                continue;
            }

            if (character == '\n')
            {
                builder.Append(keepNewLines ? '\n' : ' ');
                continue;
            }

            builder.Append(character == '\t' ? ' ' : character);
        }

        return builder.ToString();
    }

    private static bool IsUnwanted(char character) =>
        character switch
        {
            '\n' or '\t' => false,
            '\uFEFF' => true,
            >= '\u202A' and <= '\u202E' => true,
            >= '\u2066' and <= '\u2069' => true,
            _ => CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Control,
        };

    [GeneratedRegex(@"[  ]+\n")]
    private static partial Regex TrailingSpaces();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExtraBlankLines();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
