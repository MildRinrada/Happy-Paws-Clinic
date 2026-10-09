using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Billing;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Laboratory;
using HappyPaws.Domain.MedicalRecords;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.Prescriptions;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Application.Integrations.Webhooks;

/// <summary>
/// The <c>data</c> of every webhook event, and so <b>everything that leaves the system</b> by
/// webhook. One file, so it can be reviewed in one sitting; documented row by row in
/// <c>docs/features/integrations.md</c>.
/// <para>
/// <b>Thin events.</b> Each carries ids, times, statuses and amounts: enough for a receiver to
/// react, and to fetch more through the API with a key that holds the right scopes. No health data
/// travels: no diagnosis, complaint, finding, test value, drug name, dose, reason for a visit or
/// note. No contact data either: no name, phone, email or address of a customer. A receiver that
/// needs those asks the API, where scopes, branches and the audit log apply.
/// </para>
/// </summary>
public static class WebhookPayloads
{
    /// <summary><c>appointment.created</c>, <c>.rescheduled</c>, <c>.cancelled</c>, <c>.no_show</c>, <c>.checked_in</c>.</summary>
    public sealed record AppointmentData(
        string AppointmentId,
        string Status,
        string BranchId,
        string CustomerId,
        string PetId,
        string ServiceId,
        string? StaffId,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        string? QueueTicketId = null,
        DateTimeOffset? PreviousStartsAt = null);

    /// <summary><c>queue.ticket.called</c>. The room is an id, the number is what the screen shows.</summary>
    public sealed record QueueTicketData(
        string TicketId,
        string Number,
        string BranchId,
        string? AppointmentId,
        string PetId,
        string CustomerId,
        string? StaffId,
        string? RoomId,
        DateTimeOffset? CalledAt);

    /// <summary><c>consultation.closed</c>. That a visit ended, not what was found.</summary>
    public sealed record ConsultationData(
        string ConsultationId,
        string BranchId,
        string PetId,
        string CustomerId,
        string? AppointmentId,
        string VetStaffId,
        string? MedicalRecordId,
        DateTimeOffset? ClosedAt);

    /// <summary><c>record.signed</c>. The record's id and who signed it; never its content.</summary>
    public sealed record RecordData(
        string RecordId,
        string ConsultationId,
        string BranchId,
        string PetId,
        string CustomerId,
        string SignedByStaffId,
        DateTimeOffset SignedAt);

    /// <summary><c>lab.result.ready</c>. That results exist; no test names or values.</summary>
    public sealed record LabResultData(
        string LabRequestId,
        string BranchId,
        string PetId,
        string? ConsultationId,
        DateTimeOffset? ResultedAt);

    /// <summary><c>prescription.dispensed</c>. No drug, dose or quantity.</summary>
    public sealed record PrescriptionData(
        string PrescriptionId,
        string ConsultationId,
        string PetId,
        string? BranchId,
        DateTimeOffset? DispensedAt);

    /// <summary><c>invoice.issued</c>. The total, not the lines (a line can name a drug or a procedure).</summary>
    public sealed record InvoiceData(
        string InvoiceId,
        string Number,
        string BranchId,
        string CustomerId,
        string? PetId,
        string? ConsultationId,
        string Status,
        long TotalSatang,
        long BalanceSatang,
        DateTimeOffset IssuedAt);

    /// <summary><c>payment.succeeded</c>, one per payment recorded. The slip reference is left out.</summary>
    public sealed record PaymentData(
        string PaymentId,
        string InvoiceId,
        string InvoiceNumber,
        string BranchId,
        string CustomerId,
        string Method,
        long AmountSatang,
        string InvoiceStatus,
        long BalanceSatang,
        DateTimeOffset CollectedAt);

    /// <summary><c>payment.refunded</c>. Without the reason, which is free text.</summary>
    public sealed record RefundData(
        string RefundId,
        string InvoiceId,
        string InvoiceNumber,
        string BranchId,
        string CustomerId,
        string? PaymentId,
        string Method,
        long AmountSatang,
        string InvoiceStatus,
        DateTimeOffset RefundedAt);

