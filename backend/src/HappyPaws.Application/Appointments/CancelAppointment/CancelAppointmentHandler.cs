using FluentValidation;
using HappyPaws.Application.Appointments.AddToWaitingList;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointment;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Integrations;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Appointments.CancelAppointment;

/// <summary><c>POST /appointments/{id}/cancel</c> — "บันทึกเหตุผลและผู้ยกเลิก · ปล่อย slot คืน".</summary>
public sealed record CancelAppointmentCommand(string AppointmentId, string? Reason = null);

public sealed class CancelAppointmentValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentValidator() => RuleFor(command => command.Reason).MaximumLength(500);
}

/// <summary>
/// Cancels a visit and gives the time back.
/// <para>
/// The slot is released by the status change alone — a cancelled appointment stops occupying its
/// window, so availability recomputes it as free on the next read. There is no second "release"
/// step that could fail and leave a phantom booking on the calendar.
/// </para>
/// <para>
/// The row is never deleted. Who cancelled and why is what a disputed no-show fee turns on, and
/// the contract asks for both explicitly.
/// </para>
/// <para>
/// <b>Two Tier-2 rules ride on it.</b> A customer cancelling inside the cancellation policy's
/// cutoff is marked late, which counts against them like a no-show; a cancellation by staff is
/// not, because the clinic calling off a visit is not the customer's doing. And once the time is
/// free, the waiting list is told — after the commit, so a message is never sent about a slot that
/// a rolled-back cancellation still holds.
/// </para>
/// </summary>
public sealed class CancelAppointmentHandler(
    IAppointmentRepository appointments,
    AppointmentAccess access,
    IClinicSettingsStore settings,
    WaitingListNotifier waitingList,
    ICurrentUser currentUser,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock,
    ILogger<CancelAppointmentHandler> logger)
{
    public async Task<AppointmentDto> HandleAsync(
        CancelAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var appointment = await GetAppointmentHandler.Load(appointments, command.AppointmentId, cancellationToken);

        var audience = await access.AuthoriseAsync(appointment, Scopes.Appointment.Write, cancellationToken);

        var userId = currentUser.UserId ?? throw new ForbiddenException("no subject on token");
        var now = clock.UtcNow;
        var wasHoldingTime = appointment.OccupiesSlot;

        var isLate = audience == AppointmentAudience.Customer
            && (await settings.CancellationPolicyAsync(cancellationToken)).IsLate(now, appointment.StartsAt);

        appointment.Cancel(now, userId, command.Reason, isLate);
        webhooks.Enqueue(WebhookEventTypes.AppointmentCancelled, WebhookPayloads.Appointment(appointment));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Appointment {AppointmentId} cancelled by {Audience} {UserId}",
            appointment.Id,
            audience,
            userId);

        if (wasHoldingTime)
        {
            await waitingList.SlotFreedAsync(appointment, cancellationToken);
        }

        return AppointmentDto.From(appointment);
    }
}
