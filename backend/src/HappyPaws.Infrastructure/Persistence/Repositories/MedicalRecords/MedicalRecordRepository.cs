using HappyPaws.Application.MedicalRecords;
using HappyPaws.Domain.MedicalRecords;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.MedicalRecords;

/// <summary>
/// EF Core implementation of the signed-record port. Amendments always travel with the record —
/// a record read without its corrections is a document that says something the clinic no longer
/// says.
/// </summary>
internal sealed class MedicalRecordRepository(HappyPawsDbContext db) : IMedicalRecordRepository
{
    public void Add(MedicalRecord record) => db.Set<MedicalRecord>().Add(record);

    public Task<MedicalRecord?> FindByIdAsync(string recordId, CancellationToken cancellationToken = default) =>
        db.Set<MedicalRecord>()
            .Include(record => record.Amendments)
            .FirstOrDefaultAsync(record => record.Id == recordId, cancellationToken);

    public Task<MedicalRecord?> FindByConsultationAsync(
        string consultationId,
        CancellationToken cancellationToken = default) =>
        db.Set<MedicalRecord>()
            .Include(record => record.Amendments)
            .FirstOrDefaultAsync(record => record.ConsultationId == consultationId, cancellationToken);

    public async Task<IReadOnlyList<MedicalRecord>> ForPetAsync(string petId, int take, CancellationToken cancellationToken = default) =>
        await db.Set<MedicalRecord>()
            .AsNoTracking()
            .Include(record => record.Amendments)
            .Where(record => record.PetId == petId)
            .OrderByDescending(record => record.SignedAt)
            .Take(take)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
}
