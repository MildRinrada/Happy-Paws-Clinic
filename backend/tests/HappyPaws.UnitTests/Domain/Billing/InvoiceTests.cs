using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Domain.Billing;

/// <summary>
/// The arithmetic of a bill, and the state machine the contract draws over it:
/// <c>open → partially_paid → paid → refunded</c>, with <c>void</c> as an exit.
/// </summary>
public class InvoiceTests
{
    private static readonly DateTimeOffset At = new(2026, 8, 27, 14, 0, 0, TimeSpan.FromHours(7));

    /// <summary>A ฿1,460 bill — the figure from the contract's own worked example.</summary>
    private static Invoice Bill()
    {
        var invoice = Invoice.Raise("INV-202608-0001", "brn_1", "cus_1180", "stf_204", At, "pet_luna");

        invoice.AddLine(InvoiceLineKind.Service, "ตรวจรักษาทั่วไป", 1, 45_000);
        invoice.AddLine(InvoiceLineKind.Treatment, "ทำแผล", 1, 101_000);

        return invoice;
    }

    [Fact]
    public void A_new_bill_is_open_and_owes_its_subtotal()
    {
        var invoice = Bill();

        Assert.Equal(InvoiceStatus.Open, invoice.Status);
        Assert.Equal(146_000, invoice.Subtotal.Satang);
        Assert.Equal(146_000, invoice.Total.Satang);
        Assert.Equal(146_000, invoice.Balance.Satang);
    }

    [Fact]
    public void A_free_first_line_does_not_stop_the_bill_taking_more_lines()
    {
        var invoice = Invoice.Raise("INV-202608-0002", "brn_1", "cus_1180", "stf_204", At);

        invoice.AddLine(InvoiceLineKind.Service, "ตรวจสุขภาพ (ใช้สิทธิ์แพ็กเกจ)", 1, 0);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);

