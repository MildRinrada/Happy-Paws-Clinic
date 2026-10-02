using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Consultations;

/// <summary>
/// One set of vitals — <c>POST /staff/consultations/{id}/vitals</c>,
/// "ผู้ช่วยกรอกได้ · น้ำหนักเข้ากราฟอัตโนมัติ".
/// <para>
/// Readings <b>accumulate</b> rather than overwrite. A patient under observation is weighed and
/// re-checked during the same visit, and a nurse correcting a mistyped temperature is a new
/// observation, not an erasure of the old one. The latest reading is the current one.
/// </para>
/// <para>
/// This is also the weight history: <c>GET /staff/pets/{id}/weights</c> — "ชุดข้อมูลกราฟน้ำหนัก
/// ย้อนหลัง" — reads these rows by pet. A separate weights table would be a second copy of the same
/// fact, free to disagree with this one.
/// </para>
/// </summary>
public sealed class VitalsReading
{
    private VitalsReading()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string ConsultationId { get; private set; } = string.Empty;

    /// <summary>Denormalised so the weight graph reads by patient without joining consultations.</summary>
    public string PetId { get; private set; } = string.Empty;

    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>Who took them — often an assistant rather than the vet on the case.</summary>
    public string RecordedByStaffId { get; private set; } = string.Empty;

    public decimal? WeightKg { get; private set; }

    public decimal? TemperatureC { get; private set; }

    public int? HeartRateBpm { get; private set; }

    public int? RespiratoryRateBpm { get; private set; }

    /// <summary>Mucous-membrane colour, capillary refill and anything else observed in passing.</summary>
    public string? Note { get; private set; }

    public static VitalsReading Record(
        string consultationId,
        string petId,
        string recordedByStaffId,
        DateTimeOffset recordedAt,
        decimal? weightKg = null,
        decimal? temperatureC = null,
        int? heartRateBpm = null,
        int? respiratoryRateBpm = null,
        string? note = null)
    {
        if (weightKg is <= 0 or > 500)
        {
            // A transposed digit here becomes a drug dose. The bound is generous enough for any
            // patient a small-animal clinic sees and tight enough to catch 45 typed as 450.
            throw new DomainException("invalid_vitals", "น้ำหนักไม่อยู่ในช่วงที่รับได้");
        }

        if (temperatureC is < 20 or > 46)
        {
            throw new DomainException("invalid_vitals", "อุณหภูมิไม่อยู่ในช่วงที่รับได้");
        }

        return new VitalsReading
        {
            Id = EntityId.New(EntityId.Vitals),
            ConsultationId = consultationId,
            PetId = petId,
            RecordedByStaffId = recordedByStaffId,
            RecordedAt = recordedAt,
            WeightKg = weightKg,
            TemperatureC = temperatureC,
            HeartRateBpm = heartRateBpm,
            RespiratoryRateBpm = respiratoryRateBpm,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
    }
}
