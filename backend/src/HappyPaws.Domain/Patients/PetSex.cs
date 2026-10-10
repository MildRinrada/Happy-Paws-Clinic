namespace HappyPaws.Domain.Patients;

/// <summary>
/// The patient's sex as the clinic recorded it. <see cref="Unknown"/> is the default and a real
/// answer: a stray brought in at night, or a young animal nobody has checked yet. Not in the API
/// reference; see <c>docs/contract-extensions.md § 28</c>.
/// </summary>
public enum PetSex
{
    Unknown,
    Male,
    Female,
}
