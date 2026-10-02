using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients;

/// <summary>Persistence port for treatment plans.</summary>
public interface ITreatmentPlanRepository
{
    void Add(TreatmentPlan plan);

    /// <summary>A patient's plans, newest first.</summary>
    Task<IReadOnlyList<TreatmentPlan>> ForPetAsync(string petId, CancellationToken cancellationToken = default);
}
