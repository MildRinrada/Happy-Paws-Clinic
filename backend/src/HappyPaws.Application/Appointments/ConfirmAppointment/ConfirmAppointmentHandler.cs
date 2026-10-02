using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointment;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Access;

namespace HappyPaws.Application.Appointments.ConfirmAppointment;

/// <summary><c>POST /appointments/{id}/confirm</c> — "ยืนยันจากลิงก์เตือนหรือปุ่มใน LINE".</summary>
public sealed record ConfirmAppointmentCommand(string AppointmentId);

/// <summary>
/// Marks an appointment confirmed by the owner or by the counter.
/// <para>
/// Confirming an already-confirmed appointment succeeds and changes nothing. This one is worth
/// being deliberate about: the trigger is a reminder link or a LINE button, so a double tap and a
/// re-opened message are ordinary events, and answering <c>409</c> to a customer who simply
/// pressed twice would be a bug wearing a state machine's clothes.
/// </para>
/// </summary>
public sealed class ConfirmAppointmentHandler(
    IAppointmentRepository appointments,
    AppointmentAccess access,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<AppointmentDto> HandleAsync(
        ConfirmAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var appointment = await GetAppointmentHandler.Load(appointments, command.AppointmentId, cancellationToken);

        await access.AuthoriseAsync(appointment, Scopes.Appointment.Write, cancellationToken);

        appointment.Confirm(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AppointmentDto.From(appointment);
    }
}
