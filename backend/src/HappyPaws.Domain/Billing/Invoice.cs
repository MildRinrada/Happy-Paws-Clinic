using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Billing;

/// <summary>
/// A bill — <c>POST /staff/invoices</c>, "สร้างเองหรือถูกสร้างอัตโนมัติตอนปิดเคส".
/// <para>
/// The aggregate root of the cash desk. Lines, discounts, payments, refunds and the receipt all
/// hang off it, and <see cref="Status"/> is never assigned from outside — it is recomputed from the
/// money after every movement, by <see cref="Recompute"/>. That is deliberate. A status column that
/// any caller may set is a column that will eventually say <c>paid</c> over an unpaid balance, and
/// no amount of care at the call sites prevents it; deriving it means the two can never disagree.
/// </para>
/// <para>
/// Money never leaves this file as a decimal. Every amount is an integer count of satang, summed
/// with integer arithmetic, exactly as the contract transports it.
/// </para>
/// </summary>
public sealed class Invoice : Entity
{
    private readonly List<InvoiceLine> _lines = [];
    private readonly List<InvoiceDiscount> _discounts = [];
    private readonly List<InvoicePayment> _payments = [];
    private readonly List<InvoiceRefund> _refunds = [];

    private Invoice()
    {
    }

    /// <summary>The running document number, e.g. <c>INV-202608-0042</c>. Unique across the clinic.</summary>
    public string Number { get; private set; } = string.Empty;

    public string BranchId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>The patient this bill is for. Null on a bill that is not about one animal.</summary>
    public string? PetId { get; private set; }

    /// <summary>The case that produced it — "ถูกสร้างอัตโนมัติตอนปิดเคส". Null when raised by hand.</summary>
    public string? ConsultationId { get; private set; }

    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Open;

