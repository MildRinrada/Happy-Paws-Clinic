using HappyPaws.Application.Billing.Dtos;

namespace HappyPaws.Application.Billing.GetInvoice;

/// <summary>
/// <c>GET /staff/invoices/{id}</c> — <b>not in the API reference</b>; see
/// <c>docs/contract-extensions.md § 7</c>.
/// <para>
/// The billing section of the contract is six endpoints and every one of them is a <c>POST</c>. A
/// cashier can therefore create a bill, discount it, take money against it and print it — and has
/// no way to look at it. The id is only in the response body of whichever call last touched it, so
/// a bill raised by the vet closing a case at 11:00 is unreachable by the counter at 11:05.
/// </para>
/// </summary>
public sealed record GetInvoiceQuery(string InvoiceId);

public sealed class GetInvoiceHandler(InvoiceAccess access)
{
    public async Task<InvoiceDto> HandleAsync(
        GetInvoiceQuery query,
        CancellationToken cancellationToken = default) =>
        InvoiceDto.From(await access.ForStaffAsync(query.InvoiceId, cancellationToken));
}
