using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Pagination;

namespace HappyPaws.Application.Billing.ListInvoices;

/// <summary>
/// <c>GET /staff/invoices</c> — <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 7</c>.
/// <para>
/// The counter's working list: today's bills at this branch, narrowed by status. Without it the
/// only way to find an unpaid invoice is to already know its id, which nobody at a cash desk does.
/// </para>
/// </summary>
public sealed record ListInvoicesQuery(
    string? Status = null,
    string? BranchId = null,
    string? CustomerId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// Lists bills, newest first.
/// <para>
/// The branch restriction is passed to the repository rather than applied here, so the filter and
/// the keyset comparison are built in the same expression tree. A handler that filtered afterwards
/// would page over rows it then discarded and hand back short pages — or, worse, a
/// <c>next_cursor</c> pointing into another branch's bills.
/// </para>
/// </summary>
public sealed class ListInvoicesHandler(IInvoiceRepository invoices, BranchGuard branchGuard)
{
    public async Task<CursorPage<InvoiceDto>> HandleAsync(
        ListInvoicesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.BranchId is { Length: > 0 } requested)
        {
            branchGuard.EnsureCanAccess(requested);
        }

        var filter = new InvoiceListFilter(
            branchGuard.VisibleBranchIds,
            query.BranchId,
            BillingWire.ParseStatus(query.Status),
            query.CustomerId,
            query.From,
            query.To);

        var page = await invoices.ListAsync(
            filter, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);

        return new CursorPage<InvoiceDto>([.. page.Data.Select(InvoiceDto.From)], page.NextCursor);
    }
}
