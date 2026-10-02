using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Documents;

/// <summary>Persistence port for files on a patient's record.</summary>
public interface IPetDocumentRepository
{
    void Add(PetDocument document);

    Task<PetDocument?> FindByIdAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>Tracked, for marking the file as arrived.</summary>
    Task<PetDocument?> FindByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>A patient's uploaded documents, newest first; only those the owner may see when asked.</summary>
    Task<IReadOnlyList<PetDocument>> ForPetAsync(
        string petId,
        bool ownerVisibleOnly,
        CancellationToken cancellationToken = default);
}
