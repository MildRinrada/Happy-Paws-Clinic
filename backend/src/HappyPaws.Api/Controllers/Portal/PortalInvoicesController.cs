using HappyPaws.Api.Authorization;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Billing.ListMyInvoices;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Portal;

/// <summary>
/// The customer's own bills — <c>/portal/invoices</c>.
/// </summary>
[ApiController]
[Route("portal/invoices")]
[RequireScope(Scopes.Portal)]
public sealed class PortalInvoicesController : ControllerBase
{
    /// <summary>
    /// <c>GET /portal/invoices</c> — "ลูกค้าดูบิลย้อนหลังและจ่ายค้างชำระ".
    /// <para>
    /// <c>?unpaid=true</c> is the "จ่ายค้างชำระ" list. Paying from the portal is the half of that
    /// row this platform cannot yet finish: it needs the payment gateway that
    /// <c>PUT /staff/integrations/payment</c> configures, and the contract declares no
    /// <c>POST /portal/invoices/{id}/payments</c> to invent one behind.
    /// </para>
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="unpaid">True for what is still owed; false (the default) for the history.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [ProducesResponseType<CursorPage<InvoiceDto>>(StatusCodes.Status200OK)]
    public Task<CursorPage<InvoiceDto>> List(
        [FromServices] ListMyInvoicesHandler handler,
        [FromQuery] bool unpaid,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListMyInvoicesQuery(unpaid, limit is 0 ? CursorPaging.DefaultLimit : limit, cursor),
            cancellationToken);
}