        invoice.AddLine(InvoiceLineKind.Other, "ค่าอุปกรณ์", 1, 20_000);
        Assert.Equal(InvoiceStatus.Open, invoice.Status);
        Assert.Equal(20_000, invoice.Total.Satang);
    }

    [Fact]
    public void Splitting_a_payment_walks_open_to_partially_paid_to_paid()
    {
        var invoice = Bill();

        invoice.Pay(PaymentMethod.Cash, 40_000, "stf_9", At);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
        Assert.Equal(106_000, invoice.Balance.Satang);

        // "แบ่งจ่ายหลายวิธีได้" — the rest on a card, and the status arrives on its own.
        invoice.Pay(PaymentMethod.Card, 106_000, "stf_9", At, "APPR-77213");

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(0, invoice.Balance.Satang);
        Assert.Equal(2, invoice.Payments.Count);
    }

    [Fact]
    public void A_bill_cannot_take_more_money_than_it_asked_for()
    {
        var invoice = Bill();

        // Change for a cash customer is settled in the drawer. A bill that can hold more than its
        // total is a bill whose balance nobody can reconcile.
        Assert.Throws<DomainException>(() => invoice.Pay(PaymentMethod.Cash, 200_000, "stf_9", At));
    }

    [Fact]
    public void A_discount_reduces_the_total_and_records_who_approved_it()
    {
        var invoice = Bill();

        var discount = invoice.ApplyDiscount(14_600, "ลูกค้าประจำ", "stf_owner", "stf_9", At, percent: 10m);

        Assert.Equal(131_400, invoice.Total.Satang);
        Assert.Equal("stf_owner", discount.ApprovedByStaffId);
        Assert.Equal("stf_9", discount.AppliedByStaffId);
        Assert.Equal(10m, discount.Percent);
    }

    [Fact]
    public void A_percentage_is_resolved_downwards_to_the_satang()
    {
        // 7% of ฿1,460 is 10,220 satang exactly; 3% is 4,380. The rounding case is a rate that does
        // not divide: 1/3 of a satang given away is a satang the approver did not approve.
        Assert.Equal(10_220, InvoiceDiscount.ResolvePercent(7m, 146_000));
        Assert.Equal(48_666, InvoiceDiscount.ResolvePercent(33.333m, 146_000));
    }

    [Fact]
    public void A_discount_cannot_drop_the_total_below_what_has_been_collected()
    {
        var invoice = Bill();
        invoice.Pay(PaymentMethod.Cash, 140_000, "stf_9", At);

        // The clinic would owe the customer money, and money owed back is a refund — a different
        // endpoint, a different scope, and a reason the customer can be shown.
        Assert.Throws<DomainException>(() =>
            invoice.ApplyDiscount(20_000, "ลดให้", "stf_owner", "stf_9", At));
    }

    [Fact]
    public void A_discount_cannot_exceed_the_bill()
    {
        var invoice = Bill();

        Assert.Throws<DomainException>(() =>
            invoice.ApplyDiscount(200_000, "ลดให้", "stf_owner", "stf_9", At));
    }

    [Fact]
    public void A_line_cannot_be_added_once_money_has_been_taken()
    {
        var invoice = Bill();
        invoice.Pay(PaymentMethod.Cash, 10_000, "stf_9", At);

        Assert.Throws<StateConflictException>(() =>
            invoice.AddLine(InvoiceLineKind.Other, "ค่ายาเพิ่ม", 1, 5_000));
    }

    [Fact]
    public void A_refund_is_terminal_whether_or_not_it_is_full()
    {
        var invoice = Bill();
        invoice.Pay(PaymentMethod.Cash, 146_000, "stf_9", At);

        invoice.Refund(PaymentMethod.Cash, 46_000, "คิดค่าหัตถการซ้ำ", "stf_9", At);

        // The contract draws paid → refunded in a line and gives no state for "paid, then partly
        // given back". Returning this bill to partially_paid would put a settled transaction back
        // on the counter's list of people to chase.
        Assert.Equal(InvoiceStatus.Refunded, invoice.Status);
        Assert.Equal(46_000, invoice.Refunded.Satang);
    }

    [Fact]
    public void More_cannot_be_refunded_than_was_ever_collected()
    {
        var invoice = Bill();
        invoice.Pay(PaymentMethod.Cash, 100_000, "stf_9", At);

        Assert.Throws<DomainException>(() =>
            invoice.Refund(PaymentMethod.Cash, 120_000, "คืนทั้งใบ", "stf_9", At));
    }

    [Fact]
    public void A_bill_that_took_no_money_cannot_be_refunded()
    {
        var invoice = Bill();

        Assert.Throws<StateConflictException>(() =>
            invoice.Refund(PaymentMethod.Cash, 10_000, "คืนเงิน", "stf_9", At));
    }

    [Fact]
    public void Voiding_needs_a_reason_and_an_untouched_bill()
    {
        var invoice = Bill();

        Assert.Throws<DomainException>(() => invoice.Void("  ", "stf_9", At));

        invoice.Void("ออกบิลผิดคน", "stf_9", At);

        Assert.Equal(InvoiceStatus.Void, invoice.Status);
        Assert.Equal("ออกบิลผิดคน", invoice.VoidReason);
    }

    [Fact]
    public void A_bill_that_has_taken_money_is_corrected_by_a_refund_not_a_void()
    {
        var invoice = Bill();
        invoice.Pay(PaymentMethod.Cash, 146_000, "stf_9", At);

        // Voiding would erase a transaction that really happened and leave the drawer out by ฿1,460.
        Assert.Throws<StateConflictException>(() => invoice.Void("ยกเลิก", "stf_9", At));
    }

    [Fact]
    public void A_voided_bill_takes_no_further_money()
    {
        var invoice = Bill();
        invoice.Void("ออกบิลซ้ำ", "stf_9", At);

        Assert.Throws<StateConflictException>(() => invoice.Pay(PaymentMethod.Cash, 1_000, "stf_9", At));
    }

    [Fact]
    public void A_receipt_needs_a_payment_behind_it()
    {
        var invoice = Bill();

        Assert.Throws<DomainException>(() => invoice.IssueReceipt("RCP-202608-0001", "stf_9", At));
    }

    [Fact]
    public void Asking_for_a_receipt_twice_returns_the_first_one()
    {
        var invoice = Bill();
        invoice.Pay(PaymentMethod.Cash, 146_000, "stf_9", At);

        var first = invoice.IssueReceipt("RCP-202608-0001", "stf_9", At);
        var again = invoice.IssueReceipt("RCP-202608-0002", "stf_9", At.AddMinutes(1));

        // A second original ใบกำกับภาษี is a tax problem, not a convenience.
        Assert.Same(first, again);
        Assert.Equal("RCP-202608-0001", invoice.Receipt!.Number);
        Assert.Equal(146_000, invoice.Receipt.AmountSatang);
    }

    [Fact]
    public void A_tax_invoice_without_a_tax_id_is_not_a_tax_invoice()
    {
        var invoice = Bill();
        invoice.Pay(PaymentMethod.Cash, 146_000, "stf_9", At);

        Assert.Throws<DomainException>(() =>
            invoice.IssueReceipt("RCP-202608-0001", "stf_9", At, isTaxInvoice: true));
    }

    [Fact]
    public void A_negative_line_is_refused_so_discounts_cannot_arrive_in_disguise()
    {
        var invoice = Bill();

        // Otherwise the clinic's whole discount policy — reason, approver, audit row — would be
        // bypassable by anyone holding invoice:write.
        Assert.Throws<DomainException>(() =>
            invoice.AddLine(InvoiceLineKind.Other, "ส่วนลด", 1, -10_000));
    }
}
