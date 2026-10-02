using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Patients;

internal sealed class PetDocumentRepository(HappyPawsDbContext db) : IPetDocumentRepository
{
    public void Add(PetDocument document) => db.Set<PetDocument>().Add(document);

    public Task<PetDocument?> FindByIdAsync(string documentId, CancellationToken cancellationToken = default) =>
        db.Set<PetDocument>().AsNoTracking().FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken);

    public Task<PetDocument?> FindByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default) =>
        db.Set<PetDocument>().FirstOrDefaultAsync(document => document.StorageKey == storageKey, cancellationToken);

    public async Task<IReadOnlyList<PetDocument>> ForPetAsync(
        string petId,
        bool ownerVisibleOnly,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<PetDocument>()
            .AsNoTracking()
            .Where(document => document.PetId == petId && document.UploadedAt != null);

        if (ownerVisibleOnly)
        {
            query = query.Where(document => document.VisibleToOwner);
        }

        return await query.OrderByDescending(document => document.CreatedAt).ToListAsync(cancellationToken);
    }
}
