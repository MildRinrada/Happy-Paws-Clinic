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
/// <para>
/// A value is corrected, never overwritten without trace (<c>docs/contract-extensions.md § 52</c>).
/// <see cref="Amend"/> keeps every value it replaces in <see cref="PreviousValues"/>, and
/// <see cref="Void"/> keeps the value and marks it as not to be used. A voided value counts towards
/// nothing: not the abnormal count, not the timeline, not what the owner sees.
/// </para>
/// </summary>
public sealed class LabResult
{
    /// <summary>The longest reason a correction or a void may give.</summary>
    public const int MaxReasonLength = 500;

    private readonly List<LabResultRevision> _previousValues = [];

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

    /// <summary>When the value was last corrected. Null for a value as first entered.</summary>
    public DateTimeOffset? AmendedAt { get; private set; }

    public string? AmendedByStaffId { get; private set; }

    /// <summary>Why the last correction was made.</summary>
    public string? AmendReason { get; private set; }

    /// <summary>Every value this one replaced, oldest first, each with who replaced it and why.</summary>
    public IReadOnlyList<LabResultRevision> PreviousValues => _previousValues;

    /// <summary>When the value was struck out. A voided value stays on the request, marked.</summary>
    public DateTimeOffset? VoidedAt { get; private set; }

    public string? VoidedByStaffId { get; private set; }

    public string? VoidReason { get; private set; }

    public bool IsAmended => AmendedAt is not null;

    public bool IsVoided => VoidedAt is not null;

    /// <summary>Outside its range and still in use. A voided value is abnormal for no one.</summary>
    public bool IsAbnormal => !IsVoided && Flag is LabFlag.Low or LabFlag.High or LabFlag.Abnormal;

    internal static LabResult Record(
        string labRequestId,
        LabResultInput input,
        string recordedByStaffId,
        DateTimeOffset recordedAt)
    {
        Check(input);

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

    /// <summary>
    /// Replaces the value with a corrected one: a typing mistake, or a corrected report from the
    /// lab. The value it replaces is kept in <see cref="PreviousValues"/> with who replaced it,
    /// when and why, and the flag is worked out again from the new value. The analyte keeps its
    /// name when the input gives none. A voided value cannot be corrected; enter a new one instead.
    /// </summary>
    internal void Amend(LabResultInput input, string reason, string staffId, DateTimeOffset at)
    {
        if (IsVoided)
        {
            throw new StateConflictException(nameof(LabResult), "voided", nameof(Amend));
        }

        var corrected = string.IsNullOrWhiteSpace(input.Analyte) ? input with { Analyte = Analyte } : input;
        Check(corrected);
        var why = RequireReason(reason);

        _previousValues.Add(new LabResultRevision(
            Analyte,
            ValueNumeric,
            ValueText,
            Unit,
            ReferenceLow,
            ReferenceHigh,
            ReferenceText,
            Flag is { } flag ? flag.ToString().ToLowerInvariant() : null,
            at,
            staffId,
            why));

        Analyte = corrected.Analyte.Trim();
        ValueNumeric = corrected.ValueNumeric;
        ValueText = Trimmed(corrected.ValueText);
        Unit = Trimmed(corrected.Unit);
        ReferenceLow = corrected.ReferenceLow;
        ReferenceHigh = corrected.ReferenceHigh;
        ReferenceText = Trimmed(corrected.ReferenceText);
        Flag = FlagFor(corrected);
        AmendedAt = at;
        AmendedByStaffId = staffId;
        AmendReason = why;
    }

    /// <summary>
    /// Strikes the value out: it stays on the request, marked, and counts towards nothing. For a
    /// value entered against the wrong patient or the wrong request. Voiding twice is a conflict,
    /// so the first reason is the one on record.
    /// </summary>
    internal void Void(string reason, string staffId, DateTimeOffset at)
    {
        if (IsVoided)
        {
            throw new StateConflictException(nameof(LabResult), "voided", nameof(Void));
        }

        VoidReason = RequireReason(reason);
        VoidedAt = at;
        VoidedByStaffId = staffId;
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

    private static void Check(LabResultInput input)
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
    }

    private static string RequireReason(string? reason)
    {
        var trimmed = reason?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw Invalid("ต้องระบุเหตุผลที่แก้ผล");
        }

        return trimmed.Length <= MaxReasonLength
            ? trimmed
            : throw Invalid($"เหตุผลยาวได้ไม่เกิน {MaxReasonLength} ตัวอักษร");
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

/// <summary>
/// A value a correction replaced, as it stood, with who replaced it, when and why
/// (<c>docs/contract-extensions.md § 52</c>). Kept on the result as one <c>jsonb</c> list, read and
/// written whole.
/// </summary>
/// <param name="Analyte">The name the value had.</param>
/// <param name="ValueNumeric">The number, when it was one.</param>
/// <param name="ValueText">The text, when it was not.</param>
/// <param name="Unit">Its unit.</param>
/// <param name="ReferenceLow">Its range's low end.</param>
/// <param name="ReferenceHigh">Its range's high end.</param>
/// <param name="ReferenceText">Its range in words.</param>
/// <param name="Flag">Its flag in wire form (<c>high</c>), or null.</param>
/// <param name="ReplacedAt">When the correction replaced it.</param>
/// <param name="ReplacedByStaffId">Who corrected it.</param>
/// <param name="Reason">Why.</param>
public sealed record LabResultRevision(
    string Analyte,
    decimal? ValueNumeric,
    string? ValueText,
    string? Unit,
    decimal? ReferenceLow,
    decimal? ReferenceHigh,
    string? ReferenceText,
    string? Flag,
    DateTimeOffset ReplacedAt,
    string ReplacedByStaffId,
    string Reason);
