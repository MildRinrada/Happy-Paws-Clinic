using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Documents;

/// <summary>
/// The part of attaching a file that is the same for the clinic and the owner: record the document,
/// audit it, and issue the URL its bytes go to. The two endpoints differ only in who may call them
/// and what they may set.
/// </summary>
public sealed class DocumentUploads(IPetDocumentRepository documents, IFileStorage storage, IAuditTrail audit)
{
    /// <summary>Time to pick a file and send it on a slow phone connection.</summary>
    public static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Must be called inside the caller's transaction.</summary>
    public DocumentUploadDto Start(PetDocument document)
    {
        documents.Add(document);

        audit.Record(new AuditEvent(
            AuditActions.DocumentAdded,
            AuditResources.Pet,
            document.PetId,
            document.PetId,
            Details: new Dictionary<string, object?>
            {
                ["document_id"] = document.Id,
                ["source"] = document.Source.ToString().ToLowerInvariant(),
                ["kind"] = document.Kind,
                ["visible_to_owner"] = document.VisibleToOwner,
            }));

        var ticket = storage.CreateUpload(document.StorageKey, document.ContentType, document.DeclaredBytes, UploadLifetime);

        return new DocumentUploadDto(PetDocumentDto.From(document), UploadTicketDto.From(ticket));
    }
}
