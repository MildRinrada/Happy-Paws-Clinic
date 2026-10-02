using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations.Dtos;

/// <summary>An open or closed case as the API returns it — <c>con_771</c>.</summary>
public sealed record ConsultationDto(
    string Id,
    uint Version,
    string Status,
    string BranchId,
    string PetId,
    string CustomerId,
    string VetStaffId,
    string? AppointmentId,
    string? QueueTicketId,
    string? MedicalRecordId,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    string? ChiefComplaint,
    string? Subjective,
    string? Objective,
    string? Assessment,
    string? Plan,
    IReadOnlyList<VitalsDto> Vitals,
    IReadOnlyList<FindingDto> Examination,
    IReadOnlyList<DiagnosisDto> Diagnoses,
    IReadOnlyList<TreatmentDto> Treatments,
    IReadOnlyList<PrescriptionDto> Prescriptions,
    long TreatmentChargesSatang)
{
    public static ConsultationDto From(Consultation consultation) =>
        new(
            consultation.Id,
            consultation.Version,
            consultation.Status == ConsultationStatus.Draft ? "draft" : "closed",
            consultation.BranchId,
            consultation.PetId,
            consultation.CustomerId,
            consultation.VetStaffId,
            consultation.AppointmentId,
            consultation.QueueTicketId,
            consultation.MedicalRecordId,
            consultation.OpenedAt,
            consultation.ClosedAt,
            consultation.ChiefComplaint,
            consultation.Subjective,
            consultation.Objective,
            consultation.Assessment,
            consultation.Plan,
            [.. consultation.Vitals.OrderBy(v => v.RecordedAt).Select(VitalsDto.From)],
            [.. consultation.Findings.Select(FindingDto.From)],
            [.. consultation.Diagnoses.OrderByDescending(d => d.IsPrimary).Select(DiagnosisDto.From)],
            [.. consultation.Treatments.OrderBy(t => t.PerformedAt).Select(TreatmentDto.From)],
            [.. consultation.Prescriptions.OrderBy(p => p.PrescribedAt).Select(PrescriptionDto.From)],
            consultation.TreatmentCharges.Satang);
}

public sealed record VitalsDto(
    string Id,
    DateTimeOffset RecordedAt,
    string RecordedByStaffId,
    decimal? WeightKg,
    decimal? TemperatureC,
    int? HeartRateBpm,
    int? RespiratoryRateBpm,
    string? Note)
{
    public static VitalsDto From(VitalsReading reading) =>
        new(
            reading.Id,
            reading.RecordedAt,
            reading.RecordedByStaffId,
            reading.WeightKg,
            reading.TemperatureC,
            reading.HeartRateBpm,
            reading.RespiratoryRateBpm,
            reading.Note);
}

public sealed record FindingDto(string Id, string BodySystem, bool IsNormal, string? Note)
{
    public static FindingDto From(ExaminationFinding finding) =>
        new(finding.Id, finding.BodySystem, finding.IsNormal, finding.Note);
}

public sealed record DiagnosisDto(string Id, string? Code, string Description, bool IsPrimary, string? Note)
{
    public static DiagnosisDto From(Diagnosis diagnosis) =>
        new(diagnosis.Id, diagnosis.Code, diagnosis.Description, diagnosis.IsPrimary, diagnosis.Note);
}

public sealed record TreatmentDto(
    string Id,
    string? ServiceId,
    string Name,
    int Quantity,
    long UnitPriceSatang,
    long LineTotalSatang,
    string PerformedByStaffId,
    DateTimeOffset PerformedAt,
    string? Note)
{
    public static TreatmentDto From(Treatment treatment) =>
        new(
            treatment.Id,
            treatment.ServiceId,
            treatment.Name,
            treatment.Quantity,
            treatment.UnitPriceSatang,
            treatment.LineTotal.Satang,
            treatment.PerformedByStaffId,
            treatment.PerformedAt,
            treatment.Note);
}

public sealed record PrescriptionDto(
    string Id,
    string DrugName,
    string Dose,
    string Frequency,
    int DurationDays,
    int Quantity,
    string? Instructions,
    string? AllergyAcknowledgement,
    DateTimeOffset PrescribedAt,
    string PrescribedByStaffId,
    DateTimeOffset? DispensedAt)
{
    public static PrescriptionDto From(Domain.Prescriptions.Prescription prescription) =>
        new(
            prescription.Id,
            prescription.DrugName,
            prescription.Dose,
            prescription.Frequency,
            prescription.DurationDays,
            prescription.Quantity,
            prescription.Instructions,
            prescription.AllergyAcknowledgement,
            prescription.PrescribedAt,
            prescription.PrescribedByStaffId,
            prescription.DispensedAt);
}
