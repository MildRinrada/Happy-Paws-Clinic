using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.GetAppointment;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Application.Queueing;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Appointments.CheckInAppointment;

/// <summary>
/// <c>POST /appointments/{id}/check-in</c> —
/// "เช็กอินที่เคาน์เตอร์หรือลูกค้าเช็กอินเอง · สร้างคิวทันที".
/// </summary>
public sealed record CheckInAppointmentCommand(string AppointmentId);

/// <summary>
/// Admits the pet and issues its queue number, in one transaction.
/// <para>
/// The two halves cannot be allowed to come apart. A checked-in appointment with no ticket is a
/// patient invisible to the queue board; a ticket whose appointment never moved is a number nobody
/// is waiting on. Both writes commit together or neither does.
/// </para>
/// <para>
/// <b>Who may call this is unusual and deliberate.</b> The contract lists the scope as
/// <c>appointment:checkin</c> yet also says customers check themselves in, and customers hold no
/// staff scopes at all. So the route carries no scope attribute and the decision is made here:
/// staff are admitted by <c>appointment:checkin</c> and their branch, customers by care of the
/// pet. Recorded in <c>docs/assumptions.md</c>.
/// </para>
/// </summary>
public sealed class CheckInAppointmentHandler(
    IAppointmentRepository appointments,
    IQueueTicketRepository tickets,
    IQueueNumberGenerator numbers,
    IQueueEventStream events,
    IClinicSettingsStore settings,
    AppointmentAccess access,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<CheckInResultDto> HandleAsync(
        CheckInAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var serviceDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, clock.ClinicTimeZone).DateTime);

        Domain.Appointments.Appointment? admitted = null;
        QueueTicket? ticket = null;

        // Everything is inside the transaction, including the load: a retry resets the change
        // tracker, so an appointment fetched beforehand would be detached and its check-in lost
        // while the ticket still landed — see IUnitOfWork.
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var appointment = await GetAppointmentHandler.Load(appointments, command.AppointmentId, token);

            await access.AuthoriseAsync(appointment, Scopes.Appointment.CheckIn, token);

            var existing = await tickets.FindByAppointmentAsync(appointment.Id, token);
            if (existing is not null)
            {
                // Already admitted. Returning the ticket that exists is more useful than a
                // conflict: the counter's usual reason to press again is that the first response
                // was lost.
                (admitted, ticket) = (appointment, existing);
                return;
            }

            appointment.CheckIn(now);

            var queue = await settings.QueueAsync(token);
            var sequence = await numbers.NextAsync(appointment.BranchId, serviceDate, queue.ResetDaily, token);

            // The appointment type decides where the ticket starts: an emergency booked as one
            // enters the queue as one, without someone at the counter having to remember to raise it.
            var type = (await settings.AppointmentTypesAsync(token)).Find(appointment.TypeCode);

            ticket = QueueTicket.IssueForAppointment(
                appointment,
                sequence,
                serviceDate,
                now,
                queue.Format(sequence),
                type?.QueuePriority ?? QueuePriority.Normal,
                type is null ? null : $"ประเภทนัด: {type.Name}");

            tickets.Add(ticket);

            webhooks.Enqueue(
                WebhookEventTypes.AppointmentCheckedIn,
                WebhookPayloads.Appointment(appointment, queueTicketId: ticket.Id));
            admitted = appointment;
        }, cancellationToken);

        // After commit, so the board is announced only once it exists. A check-in that reached the
        // queue is exactly the event a waiting-room screen needs.
        events.Publish(new QueueEvent(
            QueueEventTypes.TicketUpdated, ticket!.BranchId, ticket.Id, ticket.CustomerId));

        return Result(admitted!, ticket);
    }

    private static CheckInResultDto Result(Domain.Appointments.Appointment appointment, QueueTicket ticket) =>
        new(AppointmentDto.From(appointment), QueueTicketDto.From(ticket));
}
