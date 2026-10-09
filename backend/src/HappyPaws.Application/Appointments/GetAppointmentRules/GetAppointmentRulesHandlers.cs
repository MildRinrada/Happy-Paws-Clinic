using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Clinics;

namespace HappyPaws.Application.Appointments.GetAppointmentRules;

/// <summary>
/// <c>GET /staff/policies/cancellation</c> — the policy as stored, with the version to send as
/// <c>If-Match</c> on the next <c>PUT</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 23</c>.
/// <para>
/// A clinic that has never saved one reads the defaults with version 0, which is also what the
/// <c>PUT</c> accepts as <c>If-Match</c> before the first save.
/// </para>
/// </summary>
public sealed class GetCancellationPolicyHandler(IClinicSettingsStore settings)
{
    public async Task<CancellationPolicyDto> HandleAsync(CancellationToken cancellationToken = default) =>
        CancellationPolicyDto.From(await settings.CancellationPolicyAsync(cancellationToken));
}

/// <summary>
/// <c>GET /staff/appointment-types</c> — every type and its rules, with the version, the same shape
/// the <c>PUT</c> answers with. Read by the counter's booking form and the settings page.
/// <c>docs/contract-extensions.md § 23</c>.
/// </summary>
public sealed class GetAppointmentTypesHandler(IClinicSettingsStore settings)
{
    public async Task<AppointmentTypesDto> HandleAsync(CancellationToken cancellationToken = default) =>
        AppointmentTypesDto.From(await settings.AppointmentTypesAsync(cancellationToken));
}

/// <summary>
/// <c>GET /portal/appointment-types</c> — the types a customer may book themselves, in the clinic's
/// order. <c>docs/contract-extensions.md § 23</c>.
/// <para>
/// Filtered by the same flag the booking path enforces (<c>online_booking</c>), so the form never
/// offers a type that <c>POST /portal/appointments</c> would refuse.
/// </para>
/// </summary>
public sealed class ListOnlineAppointmentTypesHandler(IClinicSettingsStore settings)
{
    public async Task<IReadOnlyList<OnlineAppointmentTypeDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var catalog = await settings.AppointmentTypesAsync(cancellationToken);

        return [.. catalog.Types.Where(type => type.OnlineBooking).Select(OnlineAppointmentTypeDto.From)];
    }
}
