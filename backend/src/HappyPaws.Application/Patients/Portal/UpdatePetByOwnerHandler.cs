using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Portal;

/// <summary>
/// <c>PATCH /portal/pets/{id}</c> — "แก้ได้เฉพาะรูป ชื่อเล่น อาหาร พฤติกรรม". Send only what
/// changes; an empty string clears a field.
/// </summary>
public sealed record UpdatePetByOwnerCommand(
    string PetId,
    string? Nickname = null,
    string? PhotoDocumentId = null,
    string? Diet = null,
    string? Behaviour = null,
    uint? IfMatchVersion = null);

/// <summary>
/// The owner's edit of their own pet.
/// <para>
/// <b>The four fields are all there is.</b> The command has no name, species, chip or anything
/// clinical in it, so a request carrying them has nowhere to put them: the clinical record is out of
/// reach by construction, not by a check someone could forget.
/// </para>
/// <para>
/// The photo is a document already on this pet's record — uploaded with
/// <c>POST /portal/documents</c> — and must be an image that has arrived. A document from another
/// pet, a PDF, or one still waiting for its file is refused on <c>photo_document_id</c>.
/// </para>
/// </summary>
public sealed class UpdatePetByOwnerHandler(
    IPetRepository pets,
    IPetDocumentRepository documents,
    PetAccessGuard petAccess,
    MyPetReader reader,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<MyPetDto> HandleAsync(UpdatePetByOwnerCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();
        await petAccess.EnsureCaresForAsync(command.PetId, cancellationToken);

        Pet? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pet = await pets.FindTrackedByIdAsync(command.PetId, token)
                ?? throw new NotFoundException("pet", command.PetId);

            ConcurrencyGuard.Ensure(pet, command.IfMatchVersion);

            if (command.PhotoDocumentId is { Length: > 0 } photoId)
            {
                await EnsurePhotoAsync(pet.Id, photoId, token);
            }

            pet.UpdateOwnerProfile(command.Nickname, command.PhotoDocumentId, command.Diet, command.Behaviour);
            updated = pet;
        }, cancellationToken);

        return await reader.ReadAsync(updated!, customerId, cancellationToken);
    }

    private async Task EnsurePhotoAsync(string petId, string documentId, CancellationToken cancellationToken)
    {
        var document = await documents.FindByIdAsync(documentId, cancellationToken);

        if (document is not { IsUploaded: true } || document.PetId != petId || !document.ContentType.StartsWith("image/", StringComparison.Ordinal))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["photo_document_id"] = ["รูปต้องเป็นไฟล์รูปภาพของสัตว์ตัวนี้ที่อัปโหลดเสร็จแล้ว"],
            });
        }
    }
}
