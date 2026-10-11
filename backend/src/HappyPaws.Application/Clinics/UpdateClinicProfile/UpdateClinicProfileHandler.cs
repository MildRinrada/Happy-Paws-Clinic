using FluentValidation;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Common;

namespace HappyPaws.Application.Clinics.UpdateClinicProfile;

/// <summary>
/// <c>PATCH /clinic</c> — "โปรไฟล์และ branding ของคลินิก". A field that is absent is left alone;
/// an empty string clears it. <c>TabIconUrl</c> and <c>ShowNameWithLogo</c> are § 50's.
/// </summary>
public sealed record UpdateClinicProfileCommand(
    string? Name = null,
    string? LogoUrl = null,
    string? BrandColor = null,
    string? LicenseNo = null,
    string? OperatorName = null,
    uint? IfMatchVersion = null,
    string? TabIconUrl = null,
    bool? ShowNameWithLogo = null);

public sealed class UpdateClinicProfileValidator : AbstractValidator<UpdateClinicProfileCommand>
{
    public UpdateClinicProfileValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().MaximumLength(200)
            .When(command => command.Name is not null);

        // Shape is checked here so a malformed value answers 422; the entity repeats both checks
        // because it is the one thing that cannot be bypassed. An uploaded logo or tab icon (§ 50) is
        // taken only in the exact shape POST /clinic/logo or /clinic/tab-icon hands out, so a key
        // of the other kind or under another prefix (a staff photo, a pet's document) is refused.
        RuleFor(command => command.LogoUrl)
            .MaximumLength(1000)
            .Must(BeWebAddressOrUploadedLogo).WithMessage("โลโก้ต้องเป็น URL แบบ http หรือ https หรือรูปที่อัปโหลดผ่านหน้าตั้งค่า")
            .When(command => !string.IsNullOrWhiteSpace(command.LogoUrl));

        RuleFor(command => command.TabIconUrl)
            .MaximumLength(1000)
            .Must(value => ClinicBrandImage.StorageKeyOf(ClinicBrandImageKind.TabIcon, value?.Trim()) is not null)
            .WithMessage("ไอคอนบนแท็บต้องเป็นรูปที่อัปโหลดผ่านหน้าตั้งค่า")
            .When(command => !string.IsNullOrWhiteSpace(command.TabIconUrl));

        RuleFor(command => command.BrandColor)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("สีต้องอยู่ในรูปแบบ #RRGGBB")
            .When(command => !string.IsNullOrWhiteSpace(command.BrandColor));

        RuleFor(command => command.LicenseNo).MaximumLength(64);
        RuleFor(command => command.OperatorName).MaximumLength(200);
    }

    private static bool BeWebAddressOrUploadedLogo(string? value)
    {
        var trimmed = value?.Trim();

        if (ClinicBrandImage.IsUploaded(trimmed))
        {
            return ClinicBrandImage.StorageKeyOf(ClinicBrandImageKind.Logo, trimmed) is not null;
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}

/// <summary>
/// Edits the clinic's public profile.
/// <para>
/// An uploaded logo or tab icon (§ 50) is named by the value <c>POST /clinic/logo</c> or
/// <c>POST /clinic/tab-icon</c> answered, and is taken only once its file has arrived, so the
/// website never points at a file that was never sent. Sending the value already stored keeps the
/// picture without asking the storage again; an empty string removes it, and the website goes back
/// to the logo (for the tab) or to its stand-in paw.
/// </para>
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
    IFileStorage storage,
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

            var (logoBefore, tabIconBefore) = (profile.LogoUrl, profile.TabIconUrl);

            profile.Update(
                command.Name,
                command.LogoUrl,
                command.BrandColor,
                command.LicenseNo,
                command.OperatorName,
                command.TabIconUrl,
                command.ShowNameWithLogo);

            await EnsureArrivedAsync(ClinicBrandImageKind.Logo, logoBefore, profile.LogoUrl, token);
            await EnsureArrivedAsync(ClinicBrandImageKind.TabIcon, tabIconBefore, profile.TabIconUrl, token);

            updated = profile;
        }, cancellationToken);

        return ClinicProfileDto.From(updated!);
    }

    /// <summary>A newly named upload must be in the storage. A value already stored is not asked about again.</summary>
    private async Task EnsureArrivedAsync(ClinicBrandImageKind kind, string? before, string? after, CancellationToken cancellationToken)
    {
        if (after != before
            && ClinicBrandImage.StorageKeyOf(kind, after) is { } key
            && !await storage.ExistsAsync(key, cancellationToken))
        {
            throw new DomainException(
                "invalid_clinic_profile",
                kind == ClinicBrandImageKind.Logo
                    ? "ยังไม่ได้รับไฟล์โลโก้ ลองเลือกไฟล์แล้วอัปโหลดใหม่อีกครั้ง"
                    : "ยังไม่ได้รับไฟล์ไอคอนบนแท็บ ลองเลือกไฟล์แล้วอัปโหลดใหม่อีกครั้ง");
        }
    }
}
