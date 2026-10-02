using HappyPaws.Domain.Billing;

namespace HappyPaws.Application.Billing.Dtos;

/// <summary>
/// A bill as the API returns it.
/// <para>
/// Every total is sent explicitly rather than left for the client to add up. Two clients summing
/// the same lines is two chances to disagree with the server about what a customer owes, and the
/// one place that disagreement surfaces is a counter with a queue behind it.
/// </para>
/// </summary>
public sealed record InvoiceDto(
    string Id,
    uint Version,
    string Number,
    string Status,
    string BranchId,
    string CustomerId,
    string? PetId,
    string? ConsultationId,
    DateTimeOffset IssuedAt,
    string IssuedByStaffId,
    string? Note,
    long SubtotalSatang,
    long DiscountSatang,
    long TotalSatang,
    long PaidSatang,
    long RefundedSatang,
    long BalanceSatang,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<InvoiceDiscountDto> Discounts,
    IReadOnlyList<InvoicePaymentDto> Payments,
    IReadOnlyList<InvoiceRefundDto> Refunds,
    ReceiptDto? Receipt,
    DateTimeOffset? VoidedAt,
    string? VoidReason)
{
    public static InvoiceDto From(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.Version,
            invoice.Number,
            BillingWire.ToWire(invoice.Status),
            invoice.BranchId,
            invoice.CustomerId,
            invoice.PetId,
            invoice.ConsultationId,
            invoice.IssuedAt,
            invoice.IssuedByStaffId,
            invoice.Note,
            invoice.Subtotal.Satang,
            invoice.DiscountTotal.Satang,
            invoice.Total.Satang,
            invoice.Paid.Satang,
            invoice.Refunded.Satang,
            invoice.Balance.Satang,
            [.. invoice.Lines.Select(InvoiceLineDto.From)],
            [.. invoice.Discounts.OrderBy(discount => discount.AppliedAt).Select(InvoiceDiscountDto.From)],
            [.. invoice.Payments.OrderBy(payment => payment.CollectedAt).Select(InvoicePaymentDto.From)],
            [.. invoice.Refunds.OrderBy(refund => refund.RefundedAt).Select(InvoiceRefundDto.From)],
            invoice.Receipt is null ? null : ReceiptDto.From(invoice.Receipt),
            invoice.VoidedAt,
            invoice.VoidReason);
}

public sealed record InvoiceLineDto(
    string Id,
    string Kind,
    string Description,
    int Quantity,
    long UnitPriceSatang,
    long LineTotalSatang,
    string? SourceId)
{
    public static InvoiceLineDto From(InvoiceLine line) =>
        new(
            line.Id,
            BillingWire.ToWire(line.Kind),
            line.Description,
            line.Quantity,
            line.UnitPriceSatang,
            line.LineTotal.Satang,
            line.SourceId);
}

public sealed record InvoiceDiscountDto(
    string Id,
    long AmountSatang,
    decimal? Percent,
    string Reason,
    string ApprovedByStaffId,
    string AppliedByStaffId,
    DateTimeOffset AppliedAt)
{
    public static InvoiceDiscountDto From(InvoiceDiscount discount) =>
        new(
            discount.Id,
            discount.AmountSatang,
            discount.Percent,
            discount.Reason,
            discount.ApprovedByStaffId,
            discount.AppliedByStaffId,
            discount.AppliedAt);
}

public sealed record InvoicePaymentDto(
    string Id,
    string Method,
    long AmountSatang,
    string? Reference,
    string CollectedByStaffId,
    DateTimeOffset CollectedAt)
{
    public static InvoicePaymentDto From(InvoicePayment payment) =>
        new(
            payment.Id,
            BillingWire.ToWire(payment.Method),
            payment.AmountSatang,
            payment.Reference,
            payment.CollectedByStaffId,
            payment.CollectedAt);
}

public sealed record InvoiceRefundDto(
    string Id,
    string? PaymentId,
    string Method,
    long AmountSatang,
    string Reason,
    string RefundedByStaffId,
    DateTimeOffset RefundedAt)
{
    public static InvoiceRefundDto From(InvoiceRefund refund) =>
        new(
            refund.Id,
            refund.PaymentId,
            BillingWire.ToWire(refund.Method),
            refund.AmountSatang,
            refund.Reason,
            refund.RefundedByStaffId,
            refund.RefundedAt);
}

public sealed record ReceiptDto(
    string Id,
    string Number,
    bool IsTaxInvoice,
    long AmountSatang,
    string? TaxId,
    string? LegalName,
    string? BillingAddress,
    string IssuedByStaffId,
    DateTimeOffset IssuedAt)
{
    public static ReceiptDto From(Receipt receipt) =>
        new(
            receipt.Id,
            receipt.Number,
            receipt.IsTaxInvoice,
            receipt.AmountSatang,
            receipt.TaxId,
            receipt.LegalName,
            receipt.BillingAddress,
            receipt.IssuedByStaffId,
            receipt.IssuedAt);
}
