using HappyPaws.Application.Auditing;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Billing;

/// <summary>
/// Raises a one-line bill for something sold outside a visit — today, a package
/// (<c>POST /staff/packages</c>).
/// <para>
/// In Billing for the same reason <see cref="ConsultationBilling"/> is: how a bill is numbered,
/// shaped and audited is a billing rule. The caller runs it inside its own transaction, so the
/// thing sold and the bill for it land together.
/// </para>
/// <para>
/// The line is kind <c>other</c>. Revenue reports count it when it is paid, like every bill; the
/// services later performed under the package are billed at zero, so the money is counted once.
/// </para>
/// </summary>
public sealed class SaleBilling(
    IInvoiceRepository invoices,
    IDocumentNumberGenerator numbers,
    IWebhookOutbox webhooks,
    IAuditTrail audit)
{
    public async Task<Invoice> RaiseAsync(
        SaleBillRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var invoice = Invoice.Raise(
            await numbers.NextInvoiceNumberAsync(now, cancellationToken),
            request.BranchId,
            request.CustomerId,
            request.StaffId,
            now,
            request.PetId,
            note: request.Note);

        invoice.AddLine(InvoiceLineKind.Other, request.Description, 1, request.PriceSatang, request.SourceId);

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
                ["source"] = request.Source,
                ["source_id"] = request.SourceId,
            }));

        return invoice;
    }
}

/// <summary>What is being sold, to whom, where, and what the audit row calls the source.</summary>
public sealed record SaleBillRequest(
    string BranchId,
    string CustomerId,
    string? PetId,
    string StaffId,
    string Description,
    long PriceSatang,
    string SourceId,
    string Source,
    string? Note = null);
