using HappyPaws.Application.Patients;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Patients;

/// <summary>
/// EF Core implementation of the allergy port. Read on every prescription, so it is deliberately
/// one indexed lookup and nothing else.
/// </summary>
internal sealed class PetAllergyRepository(HappyPawsDbContext db) : IPetAllergyRepository
{
    public void Add(PetAllergy allergy) => db.Set<PetAllergy>().Add(allergy);

    public async Task<IReadOnlyList<PetAllergy>> ActiveForPetAsync(
        string petId,
        CancellationToken cancellationToken = default) =>
        await db.Set<PetAllergy>()
            .AsNoTracking()
            .Where(allergy => allergy.PetId == petId && allergy.WithdrawnAt == null)
            .ToListAsync(cancellationToken);
}
