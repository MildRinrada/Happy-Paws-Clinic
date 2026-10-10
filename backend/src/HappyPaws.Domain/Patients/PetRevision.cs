namespace HappyPaws.Domain.Patients;

/// <summary>
/// What a member of staff changes on a patient's identity card — <c>PATCH /staff/pets/{id}</c>.
/// Not in the API reference; see <c>docs/contract-extensions.md § 28</c>.
/// <para>
/// A null property leaves that field alone. Text fields are cleared with an empty string, as on the
/// owner's own edit; the two fields that are not text are cleared with their <c>Clear…</c> flag,
/// because "no birth date" and "not known whether neutered" have no empty value of their own.
/// </para>
/// </summary>
public sealed record PetRevision(
    string? Name = null,
    string? Species = null,
    string? Breed = null,
    string? MicrochipNumber = null,
    PetSex? Sex = null,
    DateOnly? BirthDate = null,
    bool? BirthDateEstimated = null,
    bool ClearBirthDate = false,
    bool? Neutered = null,
    bool ClearNeutered = false);
