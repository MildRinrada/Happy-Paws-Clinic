using FluentValidation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.BrandImages;

/// <summary><c>POST /clinic/logo</c> and <c>POST /clinic/tab-icon</c>. Extension, § 50.</summary>
/// <param name="Kind">From the route: which picture.</param>
/// <param name="ContentType"><c>image/png</c>, <c>image/jpeg</c> or <c>image/webp</c>.</param>
/// <param name="SizeBytes">The file's size; the URL accepts no more.</param>
public sealed record RequestBrandImageCommand(ClinicBrandImageKind Kind, string? ContentType, long SizeBytes);

/// <summary>The new picture's value for the profile, and where to send its file.</summary>
/// <param name="Value">
/// What to put in <c>logo_url</c> or <c>tab_icon_url</c> on the next <c>PATCH /clinic</c>, once
/// the file is in.
/// </param>
/// <param name="Upload">The signed URL to <c>PUT</c> the file to.</param>
public sealed record BrandImageUploadDto(string Value, UploadTicketDto Upload);

public sealed class RequestBrandImageValidator : AbstractValidator<RequestBrandImageCommand>
{
    public RequestBrandImageValidator()
    {
        RuleFor(command => command.ContentType)
            .Must(type => type is not null && ClinicBrandImage.Extensions.ContainsKey(type))
            .WithMessage("รูปต้องเป็นไฟล์ PNG, JPEG หรือ WebP");

        RuleFor(command => command.SizeBytes)
            .InclusiveBetween(1, BrandImageLinks.MaxBytes)
            .WithMessage("รูปต้องมีขนาดไม่เกิน 5 MB");
    }
}

/// <summary>
/// Hands out a signed URL for a new logo or tab icon. Nothing about the profile changes here: the
/// picture goes on the website only when a <c>PATCH /clinic</c> names it, after its file has
/// arrived, so a picture chosen and then abandoned is never shown. The same as a staff photo (§ 32).
/// <para>
/// Not audited, because no change to the clinic profile is: the profile is what the clinic prints
/// on its sign, and the file is not shown anywhere until a profile edit names it.
/// </para>
/// </summary>
public sealed class RequestBrandImageHandler(IFileStorage storage, RequestValidator validator)
{
    public async Task<BrandImageUploadDto> HandleAsync(RequestBrandImageCommand command, CancellationToken cancellationToken = default)
    {
        command = command with { ContentType = command.ContentType?.Split(';')[0].Trim().ToLowerInvariant() };
        await validator.ValidateAsync(command, cancellationToken);

        var key = ClinicBrandImage.NewKey(command.Kind, command.ContentType!);
        var ticket = storage.CreateUpload(key, command.ContentType!, command.SizeBytes, BrandImageLinks.UploadLifetime);

        return new BrandImageUploadDto(ClinicBrandImage.ValueOf(key), UploadTicketDto.From(ticket));
    }
}
