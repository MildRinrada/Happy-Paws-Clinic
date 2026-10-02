using HappyPaws.Application.Patients;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Patients;

internal sealed class TreatmentPlanRepository(HappyPawsDbContext db) : ITreatmentPlanRepository
{
    public void Add(TreatmentPlan plan) => db.Set<TreatmentPlan>().Add(plan);

    public async Task<IReadOnlyList<TreatmentPlan>> ForPetAsync(string petId, CancellationToken cancellationToken = default) =>
        await db.Set<TreatmentPlan>()
            .AsNoTracking()
            .Where(plan => plan.PetId == petId)
            .OrderByDescending(plan => plan.CreatedAt)
            .ToListAsync(cancellationToken);
}
