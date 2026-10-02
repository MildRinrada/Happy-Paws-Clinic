using FluentValidation;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointment;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments.UpdateAppointment;

/// <summary>
/// <c>PATCH /staff/appointments/{id}</c> — "เปลี่ยนหมอ บริการ หรืออาการที่แจ้ง".
/// <para>
/// A null <c>Reason</c> leaves the recorded symptoms alone; an empty string clears them. That
/// distinction is what <c>PATCH</c> is for, and collapsing the two would make it impossible to
/// remove a note once written.
/// </para>
/// </summary>
public sealed record UpdateAppointmentCommand(
    string AppointmentId,
    string? ServiceId = null,
    string? StaffId = null,
    string? Reason = null,
    uint? IfMatchVersion = null);

public sealed class UpdateAppointmentValidator : AbstractValidator<UpdateAppointmentCommand>
{
    public UpdateAppointmentValidator() => RuleFor(command => command.Reason).MaximumLength(1000);
}

/// <summary>
/// Edits an appointment in place, without moving it.
/// <para>
/// <c>If-Match</c> is required, not optional. Two receptionists with the same appointment open is
/// the ordinary case rather than the exotic one, and a blind <c>PATCH</c> would let the second
/// save silently erase the first — including a change of vet that the first person had already
/// told the customer about.
/// </para>
/// <para>
/// Changing the service usually changes the duration, so the end time moves with it and the new
/// window is re-checked against the calendar. A twenty-minute check-up upgraded to a ninety-minute
/// dental cannot be allowed to quietly overrun the next patient.
/// </para>
/// </summary>
public sealed class UpdateAppointmentHandler(
    IAppointmentRepository appointments,
    IBookingCatalog catalog,
    BookingSlotResolver resolver,
    BranchGuard branchGuard,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<AppointmentDto> HandleAsync(
        UpdateAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Appointment? updated = null;

        // Loaded inside the transaction, because a retry resets the change tracker and an
        // appointment fetched beforehand would be detached — see IUnitOfWork.
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var appointment = await GetAppointmentHandler.Load(appointments, command.AppointmentId, token);

            branchGuard.EnsureCanAccess(appointment.BranchId);
            ConcurrencyGuard.EnsureRequired(appointment, command.IfMatchVersion);

            var serviceId = command.ServiceId ?? appointment.ServiceId;

            var service = await catalog.FindServiceAsync(serviceId, token)
                ?? throw new NotFoundException("service", serviceId);

            var slot = await resolver.ResolveAsync(
                new SlotRequest(
                    serviceId,
                    command.StaffId ?? appointment.StaffId,
                    appointment.BranchId,
                    appointment.StartsAt,
                    BypassLeadTime: true,
                    RequireOnlineBookable: false,
                    ExcludeAppointmentId: appointment.Id),
                token);

            appointment.ChangeDetails(
                slot.Service.Id,
                slot.StaffId,
                appointment.StartsAt.AddMinutes(service.DurationMinutes),
                command.Reason);

            updated = appointment;
        }, cancellationToken);

        return AppointmentDto.From(updated!);
    }
}
