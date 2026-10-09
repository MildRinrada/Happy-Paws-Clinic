using HappyPaws.Application.Auditing;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Application.Prescriptions;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.Application.Billing;

/// <summary>Where a dispensed drug's charge went.</summary>
public enum MedicineChargePlacement
{
    /// <summary>Added to the case's own invoice, raised now or still open.</summary>
    Invoiced,

    /// <summary>The case is still open: the charge waits on the prescription for the invoice its close raises.</summary>
    OnClose,

    /// <summary>The case's invoice had already taken money, or was voided, so the medicine got a bill of its own.</summary>
    SeparateInvoice,
}

/// <summary>What a dispense charged, and on which bill.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>invoice</c>: The bill it landed on; null while it waits for the case to close.</item>
/// </list>
/// </remarks>
public sealed record MedicineCharge(
    MedicineChargePlacement Placement,
    long UnitPriceSatang,
    long AmountSatang,
    Invoice? Invoice);

/// <summary>
/// Puts dispensed medicine on a bill — "ตัดสต๊อกเมื่อจ่ายจริง", charged at the price the inventory
/// item carries at that moment, captured on the prescription.
/// <para>
/// <b>The same bill as the treatments.</b> Medicine is a <c>medicine</c> line on the case's invoice,
/// beside the treatments, which is what lets the service report set it against the service it was
/// sold with. Which path that takes depends on where the case is:
/// </para>
/// <list type="bullet">
/// <item>Case still open: nothing to write yet. The charge waits on the prescription, and
/// <see cref="ConsultationBilling"/> adds it when the close raises the invoice.</item>
/// <item>Case closed, invoice still <c>open</c>: a line is added to it. The bill's version moves, so a
/// cashier holding the old one gets <c>409 version_conflict</c> instead of settling the smaller total.</item>
/// <item>Case closed with no invoice (it had nothing else to charge): its invoice is raised now.</item>
/// <item>Case closed and its invoice has taken money, or was voided: that bill is an agreed figure and
/// is never changed after the fact (<see cref="Invoice.AddLine"/> refuses). The medicine gets a bill of
/// its own, for the same customer and pet, saying which case it belongs to.</item>
/// </list>
/// <para>
/// <b>Serialised with the close.</b> Both this and the close take the case's billing lock before
/// reading anything, and read the case and its prescriptions fresh after taking it, so a dispense and
/// a close racing each other cannot both conclude that the other one will bill the medicine.
/// </para>
/// </summary>
public sealed class MedicineBilling(
    IInvoiceRepository invoices,
    IPrescriptionRepository prescriptions,
    IDocumentNumberGenerator numbers,
    IWebhookOutbox webhooks,
    IAuditTrail audit)
{
    /// <summary>Call inside the dispense's transaction, after the charge is recorded on the prescription.</summary>
    public async Task<MedicineCharge?> ChargeAsync(
        PrescriptionInContext found,
        string staffId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var prescription = found.Prescription;

        if (prescription.ChargedUnitPriceSatang is not { } unitPrice)
        {
            return null;
        }

        var amount = prescription.ChargedSatang;

        await invoices.LockConsultationAsync(prescription.ConsultationId, cancellationToken);

        if (await prescriptions.IsCaseOpenAsync(prescription.ConsultationId, cancellationToken))
        {
            return new MedicineCharge(MedicineChargePlacement.OnClose, unitPrice, amount, null);
        }

        var existing = await invoices.FindByConsultationAsync(prescription.ConsultationId, cancellationToken);

        if (existing is { Status: InvoiceStatus.Open })
        {
            AddTo(existing, prescription);
            invoices.MarkChanged(existing);

            audit.Record(new AuditEvent(
                AuditActions.InvoiceLineAdded,
                AuditResources.Invoice,
                existing.Id,
                existing.PetId,
                existing.BranchId,
                new Dictionary<string, object?>
                {
                    ["number"] = existing.Number,
                    ["kind"] = BillingWire.ToWire(InvoiceLineKind.Medicine),
                    ["prescription_id"] = prescription.Id,
                    ["amount_satang"] = amount,
                    ["total_satang"] = existing.Total.Satang,
                }));

            return new MedicineCharge(MedicineChargePlacement.Invoiced, unitPrice, amount, existing);
        }

        // No bill yet: this is the case's bill. A bill that took money or was voided: a new one, not
        // linked by consultation_id (one case, one linked bill), but saying in its note which case.
        var linked = existing is null;
        var invoice = Invoice.Raise(
            await numbers.NextInvoiceNumberAsync(now, cancellationToken),
            found.BranchId,
            found.CustomerId,
            staffId,
            now,
            prescription.PetId,
            linked ? prescription.ConsultationId : null,
            linked ? null : $"ค่ายาที่จ่ายหลังบิล {existing!.Number} ของเคส {prescription.ConsultationId} ปิดยอดแล้ว");

        AddTo(invoice, prescription);
        invoices.Add(invoice);
        webhooks.Enqueue(WebhookEventTypes.InvoiceIssued, WebhookPayloads.Invoice(invoice));

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
                ["consultation_id"] = prescription.ConsultationId,
                ["prescription_id"] = prescription.Id,
                ["source"] = "dispense",
            }));

        return new MedicineCharge(
            linked ? MedicineChargePlacement.Invoiced : MedicineChargePlacement.SeparateInvoice,
            unitPrice,
            amount,
            invoice);
    }

    /// <summary>The line a dispensed prescription becomes. Shared with the close, so both write the same row.</summary>
    internal static void AddTo(Invoice invoice, Prescription prescription) =>
        invoice.AddLine(
            InvoiceLineKind.Medicine,
            prescription.DrugName,
            prescription.Quantity,
            prescription.ChargedUnitPriceSatang!.Value,
            prescription.Id);
}
