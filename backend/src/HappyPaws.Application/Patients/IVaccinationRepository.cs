using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients;

/// <summary>Persistence port for the doses a patient has been given.</summary>
public interface IVaccinationRepository
{
    void Add(Vaccination vaccination);

    /// <summary>Every dose for one patient, newest first. A lifetime of vaccinations is a short list.</summary>
    Task<IReadOnlyList<Vaccination>> ForPetAsync(string petId, CancellationToken cancellationToken = default);
}
