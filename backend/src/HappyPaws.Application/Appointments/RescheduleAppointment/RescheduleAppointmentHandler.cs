using FluentValidation;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointment;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Appointments.RescheduleAppointment;

/// <summary><c>POST /appointments/{id}/reschedule</c> — "ลูกค้าเลื่อนได้ถ้ายังไม่ถึง cutoff".</summary>
public sealed record RescheduleAppointmentCommand(
    string AppointmentId,
    DateTimeOffset StartsAt,
    string? StaffId = null,
    uint? IfMatchVersion = null);

public sealed class RescheduleAppointmentValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentValidator() =>
        RuleFor(command => command.StartsAt).NotEqual(default(DateTimeOffset)).WithMessage("ต้องระบุเวลาเริ่มใหม่");
}

/// <summary>
/// Moves an appointment to a new time, re-running the full slot check on the destination.
/// <para>
/// The appointment being moved is excluded from its own collision check. Without that exclusion,
/// shifting a visit by ten minutes would collide with the slot it is in the middle of vacating and
/// be refused as double-booked.
/// </para>
/// <para>
/// A successful move clears the confirmation. The customer confirmed a specific time; carrying
/// that confirmation across to a different one would show the clinic a confirmed appointment
/// nobody had agreed to.
/// </para>
/// </summary>
public sealed class RescheduleAppointmentHandler(
    IAppointmentRepository appointments,
    AppointmentAccess access,
    BookingSlotResolver resolver,
    IBookingCatalog catalog,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<AppointmentDto> HandleAsync(
        RescheduleAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Appointment? rescheduled = null;

        // Loaded inside the transaction, because a retry resets the change tracker and an
        // appointment fetched beforehand would be detached — see IUnitOfWork.
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var appointment = await GetAppointmentHandler.Load(appointments, command.AppointmentId, token);

            var audience = await access.AuthoriseAsync(appointment, Scopes.Appointment.Write, token);

            ConcurrencyGuard.Ensure(appointment, command.IfMatchVersion);

            if (audience == AppointmentAudience.Customer)
            {
                await EnsureBeforeCutoffAsync(appointment, token);
            }

            var previousStartsAt = appointment.StartsAt;

            var slot = await resolver.ResolveAsync(
                new SlotRequest(
                    appointment.ServiceId,
                    command.StaffId ?? appointment.StaffId,
                    appointment.BranchId,
                    command.StartsAt,
                    BypassLeadTime: audience == AppointmentAudience.Staff,
                    RequireOnlineBookable: audience == AppointmentAudience.Customer,
                    ExcludeAppointmentId: appointment.Id,

                    // A moved visit keeps its length. It may have been set by the appointment type
                    // rather than the service, and moving it is not a reason to change it.
                    DurationMinutes: (int)(appointment.EndsAt - appointment.StartsAt).TotalMinutes),
                token);

            appointment.Reschedule(slot.StartsAt, slot.EndsAt, slot.StaffId);

            webhooks.Enqueue(
                WebhookEventTypes.AppointmentRescheduled,
                WebhookPayloads.Appointment(appointment, previousStartsAt: previousStartsAt));
            rescheduled = appointment;
        }, cancellationToken);

        return AppointmentDto.From(rescheduled!);
    }

    private async Task EnsureBeforeCutoffAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var settings = await catalog.GetBookingSettingsAsync(cancellationToken);
        var cutoffAt = appointment.StartsAt.AddHours(-settings.CancellationCutoffHours);

        if (clock.UtcNow >= cutoffAt)
        {
            throw new CutoffPassedException(settings.CancellationCutoffHours, cutoffAt);
        }
    }
}
