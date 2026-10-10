using System.Text.RegularExpressions;

namespace HappyPaws.Application.StaffAi.PetSummary;

/// <summary>
/// One record the model was shown, under the short reference it was shown with (<c>R3</c>).
/// <para>
/// <c>kind</c> is <c>allergy</c>, <c>visit</c>, <c>medicine</c>, <c>weight</c>, <c>lab</c> or
/// <c>vaccination</c>; <c>id</c> is the record's own id (for a weight, the consultation that
/// measured it), so the page can link to it.
/// </para>
/// </summary>
public sealed record SummarySource(string Ref, string Kind, string Id, DateOnly Date);

/// <summary>A source the summary actually cited, as the response lists it.</summary>
public sealed record PetSummaryCitationDto(string Ref, string Kind, string Id, DateOnly Date);

/// <summary>
/// Reads the model's citations out of its text and keeps only the honest ones.
/// <para>
/// The model is told to put <c>[R3]</c> after each fact, using only the references it was given.
/// Models do not always obey, so every marker is checked against the list: a reference that was
/// never given (made up, or mistyped) is taken out of the text, and the citation list holds only
/// references that exist, each once, in the order they first appear.
/// </para>
/// </summary>
public static partial class Citations
{
    public static (string Text, IReadOnlyList<PetSummaryCitationDto> Cited) Validate(
        string text,
        IReadOnlyList<SummarySource> sources)
    {
        var known = sources.ToDictionary(source => source.Ref, StringComparer.OrdinalIgnoreCase);
        var cited = new List<PetSummaryCitationDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var cleaned = MarkerPattern().Replace(text, match =>
        {
            var kept = ReferencePattern().Matches(match.Value)
                .Select(reference => reference.Value.ToUpperInvariant())
                .Where(known.ContainsKey)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var reference in kept)
            {
                if (seen.Add(reference))
                {
                    var source = known[reference];
                    cited.Add(new PetSummaryCitationDto(source.Ref, source.Kind, source.Id, source.Date));
                }
            }

            return kept.Count == 0 ? string.Empty : string.Concat(kept.Select(reference => $"[{reference}]"));
        });

        // Removing a marker can leave "ค่ะ ." or a double space behind.
        cleaned = SpaceBeforePunctuation().Replace(cleaned, "$1");
        cleaned = RepeatedSpaces().Replace(cleaned, " ").Trim();

        return (cleaned, cited);
    }

    /// <summary><c>[R3]</c>, <c>[R1, R4]</c>, <c>[r2;R5]</c>.</summary>
    [GeneratedRegex(@"\[\s*[Rr]\d+(?:\s*[,;/]\s*[Rr]\d+)*\s*\]", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex MarkerPattern();

    [GeneratedRegex(@"[Rr]\d+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ReferencePattern();

    [GeneratedRegex(@"[ \t]+([.,;:)])", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex SpaceBeforePunctuation();

    [GeneratedRegex(@"[ \t]{2,}", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex RepeatedSpaces();
}