    public string IssuedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }

    public string? VoidedByStaffId { get; private set; }

    public string? VoidReason { get; private set; }

    /// <summary>The document handed over, once one has been. See <see cref="Receipt"/>.</summary>
    public Receipt? Receipt { get; private set; }

    public IReadOnlyList<InvoiceLine> Lines => _lines;

    public IReadOnlyList<InvoiceDiscount> Discounts => _discounts;

    public IReadOnlyList<InvoicePayment> Payments => _payments;

    public IReadOnlyList<InvoiceRefund> Refunds => _refunds;

    public Money Subtotal => _lines.Aggregate(Money.Zero, (total, line) => total + line.LineTotal);

    public Money DiscountTotal => _discounts.Aggregate(Money.Zero, (total, discount) => total + discount.Amount);

    public Money Total => Subtotal - DiscountTotal;

    public Money Paid => _payments.Aggregate(Money.Zero, (total, payment) => total + payment.Amount);

    public Money Refunded => _refunds.Aggregate(Money.Zero, (total, refund) => total + refund.Amount);

    /// <summary>What is still owed. Never negative — the guards below make an overpayment impossible.</summary>
    public Money Balance => Total - Paid;

    public bool IsSettled => Balance.Satang <= 0;

    /// <summary><c>POST /staff/invoices</c>, and the tail of <c>POST /staff/consultations/{id}/close</c>.</summary>
    public static Invoice Raise(
        string number,
        string branchId,
        string customerId,
        string issuedByStaffId,
        DateTimeOffset issuedAt,
        string? petId = null,
        string? consultationId = null,
        string? note = null) =>
        new()
        {
            Id = EntityId.New(EntityId.Invoice),
            Number = number,
            BranchId = branchId,
            CustomerId = customerId,
            PetId = petId,
            ConsultationId = consultationId,
            IssuedByStaffId = issuedByStaffId,
            IssuedAt = issuedAt,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };

    /// <summary>
    /// Adds a billed row.
    /// <para>
    /// Only while the bill is still <c>open</c>, which — because the status is derived — is exactly
    /// the same thing as saying nothing has been collected yet. Once a customer has handed over
    /// money the bill is an agreed figure, and quietly enlarging it afterwards is how a counter ends
    /// up arguing with somebody holding a receipt. A forgotten item after that point is a second
    /// invoice.
    /// </para>
    /// </summary>
    public InvoiceLine AddLine(
        InvoiceLineKind kind,
        string description,
        int quantity,
        long unitPriceSatang,
        string? sourceId = null)
    {
        // "Open" here means nothing collected and not voided. A bill whose lines so far cost
        // nothing (a service covered by a package, say) computes as paid at ฿0, but it has taken
        // no money and is still being written.
        if (VoidedAt is not null || _payments.Count > 0)
        {
            throw new StateConflictException(nameof(Invoice), Status.ToString(), nameof(AddLine));
        }

        var line = InvoiceLine.Create(Id, kind, description, quantity, unitPriceSatang, sourceId);
        _lines.Add(line);

        Recompute();

        return line;
    }

    /// <summary>
    /// <c>POST /staff/invoices/{id}/discounts</c> — "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ".
    /// <para>
    /// Allowed on a partly paid bill, because a manager approving a reduction after the customer
    /// has already put down half is an ordinary counter event. What is refused is a discount that
    /// drops the total below what has been collected: that would leave the clinic owing money, and
    /// money owed back is a refund — a different endpoint, a different scope, and a reason the
    /// customer can be shown.
    /// </para>
    /// </summary>
    public InvoiceDiscount ApplyDiscount(
        long amountSatang,
        string reason,
        string approvedByStaffId,
        string appliedByStaffId,
        DateTimeOffset appliedAt,
        decimal? percent = null)
    {
        if (Status is not (InvoiceStatus.Open or InvoiceStatus.PartiallyPaid))
        {
            throw new StateConflictException(nameof(Invoice), Status.ToString(), nameof(ApplyDiscount));
        }

        if (DiscountTotal.Satang + amountSatang > Subtotal.Satang)
        {
            throw new DomainException("invalid_discount", "ส่วนลดรวมเกินยอดรวมของบิล");
        }

        if (Total.Satang - amountSatang < Paid.Satang)
        {
            throw new DomainException("invalid_discount", "ส่วนลดทำให้ยอดรวมต่ำกว่ายอดที่ชำระแล้ว");
        }

        var discount = InvoiceDiscount.Create(
            Id, amountSatang, reason, approvedByStaffId, appliedByStaffId, appliedAt, percent);

        _discounts.Add(discount);

        Recompute();

        return discount;
    }

    /// <summary>
    /// <c>POST /staff/invoices/{id}/payments</c> — "เงินสด บัตร PromptPay · แบ่งจ่ายหลายวิธีได้".
    /// </summary>
    public InvoicePayment Pay(
        PaymentMethod method,
        long amountSatang,
        string collectedByStaffId,
        DateTimeOffset collectedAt,
        string? reference = null)
    {
        if (Status is not (InvoiceStatus.Open or InvoiceStatus.PartiallyPaid))
        {
            throw new StateConflictException(nameof(Invoice), Status.ToString(), nameof(Pay));
        }

        if (amountSatang > Balance.Satang)
        {
            // Overpayment is refused rather than absorbed. A bill that can hold more money than it
            // asked for is a bill whose balance stops being a number anyone can reconcile, and the
            // change owed to a customer paying cash is settled in the drawer, not in the ledger.
            throw new DomainException("invalid_payment", "ยอดชำระเกินยอดคงเหลือของบิล");
        }

        var payment = InvoicePayment.Create(Id, method, amountSatang, collectedByStaffId, collectedAt, reference);
        _payments.Add(payment);

        Recompute();

        return payment;
    }

    /// <summary>
    /// <c>POST /staff/invoices/{id}/refunds</c> — "คืนเงินบางส่วนหรือทั้งใบ · ผูกเหตุผล".
    /// <para>
    /// A refund is terminal for the bill, partial or not. The contract draws the states in a line —
    /// <c>paid → refunded</c> — and gives no state for "paid, then partly given back". Returning a
    /// partly refunded bill to <c>partially_paid</c> would put it back on the list of things the
    /// counter chases for money, which is the one reading that is actually wrong. See
    /// <c>docs/assumptions.md § 22</c>.
    /// </para>
    /// </summary>
    public InvoiceRefund Refund(
        PaymentMethod method,
        long amountSatang,
        string reason,
        string refundedByStaffId,
        DateTimeOffset refundedAt,
        string? paymentId = null)
    {
        if (Status is not (InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid or InvoiceStatus.Refunded))
        {
            throw new StateConflictException(nameof(Invoice), Status.ToString(), nameof(Refund));
        }

        if (Refunded.Satang + amountSatang > Paid.Satang)
        {
            throw new DomainException("invalid_refund", "ยอดคืนเงินรวมเกินยอดที่รับชำระมา");
        }

        if (paymentId is { Length: > 0 } && _payments.All(payment => payment.Id != paymentId))
        {
            throw new DomainException("invalid_refund", "ไม่พบรายการชำระเงินที่อ้างถึง");
        }

        var refund = InvoiceRefund.Create(
            Id, method, amountSatang, reason, refundedByStaffId, refundedAt, paymentId);

        _refunds.Add(refund);

        Recompute();

        return refund;
    }

    /// <summary>
    /// <c>POST /staff/invoices/{id}/receipt</c> — "ออกใบเสร็จ/ใบกำกับภาษี".
    /// Returns the existing document if one has already been issued; see <see cref="Billing.Receipt"/>.
    /// </summary>
    public Receipt IssueReceipt(
        string number,
        string issuedByStaffId,
        DateTimeOffset issuedAt,
        bool isTaxInvoice = false,
        string? taxId = null,
        string? legalName = null,
        string? billingAddress = null)
    {
        if (Receipt is not null)
        {
            return Receipt;
        }

        if (Status is InvoiceStatus.Void)
        {
            throw new StateConflictException(nameof(Invoice), Status.ToString(), nameof(IssueReceipt));
        }

        if (_payments.Count == 0)
        {
            throw new DomainException("invalid_receipt", "ยังไม่มีการรับชำระเงินสำหรับบิลนี้");
        }

        Receipt = Receipt.Issue(
            Id, number, Paid.Satang, issuedByStaffId, issuedAt, isTaxInvoice, taxId, legalName, billingAddress);

        return Receipt;
    }

    /// <summary>
    /// <c>POST /staff/invoices/{id}/void</c> — not in the API reference; see
    /// <c>docs/contract-extensions.md § 3</c>.
    /// <para>
    /// Only a bill that has taken no money can be voided — <c>open</c>, and nothing else. Once a
    /// customer has paid, cancelling the document would erase the record of a transaction that
    /// really happened and leave the drawer out by that amount; the correction for that is a refund,
    /// which says what was given back and why.
    /// </para>
    /// </summary>
    public void Void(string reason, string byStaffId, DateTimeOffset now)
    {
        RequireStatus(InvoiceStatus.Open, nameof(Void));

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("invalid_void", "ต้องระบุเหตุผลในการยกเลิกบิล");
        }

        VoidedAt = now;
        VoidedByStaffId = byStaffId;
        VoidReason = reason.Trim();

        Recompute();
    }

    /// <summary>
    /// The single place <see cref="Status"/> is assigned. Order matters: void and refunded are exits
    /// and outrank the arithmetic, which is why they are tested first.
    /// </summary>
    private void Recompute() =>
        Status = VoidedAt is not null ? InvoiceStatus.Void
            : Refunded.Satang > 0 ? InvoiceStatus.Refunded
            : Paid.Satang >= Total.Satang ? InvoiceStatus.Paid
            : Paid.Satang > 0 ? InvoiceStatus.PartiallyPaid
            : InvoiceStatus.Open;

    private void RequireStatus(InvoiceStatus required, string transition)
    {
        if (Status != required)
        {
            throw new StateConflictException(nameof(Invoice), Status.ToString(), transition);
        }
    }
}
