using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.Photos;

/// <summary><c>POST /staff/members/{id}/public-profile/photo</c>. Extension, § 32.</summary>
/// <param name="StaffId">From the path.</param>
/// <param name="ContentType"><c>image/jpeg</c>, <c>image/png</c> or <c>image/webp</c>.</param>
/// <param name="SizeBytes">The file's size; the URL accepts no more.</param>
public sealed record RequestStaffPhotoCommand(string StaffId, string? ContentType, long SizeBytes);

/// <summary>
/// The new photo's value for the profile, and where to send its file.
/// </summary>
/// <param name="PhotoPath">What to put in <c>photo_path</c> on the next <c>PUT</c> of the profile, once the file is in.</param>
/// <param name="Upload">The signed URL to <c>PUT</c> the file to.</param>
public sealed record StaffPhotoUploadDto(string PhotoPath, UploadTicketDto Upload);

public sealed class RequestStaffPhotoValidator : AbstractValidator<RequestStaffPhotoCommand>
{
    public RequestStaffPhotoValidator()
    {
        RuleFor(command => command.ContentType)
            .Must(type => type is not null && StaffPhoto.Extensions.ContainsKey(type))
            .WithMessage("รูปต้องเป็นไฟล์ JPEG, PNG หรือ WebP");

        RuleFor(command => command.SizeBytes)
            .InclusiveBetween(1, StaffPhotoLinks.MaxBytes)
            .WithMessage("รูปต้องมีขนาดไม่เกิน 5 MB");
    }
}

/// <summary>
/// Hands out a signed URL for a member's new photo. Nothing about the profile changes here: the
/// photo goes on the website only when a <c>PUT</c> of the profile names it, after its file has
/// arrived, so a photo picked and then abandoned is never shown.
/// <para>
/// Held to the same rules as editing the profile (<see cref="StaffMemberAccess"/>): the caller's
/// own branches, and nobody who holds more than they do. Audited as
/// <c>staff.public_profile_updated</c> with a <c>photo_upload</c> entry, the way an article's cover
/// request is <c>article.updated</c>, so the log says who sent a picture of this person to the
/// storage even if it is never published.
/// </para>
/// </summary>
public sealed class RequestStaffPhotoHandler(
    StaffMemberAccess access,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    RequestValidator validator)
{
    public async Task<StaffPhotoUploadDto> HandleAsync(RequestStaffPhotoCommand command, CancellationToken cancellationToken = default)
    {
        command = command with { ContentType = command.ContentType?.Split(';')[0].Trim().ToLowerInvariant() };
        await validator.ValidateAsync(command, cancellationToken);

        string? key = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var (membership, _) = await access.ForChangeAsync(command.StaffId, token);

            key = StaffPhoto.NewKey(membership.Id, command.ContentType!);

            audit.Record(new AuditEvent(
                AuditActions.StaffPublicProfileUpdated,
                AuditResources.StaffMember,
                membership.Id,
                Details: new Dictionary<string, object?>
                {
                    ["photo_upload"] = new Dictionary<string, object?>
                    {
                        ["content_type"] = command.ContentType,
                        ["size_bytes"] = command.SizeBytes,
                        ["photo_path"] = StaffPhoto.PhotoPathOf(key),
                    },
                }));
        }, cancellationToken);

        var ticket = storage.CreateUpload(key!, command.ContentType!, command.SizeBytes, StaffPhotoLinks.UploadLifetime);

        return new StaffPhotoUploadDto(StaffPhoto.PhotoPathOf(key!), UploadTicketDto.From(ticket));
    }
}
