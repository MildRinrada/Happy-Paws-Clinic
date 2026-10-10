namespace HappyPaws.Application.StaffAi;

/// <summary>
/// The reads the staff AI features need that no existing use case serves in one piece: a patient's
/// whole file in compact form, and the people whose details must be masked before anything goes to
/// the model.
/// <para>
/// A projection port like <c>IPetTimelineReader</c>: it answers a question about a patient rather
/// than loading aggregates, and each list is capped so a patient with years of visits still costs a
/// handful of indexed reads and a prompt of bounded size.
/// </para>
/// </summary>
public interface IStaffAiReader
{
    /// <summary>The patient's file for a summary, or null when there is no such pet.</summary>
    Task<PetFile?> PetFileAsync(string petId, DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>A customer's own personal details, for masking. Never sent anywhere.</summary>
    Task<PersonDetails?> CustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>Every staff member's display name, for masking names in reports and in conversation history.</summary>
    Task<IReadOnlyList<string>> StaffNamesAsync(CancellationToken cancellationToken = default);
}

/// <summary>How many of each kind of record a summary is built from: the newest ones.</summary>
public static class PetFileLimits
{
    public const int Visits = 12;
    public const int Medicines = 20;
    public const int Weights = 12;
    public const int LabRequests = 10;
    public const int Vaccinations = 6;
}

/// <summary>A person's details as stored, used only to know what to mask.</summary>
public sealed record PersonDetails(string Name, string? Phone, string? Email);

/// <summary>Who the patient is. No hospital number, microchip or owner id: none of it helps a summary.</summary>
public sealed record PetFacts(
    string Id,
    string Name,
    string Species,
    string? Breed,
    string Sex,
    bool? Neutered,
    DateOnly? BirthDate,
    bool BirthDateEstimated,
    bool IsDeceased);

public sealed record AllergyFact(string Id, DateOnly RecordedOn, string Substance, string Severity, string? Reaction);

public sealed record DiagnosisFact(string Description, bool IsPrimary);

public sealed record VisitFact(
    string Id,
    DateOnly OpenedOn,
    string? ChiefComplaint,
    string? Assessment,
    IReadOnlyList<DiagnosisFact> Diagnoses);

public sealed record MedicineFact(
    string Id,
    DateOnly PrescribedOn,
    string DrugName,
    string Dose,
    string Frequency,
    int DurationDays,
    string? Instructions);

/// <summary>A weight reading. <c>ConsultationId</c> is the visit that measured it, which a citation links to.</summary>
public sealed record WeightFact(string ConsultationId, DateOnly RecordedOn, decimal WeightKg);

public sealed record LabValueFact(
    string Analyte,
    string Value,
    string? Unit,
    string? Reference,
    string Flag);

/// <summary>
/// A lab request. <c>AbnormalValues</c> holds only the results flagged low, high or abnormal;
/// <c>ResultCount</c> counts every result, normal ones included.
/// </summary>
public sealed record LabFact(
    string Id,
    DateOnly On,
    IReadOnlyList<string> Tests,
    int ResultCount,
    IReadOnlyList<LabValueFact> AbnormalValues);

public sealed record VaccinationFact(string Id, DateOnly GivenOn, string VaccineName, DateOnly? NextDueOn);

/// <summary>
/// The patient's file, newest first in every list, capped by <see cref="PetFileLimits"/>.
/// <see cref="People"/> are the owner and carers: their details are masked if they turn up in a
/// note, and are not themselves part of the prompt.
/// </summary>
public sealed record PetFile(
    PetFacts Pet,
    IReadOnlyList<PersonDetails> People,
    IReadOnlyList<AllergyFact> Allergies,
    IReadOnlyList<VisitFact> Visits,
    IReadOnlyList<MedicineFact> Medicines,
    IReadOnlyList<WeightFact> Weights,
    IReadOnlyList<LabFact> Labs,
    IReadOnlyList<VaccinationFact> Vaccinations);