    /// <summary>
    /// <c>inventory.low_stock</c>. Sent once when an item's usable stock at a branch falls from above
    /// its reorder level to at or below it (or to nothing), not on every dispense after that.
    /// </summary>
    public sealed record LowStockData(
        string ItemId,
        string BranchId,
        int Available,
        int ReorderLevel);

    /// <summary><c>customer.created</c>. The id only: a receiver that needs the name asks the API.</summary>
    public sealed record CustomerData(string CustomerId, DateTimeOffset CreatedAt);

    /// <summary><c>pet.created</c>. Species is kept (it routes a cat clinic's work); the name is not.</summary>
    public sealed record PetData(string PetId, string CustomerId, string Species, DateTimeOffset CreatedAt);

    public static AppointmentData Appointment(Appointment appointment, string? queueTicketId = null, DateTimeOffset? previousStartsAt = null) =>
        new(
            appointment.Id,
            AppointmentDto.ToWireStatus(appointment.Status),
            appointment.BranchId,
            appointment.CustomerId,
            appointment.PetId,
            appointment.ServiceId,
            appointment.StaffId,
            appointment.StartsAt,
            appointment.EndsAt,
            queueTicketId,
            previousStartsAt);

    public static QueueTicketData Ticket(QueueTicket ticket) =>
        new(
            ticket.Id,
            ticket.Number,
            ticket.BranchId,
            ticket.AppointmentId,
            ticket.PetId,
            ticket.CustomerId,
            ticket.StaffId,
            ticket.RoomId,
            ticket.CalledAt);

    public static ConsultationData Consultation(Consultation consultation) =>
        new(
            consultation.Id,
            consultation.BranchId,
            consultation.PetId,
            consultation.CustomerId,
            consultation.AppointmentId,
            consultation.VetStaffId,
            consultation.MedicalRecordId,
            consultation.ClosedAt);

    public static RecordData Record(MedicalRecord record) =>
        new(
            record.Id,
            record.ConsultationId,
            record.BranchId,
            record.PetId,
            record.CustomerId,
            record.SignedByStaffId,
            record.SignedAt);

    public static LabResultData LabResult(LabRequest request) =>
        new(request.Id, request.BranchId, request.PetId, request.ConsultationId, request.ResultedAt);

    public static PrescriptionData Dispensed(Prescription prescription, string? branchId) =>
        new(prescription.Id, prescription.ConsultationId, prescription.PetId, branchId, prescription.DispensedAt);

    public static InvoiceData Invoice(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.Number,
            invoice.BranchId,
            invoice.CustomerId,
            invoice.PetId,
            invoice.ConsultationId,
            BillingWire.ToWire(invoice.Status),
            invoice.Total.Satang,
            invoice.Balance.Satang,
            invoice.IssuedAt);

    public static PaymentData Payment(Invoice invoice, InvoicePayment payment) =>
        new(
            payment.Id,
            invoice.Id,
            invoice.Number,
            invoice.BranchId,
            invoice.CustomerId,
            BillingWire.ToWire(payment.Method),
            payment.AmountSatang,
            BillingWire.ToWire(invoice.Status),
            invoice.Balance.Satang,
            payment.CollectedAt);

    public static RefundData Refund(Invoice invoice, InvoiceRefund refund) =>
        new(
            refund.Id,
            invoice.Id,
            invoice.Number,
            invoice.BranchId,
            invoice.CustomerId,
            refund.PaymentId,
            BillingWire.ToWire(refund.Method),
            refund.AmountSatang,
            BillingWire.ToWire(invoice.Status),
            refund.RefundedAt);

    public static CustomerData Customer(Customer customer, DateTimeOffset createdAt) => new(customer.Id, createdAt);

    public static PetData Pet(Pet pet, DateTimeOffset createdAt) => new(pet.Id, pet.PrimaryCustomerId, pet.Species, createdAt);
}
