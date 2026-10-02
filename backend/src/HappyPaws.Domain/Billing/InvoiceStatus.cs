namespace HappyPaws.Domain.Billing;

/// <summary>
/// Invoice state machine from the contract:
/// <c>open → partially_paid → paid → refunded</c>, with <c>void</c> as an exit.
/// </summary>
public enum InvoiceStatus
{
    Open,
    PartiallyPaid,
    Paid,
    Refunded,
    Void,
}
