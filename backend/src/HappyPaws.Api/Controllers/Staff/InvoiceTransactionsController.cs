using HappyPaws.Api.Authorization;
using HappyPaws.Api.Contracts.Billing;
using HappyPaws.Api.Http;
using HappyPaws.Api.Idempotency;
using HappyPaws.Application.Billing.ApplyDiscount;
using HappyPaws.Application.Billing.CollectPayment;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Billing.IssueReceipt;
using HappyPaws.Application.Billing.RefundPayment;
using HappyPaws.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Controllers.Staff;

/// <summary>
/// The money that moves against a bill.
/// <para>
/// Four routes, four scopes, and that separation is the contract's rather than this project's:
/// <c>invoice:discount</c> reduces what is owed, <c>payment:collect</c> takes it,
/// <c>payment:refund</c> gives it back and <c>receipt:issue</c> prints the tax document. A cashier
/// holds <c>invoice:*, payment:*, receipt:issue</c>; approving a discount is deliberately not the
/// same permission as ringing one up.
/// </para>
/// </summary>
[ApiController]
[Route("staff/invoices/{id}")]
public sealed class InvoiceTransactionsController : ControllerBase
{
    /// <summary>
    /// <c>POST /staff/invoices/{id}/discounts</c> — "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ". Also where a
    /// coupon code (<c>coupon_code</c>) or loyalty points (<c>points</c>) are redeemed.
    /// </summary>
    /// <param name="id">Invoice id.</param>
    /// <param name="request">The amount or rate with the reason and approver, or a coupon code, or points.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("discounts")]
    [RequireScope(Scopes.Invoice.Discount)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceDto>> Discount(
        string id,
        [FromBody] ApplyDiscountRequest request,
        [FromServices] ApplyDiscountHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new ApplyDiscountCommand(
                id,
                request.Reason,
                request.ApprovedByStaffId,
                request.AmountSatang,
                request.Percent,
                IfMatch.Parse(Request.Headers.IfMatch),
                request.CouponCode,
                request.Points),
            cancellationToken));

    /// <summary>
    /// <c>POST /staff/invoices/{id}/payments</c> —
    /// "เงินสด บัตร PromptPay · แบ่งจ่ายหลายวิธีได้".
    /// <para>
    /// <c>Idempotency-Key</c> is required. This is the endpoint the header exists for: a double-tap
    /// at a busy counter would otherwise take a customer's money twice.
    /// </para>
    /// </summary>
    /// <param name="id">Invoice id.</param>
    /// <param name="request">One tender — method, amount, and the slip or transaction reference.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("payments")]
    [Idempotent]
    [RequireScope(Scopes.Payment.Collect)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    // 409 covers both refusals the aggregate can raise: a settled or voided bill, and an amount
    // larger than the balance. Neither is a malformed request, so neither is a 422.
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Pay(
        string id,
        [FromBody] CollectPaymentRequest request,
        [FromServices] CollectPaymentHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new CollectPaymentCommand(
                id,
                request.Method,
                request.AmountSatang,
                request.Reference,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken));

    /// <summary>
    /// <c>POST /staff/invoices/{id}/refunds</c> —
    /// "คืนเงินบางส่วนหรือทั้งใบ · ผูกเหตุผล".
    /// </summary>
    /// <param name="id">Invoice id.</param>
    /// <param name="request">Amount, reason, and optionally the tender being reversed.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("refunds")]
    [Idempotent]
    [RequireScope(Scopes.Payment.Refund)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Refund(
        string id,
        [FromBody] RefundPaymentRequest request,
        [FromServices] RefundPaymentHandler handler,
        CancellationToken cancellationToken) =>
        WithETag(await handler.HandleAsync(
            new RefundPaymentCommand(
                id,
                request.AmountSatang,
                request.Reason,
                request.Method,
                request.PaymentId,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken));

    /// <summary>
    /// <c>POST /staff/invoices/{id}/receipt</c> —
    /// "ออกใบเสร็จ/ใบกำกับภาษี · ส่งเข้า LINE".
    /// <para>
    /// <c>201</c> the first time, <c>200</c> afterwards. One invoice has one receipt, and asking
    /// again returns the document already issued rather than minting a second original — see
    /// <see cref="IssueReceiptHandler"/>.
    /// </para>
    /// </summary>
    /// <param name="id">Invoice id.</param>
    /// <param name="request">Tax-invoice details, when one is wanted.</param>
    /// <param name="handler">Resolved per request.</param>
    /// <param name="cancellationToken">Aborts with the request.</param>
    [HttpPost("receipt")]
    [RequireScope(Scopes.Receipt.Issue)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceDto>> Receipt(
        string id,
        [FromBody] IssueReceiptRequest request,
        [FromServices] IssueReceiptHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new IssueReceiptCommand(
                id,
                request.IsTaxInvoice,
                request.TaxId,
                request.LegalName,
                request.BillingAddress,
                IfMatch.Parse(Request.Headers.IfMatch)),
            cancellationToken);

        Response.Headers.ETag = IfMatch.ToETag(result.Invoice.Version);

        return result.Created
            ? Created($"/staff/invoices/{result.Invoice.Id}", result.Invoice)
            : Ok(result.Invoice);
    }

    private ActionResult<InvoiceDto> WithETag(InvoiceDto invoice)
    {
        Response.Headers.ETag = IfMatch.ToETag(invoice.Version);

        return Ok(invoice);
    }
}
