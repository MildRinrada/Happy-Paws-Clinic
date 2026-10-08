using System.Text;
using System.Text.RegularExpressions;

namespace HappyPaws.Application.Notifications.Templates;

/// <summary>
/// Fills a template's text: <c>{name}</c> becomes its value, and a <c>[ ... ]</c> segment is kept
/// only when every value inside it is present.
/// <para>
/// Values are inserted as given. Nothing a value contains is read as template syntax, so an owner
/// named "[x]" or a staff message with braces comes out as typed.
/// </para>
/// </summary>
public static partial class TemplateText
{
    public static string Fill(string pattern, IReadOnlyDictionary<string, string> values)
    {
        var output = new StringBuilder(pattern.Length + 32);
        var position = 0;

        while (position < pattern.Length)
        {
            var open = pattern.IndexOf('[', position);

            if (open < 0)
            {
                output.Append(Substitute(pattern[position..], values, out _));
                break;
            }

            var close = pattern.IndexOf(']', open + 1);

            if (close < 0)
            {
                output.Append(Substitute(pattern[position..], values, out _));
                break;
            }

            output.Append(Substitute(pattern[position..open], values, out _));

            var segment = Substitute(pattern[(open + 1)..close], values, out var complete);

            if (complete)
            {
                output.Append(segment);
            }

            position = close + 1;
        }

        return Spaces().Replace(output.ToString(), " ").Trim();
    }

    /// <summary>The variable names a pattern reads, for checking a catalogue entry against its own list.</summary>
    public static IReadOnlySet<string> NamesIn(string pattern) =>
        Placeholder().Matches(pattern).Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

    private static string Substitute(string text, IReadOnlyDictionary<string, string> values, out bool complete)
    {
        var allPresent = true;

        var result = Placeholder().Replace(text, match =>
        {
            if (values.TryGetValue(match.Groups[1].Value, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            allPresent = false;
            return string.Empty;
        });

        complete = allPresent;
        return result;
    }

    [GeneratedRegex(@"\{([a-z_]+)\}")]
    private static partial Regex Placeholder();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex Spaces();
}
