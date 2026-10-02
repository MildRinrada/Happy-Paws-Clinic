using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments.GetAppointment;

/// <summary><c>GET /appointments/{id}</c> — "ลูกค้าเห็นเฉพาะนัดของสัตว์ตัวเอง".</summary>
public sealed record GetAppointmentQuery(string AppointmentId);

/// <summary>
/// Reads one appointment for either audience.
/// <para>
/// The route carries no scope attribute because it serves both sides; the whole access decision
/// is <see cref="AppointmentAccess"/>, which admits staff by scope and branch and customers by
/// care of the pet, and answers <c>404</c> to everyone else.
/// </para>
/// </summary>
public sealed class GetAppointmentHandler(
    IAppointmentRepository appointments,
    AppointmentAccess access)
{
    public async Task<AppointmentDto> HandleAsync(
        GetAppointmentQuery query,
        CancellationToken cancellationToken = default)
    {
        var appointment = await Load(appointments, query.AppointmentId, cancellationToken);

        await access.AuthoriseAsync(appointment, Scopes.Appointment.Read, cancellationToken);

        return AppointmentDto.From(appointment);
    }

    /// <summary>
    /// Loads an appointment or answers <c>404</c>. Shared by the action handlers so that a missing
    /// id and an id the caller may not see produce the same response — the point of the 404 is
    /// lost if one of them leaks a different shape.
    /// </summary>
    internal static async Task<Appointment> Load(
        IAppointmentRepository appointments,
        string appointmentId,
        CancellationToken cancellationToken) =>
        await appointments.FindByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("appointment", appointmentId);
}
