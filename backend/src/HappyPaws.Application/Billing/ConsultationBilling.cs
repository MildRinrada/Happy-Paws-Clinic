using HappyPaws.Application.Auditing;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Billing;

/// <summary>
/// Turns a closed case into a bill — the "ออกบิล" half of
/// <c>POST /staff/consultations/{id}/close</c>, and the <c>invoice.issued ฿1,460</c> line in the
/// contract's own worked example.
/// <para>
/// It lives in the Billing module rather than inside <c>CloseConsultationHandler</c> because the
/// rules about what may be charged and how a bill is shaped belong to Billing. The consultation
/// handler calls one method inside its transaction, so the record, the room, the appointment, the
/// follow-up and the invoice all land together or not at all.
/// </para>
/// <para>
/// <b>Only treatments are billed.</b> "เข้าบิลอัตโนมัติ" is written against
/// <c>POST /staff/consultations/{id}/treatments</c> and nowhere else, and it is the only part of a
/// case that carries a price: a <c>Prescription</c> records a drug, a dose and a quantity, with no
/// money on it anywhere. Medicine is charged when it is handed over — "ตัดสต๊อกเมื่อจ่ายจริง" —
/// which needs the Inventory module for its price. Inventing a price here would put a number on a
/// customer's bill that nothing in the system can justify.
/// </para>
/// </summary>
public sealed class ConsultationBilling(
    IInvoiceRepository invoices,
    IDocumentNumberGenerator numbers,
    IAuditTrail audit)
{
    /// <summary>
    /// Raises the bill for a case, or returns null when there is nothing to charge for.
    /// <para>
    /// A case with no treatments produces no invoice. A ฿0 bill is not a courtesy — it is a
    /// document the counter has to look at, a number in the day's takings and a row in every
    /// revenue report, all to say that nothing was charged.
    /// </para>
    /// <para>
    /// Idempotent through <see cref="IInvoiceRepository.FindByConsultationAsync"/>: a close that is
    /// retried after a timeout finds the bill it already raised instead of charging the customer a
    /// second time.
    /// </para>
    /// </summary>
    public async Task<Invoice?> RaiseAsync(
        Consultation consultation,
        string staffId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (consultation.Treatments.Count == 0)
        {
            return null;
        }

        var existing = await invoices.FindByConsultationAsync(consultation.Id, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var invoice = Invoice.Raise(
            await numbers.NextInvoiceNumberAsync(now, cancellationToken),
            consultation.BranchId,
            consultation.CustomerId,
            staffId,
            now,
            consultation.PetId,
            consultation.Id);

        foreach (var treatment in consultation.Treatments.OrderBy(treatment => treatment.PerformedAt))
        {
            invoice.AddLine(
                treatment.ServiceId is { Length: > 0 } ? InvoiceLineKind.Service : InvoiceLineKind.Treatment,
                treatment.Name,
                treatment.Quantity,
                treatment.UnitPriceSatang,
                treatment.Id);
        }

        invoices.Add(invoice);

        audit.Record(new AuditEvent(
            AuditActions.InvoiceIssued,
            AuditResources.Invoice,
            invoice.Id,
            invoice.PetId,
            invoice.BranchId,
            new Dictionary<string, object?>
            {
                ["number"] = invoice.Number,
                ["total_satang"] = invoice.Total.Satang,
                ["consultation_id"] = consultation.Id,
                ["source"] = "consultation_close",
            }));

        return invoice;
    }
}
