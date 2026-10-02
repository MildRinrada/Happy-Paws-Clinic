using HappyPaws.Application.Billing.CreateInvoice;

namespace HappyPaws.Api.Contracts.Billing;

/// <summary>
/// Request bodies for the billing endpoints.
/// <para>
/// As everywhere else in this contract, the reference states each endpoint's behaviour but not its
/// schema. Field names follow the words the document itself uses — "เงินสด บัตร PromptPay",
/// "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ", "ใบเสร็จ/ใบกำกับภาษี". See <c>docs/assumptions.md § 15</c>.
/// </para>
/// </summary>
public sealed record CreateInvoiceRequest(
    string CustomerId,
    IReadOnlyList<InvoiceLineInput> Lines,
    string? PetId = null,
    string? ConsultationId = null,
    string? BranchId = null,
    string? Note = null);

/// <summary>
/// <c>POST /staff/invoices/{id}/discounts</c>. Exactly one of <c>amount_satang</c> and
/// <c>percent</c>; <c>approved_by_staff_id</c> is required and is not assumed to be the caller.
/// </summary>
public sealed record ApplyDiscountRequest(
    string Reason,
    string ApprovedByStaffId,
    long? AmountSatang = null,
    decimal? Percent = null);

/// <summary>
/// <c>POST /staff/invoices/{id}/payments</c>. One call is one tender — "แบ่งจ่ายหลายวิธีได้" is
/// two calls, not a list here, so each has its own idempotency key and its own reference.
/// </summary>
public sealed record CollectPaymentRequest(string Method, long AmountSatang, string? Reference = null);

/// <summary><c>POST /staff/invoices/{id}/refunds</c>. The reason is required — "ผูกเหตุผล".</summary>
public sealed record RefundPaymentRequest(
    long AmountSatang,
    string Reason,
    string? Method = null,
    string? PaymentId = null);

/// <summary>
/// <c>POST /staff/invoices/{id}/receipt</c>. An empty body issues a plain ใบเสร็จ;
/// <c>is_tax_invoice</c> with a <c>tax_id</c> issues a ใบกำกับภาษี.
/// </summary>
public sealed record IssueReceiptRequest(
    bool IsTaxInvoice = false,
    string? TaxId = null,
    string? LegalName = null,
    string? BillingAddress = null);

/// <summary><c>POST /staff/invoices/{id}/void</c> — not in the API reference.</summary>
public sealed record VoidInvoiceRequest(string Reason);
