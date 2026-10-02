using HappyPaws.Application.Consultations.RecordExamination;

namespace HappyPaws.Api.Contracts.Consultations;

/// <summary>
/// Request bodies for the consultation endpoints.
/// <para>
/// As with booking and the queue, the reference specifies each endpoint's behaviour but not its
/// schema. Field names follow the vocabulary the document itself uses — "อาการที่แจ้ง",
/// "ผลตรวจรายระบบ ปกติ/ผิดปกติ", "วินิจฉัยหลัก/รอง". See <c>docs/assumptions.md § 15</c>.
/// </para>
/// </summary>
public sealed record OpenConsultationRequest(
    string? AppointmentId = null,
    string? QueueTicketId = null,
    string? ChiefComplaint = null);

/// <summary>
/// <c>PATCH /staff/consultations/{id}</c>. Every field is optional: an autosave sends what the vet
/// has touched, and an omitted field is left exactly as it was.
/// </summary>
public sealed record UpdateConsultationRequest(
    string? ChiefComplaint = null,
    string? Subjective = null,
    string? Objective = null,
    string? Assessment = null,
    string? Plan = null);

public sealed record RecordVitalsRequest(
    decimal? WeightKg = null,
    decimal? TemperatureC = null,
    int? HeartRateBpm = null,
    int? RespiratoryRateBpm = null,
    string? Note = null);

/// <summary>
/// <c>POST /staff/consultations/{id}/examination</c> — the whole form. A body system omitted from
/// <c>findings</c> was not examined, which is not the same as normal.
/// </summary>
public sealed record RecordExaminationRequest(IReadOnlyList<ExaminationEntry> Findings);

public sealed record AddDiagnosisRequest(
    string Description,
    bool IsPrimary = false,
    string? Code = null,
    string? Note = null);

public sealed record AddTreatmentRequest(
    string Name,
    int Quantity,
    long UnitPriceSatang,
    string? ServiceId = null,
    string? Note = null);

/// <summary>
/// <c>POST /staff/consultations/{id}/prescriptions</c>.
/// <para>
/// <c>acknowledge_allergy</c> is sent only after a first attempt was refused with
/// <c>409 allergy_conflict</c>. Its value is the prescriber's reason, and it is stored on the
/// prescription and written to the audit log.
/// </para>
/// </summary>
public sealed record AddPrescriptionRequest(
    string DrugName,
    string Dose,
    string Frequency,
    int DurationDays,
    int Quantity,
    string? Instructions = null,
    string? AcknowledgeAllergy = null);

/// <summary>
/// <c>POST /staff/consultations/{id}/close</c>. <c>follow_up</c> is optional — "สร้างนัดติดตาม"
/// happens only when the vet asked for one.
/// </summary>
public sealed record CloseConsultationRequest(FollowUpBody? FollowUp = null);

/// <summary>The follow-up visit to book on the way out. Omitted fields repeat this visit's.</summary>
public sealed record FollowUpBody(DateTimeOffset StartsAt, string? ServiceId = null, string? StaffId = null);
