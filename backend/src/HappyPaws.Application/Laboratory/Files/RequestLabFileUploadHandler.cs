using FluentValidation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.Files;

/// <summary>
/// <c>POST /staff/lab-requests/{id}/attachments/uploads</c> — <b>not in the API reference</b>
/// (<c>docs/contract-extensions.md § 54</c>).
/// </summary>
/// <param name="LabRequestId">From the path.</param>
/// <param name="ContentType"><c>application/pdf</c>, <c>image/jpeg</c>, <c>image/png</c> or <c>image/webp</c>.</param>
/// <param name="SizeBytes">The file's size; the URL accepts no more.</param>
public sealed record RequestLabFileUploadCommand(string LabRequestId, string? ContentType, long SizeBytes);

/// <summary>Where to send the file, and the value that names it once it is there.</summary>
/// <param name="Value">What to send as <c>file</c> to <c>POST /staff/lab-requests/{id}/attachments</c> once the file is in.</param>
/// <param name="Upload">The signed URL to <c>PUT</c> the file to.</param>
public sealed record LabFileUploadDto(string Value, UploadTicketDto Upload);

public sealed class RequestLabFileUploadValidator : AbstractValidator<RequestLabFileUploadCommand>
{
    public RequestLabFileUploadValidator()
    {
        RuleFor(command => command.ContentType)
            .Must(type => type is not null && LabFile.Extensions.ContainsKey(type))
            .WithMessage("ไฟล์ต้องเป็น PDF, JPEG, PNG หรือ WebP");

        RuleFor(command => command.SizeBytes)
            .InclusiveBetween(1, LabFile.MaxBytes)
            .WithMessage("ไฟล์ต้องมีขนาดไม่เกิน 10 MB");
    }
}

/// <summary>
/// Hands out a signed URL for a new file on a request, the § 32 way: nothing on the request changes
/// here. The file joins the request only when <c>POST .../attachments</c> names it after it has
/// arrived, so a file picked and abandoned is never listed. Not audited for the same reason; the
/// attaching is.
/// <para>
/// The key names this request and the declared type (<see cref="LabFile.NewKey"/>), and the URL
/// accepts that type and at most the declared size. A request at another branch is <c>404</c>.
/// </para>
/// </summary>
public sealed class RequestLabFileUploadHandler(
    LabRequestAccess access,
    IFileStorage storage,
    RequestValidator validator)
{
    public async Task<LabFileUploadDto> HandleAsync(RequestLabFileUploadCommand command, CancellationToken cancellationToken = default)
    {
        command = command with { ContentType = command.ContentType?.Split(';')[0].Trim().ToLowerInvariant() };
        await validator.ValidateAsync(command, cancellationToken);

        var request = await access.ForStaffAsync(command.LabRequestId, cancellationToken);
        var key = LabFile.NewKey(request.Id, command.ContentType!);
        var ticket = storage.CreateUpload(key, command.ContentType!, command.SizeBytes, LabFileLinks.UploadLifetime);

        return new LabFileUploadDto(LabFile.ValueOf(key), UploadTicketDto.From(ticket));
    }
}
