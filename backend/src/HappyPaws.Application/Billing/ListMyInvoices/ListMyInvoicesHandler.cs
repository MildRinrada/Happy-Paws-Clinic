using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Pagination;

namespace HappyPaws.Application.Billing.ListMyInvoices;

/// <summary>
/// <c>GET /portal/invoices</c> — "ลูกค้าดูบิลย้อนหลังและจ่ายค้างชำระ".
/// <para>
/// <c>Unpaid</c> true is the "จ่ายค้างชำระ" half — what the customer still owes; false is the
/// history. Two lists in the portal, one flag here.
/// </para>
/// </summary>
public sealed record ListMyInvoicesQuery(
    bool Unpaid = false,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// The customer's own bills.
/// <para>
/// Scoped by <c>customer_id</c> from the token at the database, so — as with
/// <c>GET /portal/appointments</c> — the query cannot express "someone else's invoices" at all.
/// </para>
/// <para>
/// "จ่ายค้างชำระ" is the half of this row the platform cannot finish. Listing what is outstanding
/// works; a customer paying it from their phone needs the payment gateway that
/// <c>PUT /staff/integrations/payment</c> configures and nothing implements yet. The contract has
/// no <c>POST /portal/invoices/{id}/payments</c>, so no endpoint was invented for it — the gap is
/// recorded rather than filled.
/// </para>
/// </summary>
public sealed class ListMyInvoicesHandler(IInvoiceRepository invoices, ICurrentUser currentUser)
{
    public async Task<CursorPage<InvoiceDto>> HandleAsync(
        ListMyInvoicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await invoices.ListForCustomerAsync(
            currentUser.RequireCustomerId(),
            query.Unpaid,
            new CursorPageRequest(query.Limit, query.Cursor),
            cancellationToken);

        return new CursorPage<InvoiceDto>([.. page.Data.Select(InvoiceDto.From)], page.NextCursor);
    }
}
