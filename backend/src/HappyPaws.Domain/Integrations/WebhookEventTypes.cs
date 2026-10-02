namespace HappyPaws.Domain.Integrations;

/// <summary>
/// The outbound webhook events the contract commits to emitting.
/// Signed with HMAC-SHA256 in <c>X-Signature</c>; retried 5 times with exponential
/// backoff within 24 hours. Subscriptions are managed via <c>POST /staff/webhooks</c>
/// and deliveries inspected via <c>GET /staff/webhooks/{id}/deliveries</c>.
/// </summary>
public static class WebhookEventTypes
{
    public const string AppointmentCreated = "appointment.created";
    public const string AppointmentRescheduled = "appointment.rescheduled";
    public const string AppointmentCancelled = "appointment.cancelled";
    public const string AppointmentNoShow = "appointment.no_show";
    public const string AppointmentCheckedIn = "appointment.checked_in";
    public const string QueueTicketCalled = "queue.ticket.called";
    public const string ConsultationClosed = "consultation.closed";
    public const string RecordSigned = "record.signed";
    public const string LabResultReady = "lab.result.ready";
    public const string PrescriptionDispensed = "prescription.dispensed";
    public const string InvoiceIssued = "invoice.issued";
    public const string PaymentSucceeded = "payment.succeeded";
    public const string PaymentRefunded = "payment.refunded";
    public const string InventoryLowStock = "inventory.low_stock";
    public const string CustomerCreated = "customer.created";
    public const string PetCreated = "pet.created";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        AppointmentCreated,
        AppointmentRescheduled,
        AppointmentCancelled,
        AppointmentNoShow,
        AppointmentCheckedIn,
        QueueTicketCalled,
        ConsultationClosed,
        RecordSigned,
        LabResultReady,
        PrescriptionDispensed,
        InvoiceIssued,
        PaymentSucceeded,
        PaymentRefunded,
        InventoryLowStock,
        CustomerCreated,
        PetCreated,
    };
}
