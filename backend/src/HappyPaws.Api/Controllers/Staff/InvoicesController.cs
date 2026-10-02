using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Billing;
using HappyPaws.Api.Http;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Billing.CreateInvoice;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Billing.GetInvoice;
using HappyPaws.Application.Billing.ListInvoices;
using HappyPaws.Application.Billing.VoidInvoice;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The bill itself: raise one, read one, find one, cancel one. The money that moves against it —
/// discounts, payments, refunds and the receipt — is in <see cref="InvoiceTransactionsController"/>,
/// which carries four different scopes.
/// <para>
/// Three of these four routes are <b>not in the API reference</b>. The contract's billing section is
/// six endpoints and every one is a <c>POST</c>: a cashier can create a bill, discount it, take
/// money against it and print it, and has no documented way to look at one. See
/// <c>docs/contract-extensions.md § 3</c> for <c>void</c> and <c>§ 7</c> for the two reads.
/// </para>
/// </summary>
[ApiController]
[Route("staff/invoices")]
public sealed class InvoicesController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/invoices</c> — "สร้างเองหรือถูกสร้างอัตโนมัติตอนปิดเคส".
    /// <para>
    /// The automatic half happens inside <c>POST /staff/consultations/{id}/close</c>. This is the
    /// manual one: a boarding night, a bag of food, a bill reissued after a void.
    /// </para>
    /// </summary>
    /// <param name="request">Customer, lines, and optionally the pet and case it belongs to.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost]
    [Idempotent]
    [RequireScope(Scopes.Invoice.Write)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceDto>> Create(
        [FromBody] CreateInvoiceRequest request,
        [FromServices] CreateInvoiceHandler handler,
        CancellationToken cancellationToken)
    {
        var invoice = await handler.HandleAsync(
            new CreateInvoiceCommand(
                request.CustomerId,
                request.Lines,
                request.PetId,
                request.ConsultationId,
                request.BranchId,
                request.Note),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(invoice.Version);

        return Created($"/staff/invoices/{invoice.Id}", invoice);
    }

    /// <summary>
    /// <c>GET /staff/invoices/{id}</c> — <b>not in the API reference</b>;
    /// see <c>docs/contract-extensions.md § 7</c>.
    /// </summary>
    /// <param name="id">Invoice id.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet("{id}")]
    [RequireScope(Scopes.Invoice.Write)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> Get(
        string id,
        [FromServices] GetInvoiceHandler handler,
        CancellationToken cancellationToken)
    {
        var invoice = await handler.HandleAsync(new GetInvoiceQuery(id), cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(invoice.Version);

        return Ok(invoice);
    }

    /// <summary>
    /// <c>GET /staff/invoices</c> — <b>not in the API reference</b>; the counter's working list.
    /// Newest first, filtered by status, branch, customer and date. See
    /// <c>docs/contract-extensions.md § 7</c>.
    /// </summary>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="status">Contract status name — <c>open</c>, <c>partially_paid</c>, and so on.</param>
    /// <param name="branch">One of the caller's branches.</param>
    /// <param name="customerId">One customer's bills.</param>
    /// <param name="from">Issued at or after. Inclusive.</param>
    /// <param name="to">Issued before. Exclusive.</param>
    /// <param name="limit">Page size, clamped to the shared paging bounds.</param>
    /// <param name="cursor">Opaque cursor from the previous page.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpGet]
    [RequireScope(Scopes.Invoice.Write)]
    [ProducesResponseType<CursorPage<InvoiceDto>>(StatusCodes.Status200OK)]
    public Task<CursorPage<InvoiceDto>> List(
        [FromServices] ListInvoicesHandler handler,
        [FromQuery] string? status,
        [FromQuery] string? branch,
        [FromQuery(Name = "customer_id")] string? customerId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new ListInvoicesQuery(
                status,
                branch,
                customerId,
                from,
                to,
                limit is 0 ? CursorPaging.DefaultLimit : limit,
                cursor),
            cancellationToken);

    /// <summary>
    /// <c>POST /staff/invoices/{id}/void</c> — <b>not in the API reference</b>.
    /// <para>
    /// <c>void</c> is drawn in the contract's invoice state machine with no endpoint that reaches
    /// it, exactly as <c>left</c> was for the queue. Only a bill that has taken no money can be
    /// voided; after that the correction is a refund. See <c>docs/contract-extensions.md § 3</c>.
    /// </para>
    /// </summary>
    /// <param name="id">Invoice id.</param>
    /// <param name="request">Why the bill is being cancelled.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("{id}/void")]
    [RequireScope(Scopes.Invoice.Write)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Void(
        string id,
        [FromBody] VoidInvoiceRequest request,
        [FromServices] VoidInvoiceHandler handler,
        CancellationToken cancellationToken)
    {
        var invoice = await handler.HandleAsync(
            new VoidInvoiceCommand(id, request.Reason, IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(invoice.Version);

        return Ok(invoice);
    }
}
