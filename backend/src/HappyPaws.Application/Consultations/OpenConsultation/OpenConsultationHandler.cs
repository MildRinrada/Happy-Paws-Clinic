using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Application.Queueing;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Consultations.OpenConsultation;

/// <summary><c>POST /staff/consultations</c> — "เปิดเคสจากคิวหรือจากนัด".</summary>
public sealed record OpenConsultationCommand(
    string? AppointmentId = null,
    string? QueueTicketId = null,
    string? ChiefComplaint = null);

/// <summary>
/// Takes the patient into the consulting room.
/// <para>
/// Three things move together and must not come apart: the case is created, the queue ticket
/// becomes <c>in_room</c>, and the appointment becomes <c>in_consultation</c>. A case with no
/// ticket transition leaves the patient apparently still waiting on the board while the vet is
/// examining them; a ticket moved without a case is a room marked busy with nothing in it.
/// </para>
/// <para>
/// Pressing the button twice returns the case that already exists rather than a conflict. The
/// usual reason a vet presses again is that the first response never arrived, and answering 409
/// there would strand them on a case they cannot see.
/// </para>
/// </summary>
public sealed class OpenConsultationHandler(
    IConsultationRepository consultations,
    IAppointmentRepository appointments,
    IQueueTicketRepository tickets,
    IQueueEventStream events,
    IAuditTrail audit,
    BranchGuard branchGuard,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ConsultationDto> HandleAsync(
        OpenConsultationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AppointmentId is not { Length: > 0 } && command.QueueTicketId is not { Length: > 0 })
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["appointment_id"] = ["ต้องระบุนัดหรือบัตรคิวอย่างใดอย่างหนึ่ง"],
            });
        }

        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;

        Consultation? opened = null;
        QueueTicket? movedTicket = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            // Order matters here. The origin is resolved and checked for coherence first, then the
            // caller is authorised, and only then does the existing-case lookup run. Looking first
            // would hand back a case for whichever of the two ids happened to match — so a request
            // naming one visit's appointment and another's ticket would be answered with a case
            // belonging to neither, and reported as a success.
            var (appointment, ticket) = await ResolveOriginAsync(command, token);
            var branchId = appointment?.BranchId ?? ticket!.BranchId;

            branchGuard.EnsureCanAccess(branchId);

            var existing = await consultations.FindByOriginAsync(appointment?.Id, ticket?.Id, token);

            if (existing is not null)
            {
                opened = existing;
                return;
            }

            appointment?.BeginConsultation();
            movedTicket = TakeIntoRoom(ticket, staffId, now);

            var consultation = Consultation.Open(
                branchId,
                appointment?.PetId ?? ticket!.PetId,
                appointment?.CustomerId ?? ticket!.CustomerId,
                staffId,
                now,
                appointment?.Id,
                ticket?.Id,
                command.ChiefComplaint ?? appointment?.Reason);

            consultations.Add(consultation);

            audit.Record(new AuditEvent(
                AuditActions.ConsultationOpened,
                AuditResources.Consultation,
                consultation.Id,
                consultation.PetId,
                branchId));

            opened = consultation;
        }, cancellationToken);

        if (movedTicket is not null)
        {
            // After commit: the board should announce a room change only once it is true.
            events.Publish(new QueueEvent(
                QueueEventTypes.TicketUpdated, movedTicket.BranchId, movedTicket.Id, movedTicket.CustomerId));
        }

        return ConsultationDto.From(opened!);
    }

    /// <summary>
    /// Moves the ticket into the consulting room, calling the patient first if nobody did.
    /// <para>
    /// The state machine only allows <c>called → in_room</c>, and that is worth keeping: a patient
    /// cannot be in a room they were never asked into. But insisting the board be used would refuse
    /// the ordinary case of a vet who walks out and fetches the patient themselves — common in a
    /// small clinic, and in a busy one whenever the tannoy is broken. So opening the case <i>is</i>
    /// the call, attributed to the vet who opened it, and <c>called_at</c> stays honest for the
    /// wait-time statistics rather than staying null.
    /// </para>
    /// <para>
    /// A ticket already in the room — the vet stepped out and came back — is left alone. What gates
    /// a double-open is the case lookup above, not this.
    /// </para>
    /// </summary>
    private static QueueTicket? TakeIntoRoom(QueueTicket? ticket, string staffId, DateTimeOffset now)
    {
        if (ticket is null || ticket.Status is QueueTicketStatus.InRoom)
        {
            return null;
        }

        if (ticket.Status is not QueueTicketStatus.Called)
        {
            ticket.Call(staffId, now);
        }

        ticket.EnterRoom(now);

        return ticket;
    }

    /// <summary>
    /// Resolves whichever of the two origins was given, and pairs it with the other when it exists.
    /// A case opened from an appointment still has to move that appointment's ticket, and a case
    /// opened from a walk-in ticket has no appointment at all.
    /// </summary>
    private async Task<(Appointment? Appointment, QueueTicket? Ticket)> ResolveOriginAsync(
        OpenConsultationCommand command,
        CancellationToken cancellationToken)
    {
        QueueTicket? ticket = null;
        Appointment? appointment = null;

        if (command.QueueTicketId is { Length: > 0 } ticketId)
        {
            ticket = await tickets.FindByIdAsync(ticketId, cancellationToken)
                ?? throw new NotFoundException("queue ticket", ticketId);
        }

        var appointmentId = command.AppointmentId ?? ticket?.AppointmentId;

        if (appointmentId is { Length: > 0 })
        {
            appointment = await appointments.FindByIdAsync(appointmentId, cancellationToken)
                ?? throw new NotFoundException("appointment", appointmentId);

            ticket ??= await tickets.FindTrackedByAppointmentAsync(appointment.Id, cancellationToken);
        }

        if (ticket is not null && appointment is not null && ticket.AppointmentId != appointment.Id)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["queue_ticket_id"] = ["บัตรคิวนี้ไม่ใช่ของนัดที่ระบุ"],
            });
        }

        return (appointment, ticket);
    }
}
