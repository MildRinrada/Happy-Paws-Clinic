using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Laboratory.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.Files;

/// <summary>
/// <c>POST /staff/lab-requests/{id}/attachments</c> — <b>not in the API reference</b>
/// (<c>docs/contract-extensions.md § 54</c>).
/// </summary>
/// <param name="LabRequestId">From the path.</param>
/// <param name="File">The <c>value</c> the upload request answered: <c>storage:lab-files/…</c>.</param>
/// <param name="FileName">The name to show. Never used as a path.</param>
/// <param name="SizeBytes">The size the upload was asked for, to show.</param>
/// <param name="IfMatchVersion">The request's version, when the caller sends one.</param>
public sealed record AttachLabFileCommand(
    string LabRequestId,
    string? File,
    string? FileName,
    long? SizeBytes = null,
    uint? IfMatchVersion = null);

public sealed class AttachLabFileValidator : AbstractValidator<AttachLabFileCommand>
{
    public AttachLabFileValidator()
    {
        RuleFor(command => command.File)
            .Must(value => LabFile.KeyOf(value) is not null)
            .WithMessage("ค่า file ต้องเป็นค่าที่ได้จากการขออัปโหลดไฟล์");

        RuleFor(command => command.FileName).NotEmpty().MaximumLength(LabFile.MaxFileNameLength);

        RuleFor(command => command.SizeBytes)
            .InclusiveBetween(1, LabFile.MaxBytes)
            .When(command => command.SizeBytes is not null)
            .WithMessage("ไฟล์ต้องมีขนาดไม่เกิน 10 MB");
    }
}

/// <summary>
/// Puts an uploaded file on a request. The key must have been made for this request
/// (<see cref="LabFile.RequestOf"/>), and the file must have arrived: the storage is asked first,
/// as § 32 does for a photo, so a request never lists a file that is not there.
/// <para>
/// <c>lab:write</c>, the scope of whoever enters what the lab sent. Attaching does not change
/// whether the results have been read; the values are what a vet reads, and they are entered as
/// values. Audited as <c>lab.file_attached</c> with the name, type, size and key.
/// </para>
/// </summary>
public sealed class AttachLabFileHandler(
    LabRequestAccess access,
    IFileStorage storage,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<LabRequestDto> HandleAsync(AttachLabFileCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var key = LabFile.KeyOf(command.File)!;
        LabRequest? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var request = await access.ForStaffAsync(command.LabRequestId, token);
            ConcurrencyGuard.Ensure(request, command.IfMatchVersion);

            // The key is checked against this request before the storage is asked anything, so a
            // caller learns nothing about files uploaded for other requests.
            if (LabFile.RequestOf(key) != request.Id)
            {
                throw new DomainException("invalid_lab_attachment", "ไฟล์นี้ไม่ได้อัปโหลดให้คำขอตรวจนี้");
            }

            if (!await storage.ExistsAsync(key, token))
            {
                throw new DomainException("invalid_lab_attachment", "ไฟล์ยังส่งไม่ถึงที่เก็บไฟล์ ส่งไฟล์ให้เสร็จก่อนแนบ");
            }

            var attachment = request.Attach(key, command.FileName!, command.SizeBytes, staffId, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.LabFileAttached,
                AuditResources.LabRequest,
                request.Id,
                request.PetId,
                request.BranchId,
                new Dictionary<string, object?>
                {
                    ["attachment_id"] = attachment.Id,
                    ["file_name"] = attachment.FileName,
                    ["content_type"] = attachment.ContentType,
                    ["size_bytes"] = attachment.SizeBytes,
                    ["file"] = LabFile.ValueOf(attachment.StorageKey),
                }));

            updated = request;
        }, cancellationToken);

        return LabRequestDto.From(updated!);
    }
}
