using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients;

/// <summary>
/// Persistence port for recorded allergies — the table the prescription check reads.
/// </summary>
public interface IPetAllergyRepository
{
    void Add(PetAllergy allergy);

    /// <summary>Allergies still believed to apply. Withdrawn entries are kept but not returned.</summary>
    Task<IReadOnlyList<PetAllergy>> ActiveForPetAsync(string petId, CancellationToken cancellationToken = default);
}
