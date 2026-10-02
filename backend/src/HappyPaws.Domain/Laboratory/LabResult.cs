using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Laboratory;

/// <summary>
/// One measured value — "ค่าที่วัดได้ + ช่วงอ้างอิง · ธงค่าผิดปกติ".
/// <para>
/// The reference range is stored with each value, not looked up from a table. Ranges differ
/// between labs, analysers and species, and the range that matters is the one printed next to this
/// number by whoever measured it.
/// </para>
/// <para>
/// The flag is computed here when the value is a number and a range was given, and taken from the
/// caller only when it cannot be computed. A lab system that sends "H" next to a value inside its
/// own range is a discrepancy, and the number wins.
/// </para>
/// </summary>
public sealed class LabResult
{
    private LabResult()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string LabRequestId { get; private set; } = string.Empty;

    /// <summary>What was measured — <c>ALT</c>, <c>Hematocrit</c>.</summary>
    public string Analyte { get; private set; } = string.Empty;

    public decimal? ValueNumeric { get; private set; }

    /// <summary>A value that is not a number — <c>positive</c>, "พบไข่พยาธิ".</summary>
    public string? ValueText { get; private set; }

    public string? Unit { get; private set; }

    public decimal? ReferenceLow { get; private set; }

    public decimal? ReferenceHigh { get; private set; }

    /// <summary>A range that is not two numbers — <c>negative</c>.</summary>
    public string? ReferenceText { get; private set; }

    /// <summary>Null when neither a range nor the caller said whether the value is normal.</summary>
    public LabFlag? Flag { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public string RecordedByStaffId { get; private set; } = string.Empty;

    public bool IsAbnormal => Flag is LabFlag.Low or LabFlag.High or LabFlag.Abnormal;

    internal static LabResult Record(
        string labRequestId,
        LabResultInput input,
        string recordedByStaffId,
        DateTimeOffset recordedAt)
    {
        if (string.IsNullOrWhiteSpace(input.Analyte))
        {
            throw Invalid("ต้องระบุชื่อค่าที่ตรวจ");
        }

        if (input.ValueNumeric is null && string.IsNullOrWhiteSpace(input.ValueText))
        {
            throw Invalid($"ค่า {input.Analyte} ต้องมีผลเป็นตัวเลขหรือข้อความ");
        }

        if (input.ReferenceLow is { } low && input.ReferenceHigh is { } high && low > high)
        {
            throw Invalid($"ช่วงอ้างอิงของ {input.Analyte} ค่าต่ำสุดมากกว่าค่าสูงสุด");
        }

        return new LabResult
        {
            Id = EntityId.New(EntityId.LabResult),
            LabRequestId = labRequestId,
            Analyte = input.Analyte.Trim(),
            ValueNumeric = input.ValueNumeric,
            ValueText = Trimmed(input.ValueText),
            Unit = Trimmed(input.Unit),
            ReferenceLow = input.ReferenceLow,
            ReferenceHigh = input.ReferenceHigh,
            ReferenceText = Trimmed(input.ReferenceText),
            Flag = FlagFor(input),
            RecordedAt = recordedAt,
            RecordedByStaffId = recordedByStaffId,
        };
    }

    /// <summary>Below the range is low, above is high, inside is normal. Only the caller can judge text.</summary>
    public static LabFlag? FlagFor(LabResultInput input)
    {
        if (input.ValueNumeric is { } value && (input.ReferenceLow is not null || input.ReferenceHigh is not null))
        {
            if (input.ReferenceLow is { } low && value < low)
            {
                return LabFlag.Low;
            }

            if (input.ReferenceHigh is { } high && value > high)
            {
                return LabFlag.High;
            }

            return LabFlag.Normal;
        }

        return input.Abnormal switch
        {
            true => LabFlag.Abnormal,
            false => LabFlag.Normal,
            null => null,
        };
    }

    private static DomainException Invalid(string message) => new("invalid_lab_result", message);

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// What the caller sends for one value. <c>Abnormal</c> is the caller's own judgement, used only
/// when no numeric range decides it.
/// </summary>
public sealed record LabResultInput(
    string Analyte,
    decimal? ValueNumeric = null,
    string? ValueText = null,
    string? Unit = null,
    decimal? ReferenceLow = null,
    decimal? ReferenceHigh = null,
    string? ReferenceText = null,
    bool? Abnormal = null);
