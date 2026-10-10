using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HappyPaws.Domain.Articles;

/// <summary>
/// The rules for an article's address, <c>/articles/{slug}</c>
/// (<c>docs/contract-extensions.md § 26</c>).
/// <para>
/// A slug is lowercase ASCII letters, digits and single hyphens, at most
/// <see cref="Article.SlugMaxLength"/> characters. ASCII only, on purpose: a Thai slug becomes a
/// percent-encoded address three times its length that no one can read aloud or type.
/// </para>
/// <para>
/// A slug is made from the title when the writer leaves it out, from the title's Latin letters and
/// digits. A title in Thai has none to make one from, and there is no transliteration here: Thai
/// romanisation needs a dictionary to split syllables, and a wrong one is worse than asking. So a
/// Thai title needs a slug typed by hand, which the editor says next to the field.
/// </para>
/// </summary>
public static partial class ArticleSlug
{
    /// <summary>True when the value is a clean address as it stands.</summary>
    public static bool IsValid(string? slug) =>
        slug is { Length: > 0 and <= Article.SlugMaxLength } && Pattern().IsMatch(slug);

    /// <summary>
    /// Tidies what a person typed: trims, lowercases, turns spaces and underscores into hyphens,
    /// and folds runs of hyphens into one. Characters outside the rule are kept, so
    /// <see cref="IsValid"/> can refuse them rather than have them vanish without a word.
    /// </summary>
    public static string Normalize(string input)
    {
        var lowered = input.Trim().ToLowerInvariant();
        var hyphenated = Separators().Replace(lowered, "-");

        return Hyphens().Replace(hyphenated, "-").Trim('-');
    }

    /// <summary>
    /// A slug made from a title's Latin letters and digits, or null when the title has no Latin
    /// letter to make one from (a title in Thai). Accents are dropped (<c>café</c> to <c>cafe</c>).
    /// A long title is cut at a word, never in the middle of one.
    /// </summary>
    public static string? FromTitle(string title)
    {
        var decomposed = title.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(character);
            builder.Append(lower is (>= 'a' and <= 'z') or (>= '0' and <= '9') ? lower : '-');
        }

        var slug = Hyphens().Replace(builder.ToString(), "-").Trim('-');

        if (!slug.Any(character => character is >= 'a' and <= 'z'))
        {
            return null;
        }

        if (slug.Length > Article.SlugMaxLength)
        {
            var cut = slug[..Article.SlugMaxLength];
            var lastHyphen = cut.LastIndexOf('-');
            slug = (lastHyphen > 0 ? cut[..lastHyphen] : cut).Trim('-');
        }

        return slug;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Pattern();

    [GeneratedRegex(@"[\s_]+")]
    private static partial Regex Separators();

    [GeneratedRegex("-{2,}")]
    private static partial Regex Hyphens();
}
