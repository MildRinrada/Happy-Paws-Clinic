using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.CorrectLabResults;

/// <summary>
/// One value as the audit trail writes it: before and after a correction, or as it stood when
/// struck out. Small on purpose, the same few fields every time, so two rows compare at a glance.
/// </summary>
internal static class LabValueSnapshot
{
    public static Dictionary<string, object?> Of(LabResult result) =>
        Of(
            result.Analyte,
            result.ValueNumeric,
            result.ValueText,
            result.Unit,
            result.ReferenceLow,
            result.ReferenceHigh,
            result.ReferenceText,
            result.Flag?.ToString().ToLowerInvariant());

    public static Dictionary<string, object?> Of(LabResultRevision revision) =>
        Of(
            revision.Analyte,
            revision.ValueNumeric,
            revision.ValueText,
            revision.Unit,
            revision.ReferenceLow,
            revision.ReferenceHigh,
            revision.ReferenceText,
            revision.Flag);

    private static Dictionary<string, object?> Of(
        string analyte,
        decimal? valueNumeric,
        string? valueText,
        string? unit,
        decimal? referenceLow,
        decimal? referenceHigh,
        string? referenceText,
        string? flag) =>
        new()
        {
            ["analyte"] = analyte,
            ["value_numeric"] = valueNumeric,
            ["value_text"] = valueText,
            ["unit"] = unit,
            ["reference_low"] = referenceLow,
            ["reference_high"] = referenceHigh,
            ["reference_text"] = referenceText,
            ["flag"] = flag,
        };
}
