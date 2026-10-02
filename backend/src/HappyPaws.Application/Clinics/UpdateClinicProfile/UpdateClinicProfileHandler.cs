using FluentValidation;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.UpdateClinicProfile;

/// <summary>
/// <c>PATCH /clinic</c> — "โปรไฟล์และ branding ของคลินิก". A field that is absent is left alone;
/// an empty string clears it.
/// </summary>
public sealed record UpdateClinicProfileCommand(
    string? Name = null,
    string? LogoUrl = null,
    string? BrandColor = null,
    string? LicenseNo = null,
    string? OperatorName = null,
    uint? IfMatchVersion = null);

public sealed class UpdateClinicProfileValidator : AbstractValidator<UpdateClinicProfileCommand>
{
    public UpdateClinicProfileValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().MaximumLength(200)
            .When(command => command.Name is not null);

        // Shape is checked here so a malformed value answers 422; the entity repeats both checks
        // because it is the one thing that cannot be bypassed.
        RuleFor(command => command.LogoUrl)
            .MaximumLength(1000)
            .Must(BeWebAddress).WithMessage("ที่อยู่โลโก้ต้องเป็น URL แบบ http หรือ https")
            .When(command => !string.IsNullOrWhiteSpace(command.LogoUrl));

        RuleFor(command => command.BrandColor)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("สีต้องอยู่ในรูปแบบ #RRGGBB")
            .When(command => !string.IsNullOrWhiteSpace(command.BrandColor));

        RuleFor(command => command.LicenseNo).MaximumLength(64);
        RuleFor(command => command.OperatorName).MaximumLength(200);
    }

    private static bool BeWebAddress(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

/// <summary>
/// Edits the clinic's public profile.
/// <para>
/// <c>If-Match</c> is honoured but not demanded. Two admins editing the footer at once is rare and
/// the loser re-types a name; demanding the header would buy little and would make the first edit
/// on a fresh instance — where there is no row to have read a version from — an awkward special
/// case.
/// </para>
/// </summary>
public sealed class UpdateClinicProfileHandler(
    IClinicSettingsStore settings,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<ClinicProfileDto> HandleAsync(
        UpdateClinicProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        ClinicProfile? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var profile = await settings.ProfileForUpdateAsync(token);

            ConcurrencyGuard.Ensure(profile, command.IfMatchVersion);

            profile.Update(
                command.Name, command.LogoUrl, command.BrandColor, command.LicenseNo, command.OperatorName);

            updated = profile;
        }, cancellationToken);

        return ClinicProfileDto.From(updated!);
    }
}
