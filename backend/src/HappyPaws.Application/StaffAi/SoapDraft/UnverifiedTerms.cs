using System.Globalization;
using System.Text.RegularExpressions;

namespace HappyPaws.Application.StaffAi.SoapDraft;

/// <summary>
/// Something in a draft that the vet's notes do not contain. <c>reason</c> is
/// <c>dose_not_in_notes</c>, <c>number_not_in_notes</c> or <c>term_not_in_notes</c>.
/// </summary>
public sealed record DraftWarningDto(string Field, string Text, string Reason);

/// <summary>
/// The cheap check behind the prompt's "add nothing" rule: anything in the draft that looks like a
/// dose, a number or a Latin-script term (how drug names are usually written) and does not appear in
/// the notes is flagged <c>unverified</c> for the vet to look at.
/// <para>
/// <b>What it catches.</b> A dose such as <c>250 mg</c> or <c>2 เม็ด</c> not in the notes (spacing
/// ignored, so <c>250mg</c> in the notes covers <c>250 mg</c>), any other number not in the notes
/// (a temperature, a heart rate, a count the model made up), and an English or Latin word of four
/// letters or more not in the notes (a drug or disease name written in English).
/// </para>
/// <para>
/// <b>What it does not.</b> A drug or disease written in Thai script (<c>ยาปฏิชีวนะ</c>,
/// <c>โรคไต</c>), a synonym or a translation of something that is in the notes (the notes say
/// "อาเจียน", the draft says "vomiting": flagged although it is right), a number written in words,
/// and meaning: a draft that moves a finding into the wrong field passes. It is a prompt for the
/// vet's attention, not a guarantee, and the page labels every draft as unchecked whatever it says.
/// </para>
/// </summary>
public static partial class UnverifiedTerms
{
    public const string DoseNotInNotes = "dose_not_in_notes";
    public const string NumberNotInNotes = "number_not_in_notes";
    public const string TermNotInNotes = "term_not_in_notes";

    /// <summary>A draft with more than this many is flagged that many times; the vet reads it all anyway.</summary>
    public const int MaxWarnings = 20;

    /// <summary>English words a model uses to write a form, which say nothing clinical.</summary>
    private static readonly HashSet<string> Ordinary = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "normal", "abnormal", "within", "limits", "with", "without", "from", "that", "this", "patient",
        "owner", "history", "plan", "assessment", "subjective", "objective", "note", "notes", "days", "weeks",
        "times", "daily", "once", "twice", "follow", "check", "recheck", "continue", "monitor",
    };

    public static IReadOnlyList<DraftWarningDto> Find(IEnumerable<(string Field, string Text)> fields, string source)
    {
        var sourceLower = source.ToLowerInvariant();
        var sourceCompact = Compact(sourceLower);
        var sourceNumbers = NumberPattern().Matches(source).Select(match => Normalize(match.Value)).ToHashSet(StringComparer.Ordinal);

        var warnings = new List<DraftWarningDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Warn(string field, string text, string reason)
        {
            if (warnings.Count < MaxWarnings && seen.Add($"{field}\u0001{text}"))
            {
                warnings.Add(new DraftWarningDto(field, text, reason));
            }
        }

        foreach (var (field, raw) in fields)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            // Placeholders such as [OWNER_1] are ours, not the model's; their digits are not numbers.
            var text = PlaceholderPattern().Replace(raw, " ");
            var doseSpans = new List<(int Start, int End)>();

            foreach (Match dose in DosePattern().Matches(text))
            {
                doseSpans.Add((dose.Index, dose.Index + dose.Length));

                if (!sourceCompact.Contains(Compact(dose.Value.ToLowerInvariant()), StringComparison.Ordinal))
                {
                    Warn(field, dose.Value.Trim(), DoseNotInNotes);
                }
            }

            foreach (Match number in NumberPattern().Matches(text))
            {
                var insideDose = doseSpans.Any(span => number.Index >= span.Start && number.Index < span.End);

                if (!insideDose && !sourceNumbers.Contains(Normalize(number.Value)))
                {
                    Warn(field, number.Value, NumberNotInNotes);
                }
            }

            foreach (Match word in LatinWordPattern().Matches(text))
            {
                if (!Ordinary.Contains(word.Value) && !sourceLower.Contains(word.Value.ToLowerInvariant(), StringComparison.Ordinal))
                {
                    Warn(field, word.Value, TermNotInNotes);
                }
            }
        }

        return warnings;
    }

    private static string Compact(string text) => WhitespacePattern().Replace(text, string.Empty);

    /// <summary>"39,5" and "39.50" are the same number as "39.5".</summary>
    private static string Normalize(string number)
    {
        var dotted = number.Replace(',', '.');
        return decimal.TryParse(dotted, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value.ToString("0.############", CultureInfo.InvariantCulture)
            : dotted;
    }

    [GeneratedRegex(
        @"\d+(?:[.,]\d+)?\s*(?:mg/kg|mcg|µg|ug|mg|ml|cc|iu|units?|tabs?|caps?|g|kg|มก\.?|มล\.?|ซีซี|เม็ด|แคปซูล|ยูนิต|หยด)(?![A-Za-z])",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        1000)]
    private static partial Regex DosePattern();

    [GeneratedRegex(@"(?<![\d.,])\d+(?:[.,]\d+)?(?![\d])", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"(?<![A-Za-z])[A-Za-z][A-Za-z\-]{3,}(?![A-Za-z])", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex LatinWordPattern();

    [GeneratedRegex(@"\[(?:OWNER|STAFF|PHONE|EMAIL|ID|ADDRESS|PERSON)_\d+\]", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex WhitespacePattern();
}
