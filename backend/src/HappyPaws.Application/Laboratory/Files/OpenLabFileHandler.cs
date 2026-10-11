using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Laboratory.Files;

/// <summary>
/// <c>GET /staff/lab-requests/{id}/attachments/{attachmentId}/link</c> — <b>not in the API
/// reference</b> (<c>docs/contract-extensions.md § 54</c>).
/// </summary>
public sealed record OpenLabFileCommand(string LabRequestId, string AttachmentId);

/// <summary>
/// A link to open one file, made at the moment it is asked for and good for five minutes.
/// <para>
/// Lab files are staff-only, so no list carries a working address: whoever wants to see one asks
/// here, under <c>lab:read</c> and the request's branch, and the asking is written to the access log
/// (<c>pet.lab_file_opened</c>) like any other look at a patient's results. A PDF's link carries its
/// name, so the storage serves it as a download (<c>Content-Disposition: attachment</c>) rather than
/// in the browser's viewer; pictures open in place. Every file is served with the type its upload
/// URL allowed and <c>X-Content-Type-Options: nosniff</c>.
/// </para>
/// </summary>
public sealed class OpenLabFileHandler(
    LabRequestAccess access,
    IFileStorage storage,
    IAuditTrail audit,
    IClock clock)
{
    public async Task<LabFileLinkDto> HandleAsync(OpenLabFileCommand command, CancellationToken cancellationToken = default)
    {
        var request = await access.ForStaffAsync(command.LabRequestId, cancellationToken);
        var attachment = request.Attachments.FirstOrDefault(item => item.Id == command.AttachmentId)
            ?? throw new NotFoundException("lab_attachment", command.AttachmentId);

        var saveAs = attachment.ContentType == "application/pdf" ? PdfName(attachment.FileName) : null;
        var url = storage.CreateDownloadUrl(attachment.StorageKey, LabFileLinks.OpenLifetime, saveAs);

        await audit.RecordImmediatelyAsync(
            new AuditEvent(
                AuditActions.LabFileOpened,
                AuditResources.LabRequest,
                request.Id,
                request.PetId,
                request.BranchId,
                new Dictionary<string, object?> { ["attachment_id"] = attachment.Id, ["file_name"] = attachment.FileName }),
            cancellationToken);

        return new LabFileLinkDto(url, clock.UtcNow + LabFileLinks.OpenLifetime);
    }

    /// <summary>The uploader's name, ending in <c>.pdf</c> whatever it ended in before.</summary>
    private static string PdfName(string fileName) =>
        fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? fileName : $"{Path.GetFileNameWithoutExtension(fileName)}.pdf";
}
