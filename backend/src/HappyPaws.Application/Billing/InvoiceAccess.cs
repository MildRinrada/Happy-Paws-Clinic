using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Billing;

namespace HappyPaws.Application.Billing;

/// <summary>
/// Loads a bill and decides who may act on it.
/// <para>
/// The gate is the branch the bill was raised at. A cashier at Sukhumvit taking payment for a bill
/// raised at Thonglor is not a workflow this clinic has — the money goes into a different drawer
/// and reconciles against a different day's takings — and branch membership is the check that is
/// already applied to every other resource in the system.
/// </para>
/// </summary>
public sealed class InvoiceAccess(IInvoiceRepository invoices, BranchGuard branchGuard)
{
    public async Task<Invoice> ForStaffAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.FindByIdAsync(invoiceId, cancellationToken)
            ?? throw new NotFoundException("invoice", invoiceId);

        branchGuard.EnsureCanAccess(invoice.BranchId);

        return invoice;
    }

    /// <summary>
    /// The same bill, for a customer looking at their own.
    /// <para>
    /// Answers <c>404</c> rather than <c>403</c> for someone else's invoice. A customer has no way
    /// to know an id they were not given, and telling them that one exists is itself a disclosure —
    /// bill ids are guessable in aggregate even when a single one is not.
    /// </para>
    /// </summary>
    public async Task<Invoice> ForCustomerAsync(
        string invoiceId,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.FindByIdAsync(invoiceId, cancellationToken);

        return invoice is null || invoice.CustomerId != customerId
            ? throw new NotFoundException("invoice", invoiceId)
            : invoice;
    }
}
