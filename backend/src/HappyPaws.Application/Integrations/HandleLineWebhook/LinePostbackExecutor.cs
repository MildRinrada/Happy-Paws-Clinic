using HappyPaws.Application.Appointments;
using HappyPaws.Application.Appointments.AddToWaitingList;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Queueing;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Integrations;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Integrations.HandleLineWebhook;

/// <summary>What to say back after a button press, and the buttons to offer with it.</summary>
public sealed record LineReply(string Text, IReadOnlyList<LineQuickReply>? QuickReplies = null);

/// <summary>
/// Runs the contract's five chat buttons for a customer found by their LINE address.
/// <para>
/// <b>Every action re-checks the record against the customer</b> ("ทุกอันตรวจสิทธิ์ซ้ำฝั่ง
/// เซิร์ฟเวอร์ก่อนทำงานเสมอ"): an appointment must be for a pet this customer cares for, the same
/// rule <c>AppointmentAccess</c> applies to the portal, and an invoice must be theirs. The ids in a
/// button came from a message we sent, but they come back as a claim. A record that fails the
/// check is answered exactly like one that does not exist.
/// </para>
/// <para>
/// The portal's own handlers are not called, because they take the caller from an access token and
/// a webhook carries none. The two writes here are the same domain calls those handlers make —
/// <c>Confirm</c>, and <c>Cancel</c> with the cancellation policy's lateness and the waiting list —
/// so the rules stay on the entity, not copied into this class.
/// </para>
/// <para>
/// Cancelling asks first. A chat button is one careless tap from a scroll, and a cancelled visit
/// gives the time to someone else; the first tap answers "แน่ใจไหม" with a second button.
/// </para>
/// </summary>
public sealed class LinePostbackExecutor(
    IAppointmentRepository appointments,
    IPetAccessPolicy petAccess,
    IClinicSettingsStore settings,
    WaitingListNotifier waitingList,
    IQueueTicketRepository tickets,
    IInvoiceRepository invoices,
    ILineGateway line,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<LinePostbackExecutor> logger)
{
    public async Task<LineReply> ExecuteAsync(
        Customer? customer,
        LinePostbackData postback,
        CancellationToken cancellationToken = default)
    {
        if (customer is null)
        {
            return new LineReply(LineReplies.NotRegistered(line.PortalUrl));
        }

        return postback.Action switch
        {
            LinePostbackActions.ConfirmAppointment => await ConfirmAsync(customer, postback, cancellationToken),
            LinePostbackActions.CancelAppointment => await CancelAsync(customer, postback, cancellationToken),
            LinePostbackActions.RescheduleAppointment => new LineReply(LineReplies.Reschedule(line.PortalUrl)),
            LinePostbackActions.ViewQueue => await QueueAsync(customer, cancellationToken),
            LinePostbackActions.OpenInvoice => await InvoiceAsync(customer, postback, cancellationToken),
            _ => new LineReply(LineReplies.NotFound),
        };
    }

    private async Task<LineReply> ConfirmAsync(Customer customer, LinePostbackData postback, CancellationToken cancellationToken)
    {
        var appointment = await OwnAppointmentAsync(customer, postback, cancellationToken);

        if (appointment is null)
        {
            return new LineReply(LineReplies.NotFound);
        }

        try
        {
            appointment.Confirm(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new LineReply(LineReplies.Confirmed);
        }
        catch (StateConflictException)
        {
            return new LineReply(LineReplies.CannotConfirm);
        }
    }

    private async Task<LineReply> CancelAsync(Customer customer, LinePostbackData postback, CancellationToken cancellationToken)
    {
        var appointment = await OwnAppointmentAsync(customer, postback, cancellationToken);

        if (appointment is null)
        {
            return new LineReply(LineReplies.NotFound);
        }

        if (postback[LinePostbackData.Confirmed] != "1")
        {
            return new LineReply(
                LineReplies.AskCancel,
                [
                    new LineQuickReply(
                        LineReplies.ConfirmCancelLabel,
                        LinePostbackData.Build(
                            LinePostbackActions.CancelAppointment,
                            (LinePostbackData.AppointmentId, appointment.Id),
                            (LinePostbackData.Confirmed, "1"))),
                ]);
        }

        var now = clock.UtcNow;
        var wasHoldingTime = appointment.OccupiesSlot;

        try
        {
            // A customer cancelling, judged by the same policy as a cancellation from the portal.
            var isLate = (await settings.CancellationPolicyAsync(cancellationToken)).IsLate(now, appointment.StartsAt);

            // The portal account when there is one; otherwise the customer record, so "who
            // cancelled" still names the person rather than nobody.
            appointment.Cancel(now, customer.UserId ?? customer.Id, LineReplies.CancelReason, isLate);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (StateConflictException)
        {
            return new LineReply(LineReplies.CannotCancel);
        }

        logger.LogInformation("Appointment {AppointmentId} cancelled from LINE by customer {CustomerId}", appointment.Id, customer.Id);

        if (wasHoldingTime)
        {
            await waitingList.SlotFreedAsync(appointment, cancellationToken);
        }

        return new LineReply(LineReplies.Cancelled);
    }

    private async Task<LineReply> QueueAsync(Customer customer, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.NowInClinicTime().DateTime);
        var ticket = (await tickets.LiveForCustomerAsync(customer.Id, today, cancellationToken)).FirstOrDefault();

        if (ticket is null)
        {
            return new LineReply(LineReplies.NoQueue);
        }

        var ahead = await tickets.CountAheadOfAsync(ticket, cancellationToken);

        return new LineReply(LineReplies.Queue(ticket.Number, LineReplies.QueueStatus(ticket.Status), ahead));
    }

    private async Task<LineReply> InvoiceAsync(Customer customer, LinePostbackData postback, CancellationToken cancellationToken)
    {
        var invoice = postback[LinePostbackData.InvoiceId] is { Length: > 0 } invoiceId
            ? await invoices.FindByIdAsync(invoiceId, cancellationToken)
            : null;

        return invoice is null || invoice.CustomerId != customer.Id
            ? new LineReply(LineReplies.NotFound)
            : new LineReply(LineReplies.Invoice(invoice.Number, invoice.Total.ToString(), invoice.Balance.ToString(), line.PortalUrl));
    }

    /// <summary>The appointment the button names, if it is for a pet this customer cares for.</summary>
    private async Task<Domain.Appointments.Appointment?> OwnAppointmentAsync(
        Customer customer,
        LinePostbackData postback,
        CancellationToken cancellationToken)
    {
        if (postback[LinePostbackData.AppointmentId] is not { Length: > 0 } appointmentId)
        {
            return null;
        }

        var appointment = await appointments.FindByIdAsync(appointmentId, cancellationToken);

        return appointment is not null && await petAccess.IsCaredForByAsync(appointment.PetId, customer.Id, cancellationToken)
            ? appointment
            : null;
    }
}
