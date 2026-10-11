using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.Files;

/// <summary>
/// <c>DELETE /staff/lab-requests/{id}/attachments/{attachmentId}</c> — <b>not in the API
/// reference</b> (<c>docs/contract-extensions.md § 54</c>).
/// </summary>
public sealed record RemoveLabFileCommand(string LabRequestId, string AttachmentId, uint? IfMatchVersion = null);

/// <summary>
/// Takes a file off a request: the wrong report, the wrong patient. The row goes; the file stays in
/// the storage, which has no delete, and the audit row (<c>lab.file_removed</c>) names its key, so
/// a removal can be traced and undone by hand. <c>lab:write</c>, and the request's branch as always.
/// </summary>
public sealed class RemoveLabFileHandler(
    LabRequestAccess access,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<LabRequestDto> HandleAsync(RemoveLabFileCommand command, CancellationToken cancellationToken = default)
    {
        LabRequest? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var request = await access.ForStaffAsync(command.LabRequestId, token);
            ConcurrencyGuard.Ensure(request, command.IfMatchVersion);

            var removed = request.RemoveAttachment(command.AttachmentId, clock.UtcNow)
                ?? throw new NotFoundException("lab_attachment", command.AttachmentId);

            audit.Record(new AuditEvent(
                AuditActions.LabFileRemoved,
                AuditResources.LabRequest,
                request.Id,
                request.PetId,
                request.BranchId,
                new Dictionary<string, object?>
                {
                    ["attachment_id"] = removed.Id,
                    ["file_name"] = removed.FileName,
                    ["content_type"] = removed.ContentType,
                    ["file"] = LabFile.ValueOf(removed.StorageKey),
                }));

            updated = request;
        }, cancellationToken);

        return LabRequestDto.From(updated!);
    }
}
