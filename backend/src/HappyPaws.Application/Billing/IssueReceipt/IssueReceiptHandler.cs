using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Billing.IssueReceipt;

/// <summary>
/// <c>POST /staff/invoices/{id}/receipt</c> —
/// "ออกใบเสร็จ/ใบกำกับภาษี · ส่งเข้า LINE".
/// </summary>
public sealed record IssueReceiptCommand(
    string InvoiceId,
    bool IsTaxInvoice = false,
    string? TaxId = null,
    string? LegalName = null,
    string? BillingAddress = null,
    uint? IfMatchVersion = null);

/// <summary>The bill with its receipt, and whether this call is what created it.</summary>
public sealed record IssueReceiptResult(InvoiceDto Invoice, bool Created);

/// <summary>
/// Issues the customer's copy and sends it.
/// <para>
/// Asking twice is not an error and does not mint a second document — the aggregate returns the one
/// already issued and <see cref="IssueReceiptResult.Created"/> is false, which the controller
/// answers as <c>200</c> instead of <c>201</c>. A duplicate original ใบกำกับภาษี is a problem for
/// the clinic's books, so the safe behaviour is the idempotent one, and a counter that pressed the
/// button twice still gets the document it wanted.
/// </para>
/// <para>
/// "ส่งเข้า LINE" happens after the transaction commits and cannot fail the request. The receipt
/// exists whether or not the push arrived; the customer is standing at the counter holding it.
/// </para>
/// </summary>
public sealed class IssueReceiptHandler(
    InvoiceAccess access,
    IDocumentNumberGenerator numbers,
    INotificationDispatcher notifications,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<IssueReceiptHandler> logger)
{
    public async Task<IssueReceiptResult> HandleAsync(
        IssueReceiptCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;

        Invoice? updated = null;
        var created = false;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var invoice = await access.ForStaffAsync(command.InvoiceId, token);

            ConcurrencyGuard.Ensure(invoice, command.IfMatchVersion);

            if (invoice.Receipt is not null)
            {
                updated = invoice;

                return;
            }

            var receipt = invoice.IssueReceipt(
                await numbers.NextReceiptNumberAsync(now, token),
                staffId,
                now,
                command.IsTaxInvoice,
                command.TaxId,
                command.LegalName,
                command.BillingAddress);

            audit.Record(new AuditEvent(
                AuditActions.ReceiptIssued,
                AuditResources.Invoice,
                invoice.Id,
                invoice.PetId,
                invoice.BranchId,
                new Dictionary<string, object?>
                {
                    ["receipt_id"] = receipt.Id,
                    ["number"] = receipt.Number,
                    ["is_tax_invoice"] = receipt.IsTaxInvoice,
                    ["amount_satang"] = receipt.AmountSatang,
                }));

            (updated, created) = (invoice, true);
        }, cancellationToken);

        if (created)
        {
            await NotifyAsync(updated!, cancellationToken);
        }

        return new IssueReceiptResult(InvoiceDto.From(updated!), created);
    }

    private async Task NotifyAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["invoice_id"] = invoice.Id,
            ["invoice_number"] = invoice.Number,
            ["receipt_number"] = invoice.Receipt!.Number,
            ["amount"] = invoice.Receipt.Amount.ToString(),
        };

        try
        {
            await notifications.SendAsync(
                new NotificationRequest(invoice.CustomerId, NotificationTemplates.ReceiptIssued, data) { BranchId = invoice.BranchId },
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Receipt notification failed for invoice {InvoiceId} (customer {CustomerId})",
                invoice.Id,
                invoice.CustomerId);
        }
    }
}
