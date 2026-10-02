namespace HappyPaws.Application.MedicalRecords.GetOwnerVisibleRecords;

/// <summary>
/// <c>GET /portal/pets/{id}/records</c> — "เฉพาะส่วนที่คลินิกตั้งค่าให้เจ้าของเห็น".
/// <para>
/// <c>visible_sections</c> is what the clinic has opened; every section not in it is absent from the
/// answer, not empty. Absent and empty mean different things to an owner: "nothing was prescribed"
/// is not the same as "the clinic does not show prescriptions here".
/// </para>
/// </summary>
public sealed record OwnerRecordsDto(
    string PetId,
    IReadOnlyList<string> VisibleSections,
    IReadOnlyList<OwnerVisitDto> Visits,
    IReadOnlyList<OwnerLabResultDto>? LabResults,
    IReadOnlyList<OwnerAttachmentDto>? Attachments);

/// <summary>One signed visit, cut down to the sections the clinic shows owners.</summary>
public sealed record OwnerVisitDto(
    string RecordId,
    DateTimeOffset VisitedAt,
    DateTimeOffset SignedAt,
    IReadOnlyList<OwnerVitalsDto>? Vitals = null,
    IReadOnlyList<OwnerFindingDto>? Examination = null,
    IReadOnlyList<string>? Diagnoses = null,
    IReadOnlyList<OwnerTreatmentDto>? Treatments = null,
    IReadOnlyList<OwnerPrescriptionDto>? Prescriptions = null);

public sealed record OwnerVitalsDto(DateTimeOffset RecordedAt, decimal? WeightKg, decimal? TemperatureC, int? HeartRateBpm, int? RespiratoryRateBpm);

public sealed record OwnerFindingDto(string BodySystem, bool IsNormal, string? Note);

/// <summary>A procedure, without its price: the bill is where the owner reads what it cost.</summary>
public sealed record OwnerTreatmentDto(string Name, int Quantity);

public sealed record OwnerPrescriptionDto(string DrugName, string Dose, string Frequency, int DurationDays, string? Instructions);

/// <summary>A lab request's results, shown once a vet has read them.</summary>
public sealed record OwnerLabResultDto(
    string LabRequestId,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ReviewedAt,
    IReadOnlyList<string> Tests,
    IReadOnlyList<OwnerLabValueDto> Values);

public sealed record OwnerLabValueDto(string Analyte, decimal? Value, string? ValueText, string? Unit, string? Flag);

/// <summary>A file the owner may open, with a short-lived link.</summary>
public sealed record OwnerAttachmentDto(
    string DocumentId,
    string Title,
    string Kind,
    string Source,
    string ContentType,
    DateTimeOffset AddedAt,
    string? DownloadUrl);
