using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Common;

namespace HappyPaws.Application.Billing;

/// <summary>
/// Persistence port for the invoice aggregate.
/// <para>
/// <see cref="FindByIdAsync"/> returns the bill with its lines, discounts, payments, refunds and
/// receipt loaded. As with the consultation, partial loading is not offered: every guard in
/// <see cref="Invoice"/> is arithmetic over those collections, so a root that came back without its
/// payments would happily accept a second full settlement of a bill that was already paid.
/// </para>
/// </summary>
public interface IInvoiceRepository
{
    void Add(Invoice invoice);

    Task<Invoice?> FindByIdAsync(string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>The bill already raised for a case, if any. Keeps closing a case idempotent.</summary>
    Task<Invoice?> FindByConsultationAsync(string consultationId, CancellationToken cancellationToken = default);

    Task<CursorPage<Invoice>> ListAsync(
        InvoiceListFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /portal/invoices</c> — "ลูกค้าดูบิลย้อนหลังและจ่ายค้างชำระ".
    /// Scoped by customer at the database, so the query cannot express someone else's bills.
    /// </summary>
    Task<CursorPage<Invoice>> ListForCustomerAsync(
        string customerId,
        bool unpaidOnly,
        CursorPageRequest page,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// "ยอดค้างชำระ" on <c>GET /staff/customers/{id}</c> — what this customer still owes.
    /// <para>
    /// Lives here rather than in the Customers module because <i>which statuses still count as
    /// owed</i> is a billing rule. A second implementation of it beside the customer record would
    /// be a second answer, free to drift from the one the invoice list shows.
    /// </para>
    /// </summary>
    Task<Money> OutstandingForCustomerAsync(string customerId, CancellationToken cancellationToken = default);
}

/// <summary>
/// What a staff caller may narrow the invoice list by.
/// </summary>
/// <param name="VisibleBranchIds">
/// The caller's branches. Applied by the repository rather than the handler so that no list query
/// can be written that forgets it.
/// </param>
/// <param name="BranchId">A single branch within those, when the caller asked for one.</param>
/// <param name="Status">Contract status name — <c>open</c>, <c>paid</c>, and so on.</param>
/// <param name="CustomerId">One customer's bills.</param>
/// <param name="From">Issued at or after. Inclusive.</param>
/// <param name="To">Issued before. Exclusive, so a day range does not double-count midnight.</param>
public sealed record InvoiceListFilter(
    IReadOnlyCollection<string>? VisibleBranchIds = null,
    string? BranchId = null,
    InvoiceStatus? Status = null,
    string? CustomerId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);

/// <summary>
/// Allocates the next invoice or receipt number.
/// <para>
/// Its own port for the same reason the queue's is: "next number" is a concurrency problem, not a
/// domain rule, and two cashiers ringing up at the same second must not both be handed
/// <c>INV-202608-0042</c>. Only the database can settle that, and the implementation takes a
/// transaction-scoped advisory lock to do it.
/// </para>
/// <para>
/// Must be called inside the caller's transaction. A bill that rolls back must not burn a number:
/// a gap in a tax document sequence is something the clinic has to explain.
/// </para>
/// </summary>
public interface IDocumentNumberGenerator
{
    /// <summary>e.g. <c>INV-202608-0042</c>.</summary>
    Task<string> NextInvoiceNumberAsync(DateTimeOffset issuedAt, CancellationToken cancellationToken = default);

    /// <summary>e.g. <c>RCP-202608-0042</c>.</summary>
    Task<string> NextReceiptNumberAsync(DateTimeOffset issuedAt, CancellationToken cancellationToken = default);
}
