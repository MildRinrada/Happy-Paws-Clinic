using HappyPaws.Application.Clinics.Dtos;

namespace HappyPaws.Application.Clinics.GetClinicProfile;

/// <summary>
/// <c>GET /clinic</c> — "ชื่อ โลโก้ สี ใบอนุญาต ผู้ควบคุมกิจการ".
/// <para>
/// Public, and safe to be: the profile holds only what a clinic already prints on its own sign.
/// An instance that has never been configured answers with the default profile rather than 404 —
/// the clinic's website has to render something on the day it is installed.
/// </para>
/// </summary>
public sealed class GetClinicProfileHandler(IClinicSettingsStore settings)
{
    public async Task<ClinicProfileDto> HandleAsync(CancellationToken cancellationToken = default) =>
        ClinicProfileDto.From(await settings.ProfileAsync(cancellationToken));
}
