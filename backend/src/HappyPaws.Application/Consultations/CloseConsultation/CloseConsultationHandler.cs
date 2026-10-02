using HappyPaws.Application.Appointments;
using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Application.MedicalRecords;
using HappyPaws.Application.MedicalRecords.Dtos;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Queueing;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.MedicalRecords;
using HappyPaws.Domain.Queueing;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Consultations.CloseConsultation;

/// <summary>
/// <c>POST /staff/consultations/{id}/close</c> —
/// "sign เวชระเบียน ออกบิล สร้างนัดติดตาม ส่งสรุป".
/// </summary>
public sealed record CloseConsultationCommand(
    string ConsultationId,
    FollowUpRequest? FollowUp = null,
    uint? IfMatchVersion = null);

/// <summary>The follow-up visit to book on the way out — "สร้างนัดติดตาม (นัดติดตาม 27 ส.ค.)".</summary>
public sealed record FollowUpRequest(DateTimeOffset StartsAt, string? ServiceId = null, string? StaffId = null);

/// <summary>What closing a case produced.</summary>
public sealed record CloseConsultationResult(
    ConsultationDto Consultation,
    MedicalRecordDto Record,
    AppointmentDto? FollowUp,
    long ChargesSatang,
    InvoiceDto? Invoice);

/// <summary>
/// Ends the visit: signs the record, releases the room, completes the appointment and books the
/// follow-up — all in one transaction.
/// <para>
/// This is the widest transaction in the system, and every part of it belongs there. A signed
/// record with the patient still shown in a consulting room is a room nobody can use; a completed
/// appointment with no record is a visit that vanished from the patient's history; a follow-up
/// booked against a case that failed to close is an appointment for a consultation that never
/// happened. Either all of it lands or none of it does.
/// </para>
/// <para>
/// <b>The invoice is part of it.</b> "ออกบิล" is in this endpoint's description and the contract's
/// worked example ends the visit with <c>invoice.issued ฿1,460</c>, so the bill is raised in the
/// same transaction through <see cref="ConsultationBilling"/> — a signed record whose charges never
/// became a bill is a visit the clinic performed for free without deciding to. Only the treatments
/// are billed; medicine is charged at dispense, for the reason given there.
/// </para>
/// </summary>
public sealed class CloseConsultationHandler(
    ConsultationAccess access,
    IMedicalRecordRepository records,
    IAppointmentRepository appointments,
    IQueueTicketRepository tickets,
    IBookingCatalog catalog,
    IClinicSettingsStore settings,
    ConsultationBilling billing,
    IQueueEventStream events,
    INotificationDispatcher notifications,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<CloseConsultationHandler> logger)
{
    public async Task<CloseConsultationResult> HandleAsync(
        CloseConsultationCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;

        Consultation? closed = null;
        MedicalRecord? record = null;
        Appointment? followUp = null;
        QueueTicket? servedTicket = null;
        Invoice? invoice = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var consultation = await access.ForStaffAsync(command.ConsultationId, token);

            ConcurrencyGuard.Ensure(consultation, command.IfMatchVersion);

            await EnsureClinicRulesAsync(consultation, staffId, token);

            var signed = MedicalRecord.Sign(
                consultation.Id,
                consultation.PetId,
                consultation.CustomerId,
                consultation.BranchId,
                RecordContent.Build(consultation),
                staffId,
                now);

            // Close before adding the record: the aggregate refuses if the case is already closed,
            // so a duplicate request fails here rather than after writing a second document.
            consultation.Close(now, signed.Id);
            records.Add(signed);

            servedTicket = await ReleaseRoomAsync(consultation, now, token);
            await CompleteAppointmentAsync(consultation, token);
            followUp = await BookFollowUpAsync(consultation, command.FollowUp, staffId, token);
            invoice = await billing.RaiseAsync(consultation, staffId, now, token);

            audit.Record(new AuditEvent(
                AuditActions.ConsultationClosed,
                AuditResources.Consultation,
                consultation.Id,
                consultation.PetId,
                consultation.BranchId));

            audit.Record(new AuditEvent(
                AuditActions.RecordSigned,
                AuditResources.MedicalRecord,
                signed.Id,
                consultation.PetId,
                consultation.BranchId,
                new Dictionary<string, object?>
                {
                    ["consultation_id"] = consultation.Id,
                    ["content_hash"] = signed.ContentHash,
                    ["charges_satang"] = consultation.TreatmentCharges.Satang,
                }));

            (closed, record) = (consultation, signed);
        }, cancellationToken);

        if (servedTicket is not null)
        {
            events.Publish(new QueueEvent(
                QueueEventTypes.TicketUpdated, servedTicket.BranchId, servedTicket.Id, servedTicket.CustomerId));
        }

        await NotifyOwnerAsync(closed!, record!, cancellationToken);

        return new CloseConsultationResult(
            ConsultationDto.From(closed!),
            MedicalRecordDto.From(record!),
            followUp is null ? null : AppointmentDto.From(followUp),
            closed!.TreatmentCharges.Satang,
            invoice is null ? null : InvoiceDto.From(invoice));
    }

    /// <summary>
    /// Applies what this clinic has configured under <c>PATCH /clinic/settings/medical-record</c>
    /// — "ฟิลด์บังคับ … การ sign" — before anything is signed.
    /// <para>
    /// Read inside the transaction and on every close, so a rule tightened this morning applies to
    /// the next case closed and not to the ones already signed.
    /// </para>
    /// </summary>
    private async Task EnsureClinicRulesAsync(
        Consultation consultation,
        string staffId,
        CancellationToken cancellationToken)
    {
        var rules = await settings.MedicalRecordAsync(cancellationToken);

        if (rules.SignerMustBeCaseVet && consultation.VetStaffId != staffId)
        {
            throw new ForbiddenException(
                $"clinic requires the case vet {consultation.VetStaffId} to sign; caller is {staffId}");
        }

        var missing = ConsultationRequirements.MissingFrom(consultation, rules.RequiredFields);

        if (missing.Count > 0)
        {
            throw new IncompleteConsultationException(missing);
        }
    }

    /// <summary>Marks the queue ticket served, if the case came from one and it is still live.</summary>
    private async Task<QueueTicket?> ReleaseRoomAsync(
        Consultation consultation,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (consultation.QueueTicketId is not { Length: > 0 } ticketId)
        {
            return null;
        }

        var ticket = await tickets.FindByIdAsync(ticketId, cancellationToken);

        if (ticket is null || ticket.Status != QueueTicketStatus.InRoom)
        {
            return null;
        }

        ticket.MarkServed(now);

        return ticket;
    }

    private async Task CompleteAppointmentAsync(Consultation consultation, CancellationToken cancellationToken)
    {
        if (consultation.AppointmentId is not { Length: > 0 } appointmentId)
        {
            return;
        }

        var appointment = await appointments.FindByIdAsync(appointmentId, cancellationToken);
        appointment?.Complete();
    }

    /// <summary>
    /// Books the follow-up inside the same transaction.
    /// <para>
    /// Availability is not re-checked, matching <c>POST /staff/appointments</c> — "จองแทนลูกค้าและ
    /// รับ walk-in · ข้าม lead time ได้". What still holds is the database's exclusion constraint on
    /// overlapping slots, so a follow-up onto a time the vet is already booked for is refused as
    /// <c>slot_unavailable</c> rather than silently double-booked.
    /// </para>
    /// </summary>
    private async Task<Appointment?> BookFollowUpAsync(
        Consultation consultation,
        FollowUpRequest? request,
        string staffId,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return null;
        }

        var serviceId = request.ServiceId ?? await OriginalServiceIdAsync(consultation, cancellationToken)
            ?? throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["follow_up.service_id"] = ["ต้องระบุบริการสำหรับนัดติดตาม"],
            });

        var service = await catalog.FindServiceAsync(serviceId, cancellationToken)
            ?? throw new NotFoundException("service", serviceId);

        var appointment = Appointment.Book(
            consultation.BranchId,
            consultation.CustomerId,
            consultation.PetId,
            service.Id,
            request.StaffId ?? consultation.VetStaffId,
            request.StartsAt,
            request.StartsAt.AddMinutes(service.DurationMinutes),
            reason: "นัดติดตามผล",
            bookedByStaffId: staffId);

        appointments.Add(appointment);

        return appointment;
    }

    /// <summary>The service the original visit was booked for, so a follow-up can simply repeat it.</summary>
    private async Task<string?> OriginalServiceIdAsync(Consultation consultation, CancellationToken cancellationToken)
    {
        if (consultation.AppointmentId is not { Length: > 0 } appointmentId)
        {
            return null;
        }

        var appointment = await appointments.FindByIdAsync(appointmentId, cancellationToken);

        return appointment?.ServiceId;
    }

    private async Task NotifyOwnerAsync(
        Consultation consultation,
        MedicalRecord record,
        CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["record_id"] = record.Id,
            ["diagnosis"] = consultation.PrimaryDiagnosis?.Description ?? string.Empty,
            ["charges"] = consultation.TreatmentCharges.ToString(),
        };

        try
        {
            await notifications.SendAsync(
                new NotificationRequest(
                    consultation.CustomerId, NotificationTemplates.ConsultationSummary, data),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The record is signed; that is the fact of record. A summary that did not send is
            // worth an alert, never a reason to fail a close the vet has already moved on from.
            logger.LogError(
                exception,
                "Consultation summary notification failed for record {RecordId} (customer {CustomerId})",
                record.Id,
                consultation.CustomerId);
        }
    }
}
