using HappyPaws.Application.Patients;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Patients;

internal sealed class VaccinationRepository(HappyPawsDbContext db) : IVaccinationRepository
{
    public void Add(Vaccination vaccination) => db.Set<Vaccination>().Add(vaccination);

    public async Task<IReadOnlyList<Vaccination>> ForPetAsync(
        string petId,
        CancellationToken cancellationToken = default) =>
        await db.Set<Vaccination>()
            .AsNoTracking()
            .Where(dose => dose.PetId == petId)
            .OrderByDescending(dose => dose.AdministeredAt).ThenByDescending(dose => dose.Id)
            .ToListAsync(cancellationToken);
}
