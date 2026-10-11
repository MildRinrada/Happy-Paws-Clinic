using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.BrandImages;

/// <summary>
/// <c>GET /clinic/logo</c> and <c>GET /clinic/tab-icon</c>: a fresh signed link to the uploaded
/// picture, for the controller to redirect to. Extension, § 50.
/// <para>
/// Only an uploaded picture is served here, and only the one asked for: the tab icon does not fall
/// back to the logo, because that choice (and drawing a wide logo into a square) is the website's.
/// A logo that is an address on another site answers <c>404</c> too: the website uses such an
/// address as it is, and redirecting to it would make this route a way to send people to whatever
/// was typed there.
/// </para>
/// </summary>
public sealed class GetBrandImageHandler(IClinicSettingsStore settings, IFileStorage storage)
{
    public async Task<string> HandleAsync(ClinicBrandImageKind kind, CancellationToken cancellationToken = default)
    {
        var profile = await settings.ProfileAsync(cancellationToken);
        var value = kind == ClinicBrandImageKind.Logo ? profile.LogoUrl : profile.TabIconUrl;

        return ClinicBrandImage.StorageKeyOf(kind, value) is { } key
            ? storage.CreateDownloadUrl(key, BrandImageLinks.PublicLinkLifetime)
            : throw new NotFoundException(kind == ClinicBrandImageKind.Logo ? "clinic_logo" : "clinic_tab_icon", ClinicProfile.SingletonId);
    }
}
