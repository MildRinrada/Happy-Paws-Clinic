using HappyPaws.Domain.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Billing;

/// <summary>
/// The five child tables of an invoice. Grouped in one file for the same reason the consultation's
/// are: each is a handful of columns and a foreign key, and five files would add navigation without
/// adding a boundary.
/// <para>
/// Enums are stored as <c>text</c> here rather than as ints. These rows are read directly during a
/// month-end reconciliation, often by someone querying the database rather than the API, and
/// <c>method = 'promptpay'</c> is legible in a way <c>method = 2</c> is not — the queue's priority
/// is stored as an int for the opposite reason, because it is <i>sorted</i> on and its order is the
/// whole point.
/// </para>
/// </summary>
internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines");
        builder.HasKey(line => line.Id);

        builder.Property(line => line.Id).HasMaxLength(40);
        builder.Property(line => line.InvoiceId).HasMaxLength(40).IsRequired();
        builder.Property(line => line.Description).HasMaxLength(300).IsRequired();
        builder.Property(line => line.SourceId).HasMaxLength(40);

        builder.Property(line => line.Kind)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // "กำไรต่อบริการหลังหักยา" reads by kind across a date range.
        builder.HasIndex(line => line.Kind);
    }
}

internal sealed class InvoiceDiscountConfiguration : IEntityTypeConfiguration<InvoiceDiscount>
{
    public void Configure(EntityTypeBuilder<InvoiceDiscount> builder)
    {
        builder.ToTable("invoice_discounts");
        builder.HasKey(discount => discount.Id);

        builder.Property(discount => discount.Id).HasMaxLength(40);
        builder.Property(discount => discount.InvoiceId).HasMaxLength(40).IsRequired();
        builder.Property(discount => discount.Reason).HasMaxLength(500).IsRequired();
        builder.Property(discount => discount.ApprovedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(discount => discount.AppliedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(discount => discount.Percent).HasPrecision(5, 2);

        // "ส่วนลดที่ใครอนุมัติบ้างเดือนนี้" — the question this table exists to answer.
        builder.HasIndex(discount => new { discount.ApprovedByStaffId, discount.AppliedAt });
    }
}

internal sealed class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.ToTable("invoice_payments");
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Id).HasMaxLength(40);
        builder.Property(payment => payment.InvoiceId).HasMaxLength(40).IsRequired();
        builder.Property(payment => payment.Reference).HasMaxLength(120);
        builder.Property(payment => payment.CollectedByStaffId).HasMaxLength(40).IsRequired();

        builder.Property(payment => payment.Method)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // The day's takings, split by tender — what reconciles against the drawer and the acquirer.
        builder.HasIndex(payment => new { payment.CollectedAt, payment.Method });
    }
}

internal sealed class InvoiceRefundConfiguration : IEntityTypeConfiguration<InvoiceRefund>
{
    public void Configure(EntityTypeBuilder<InvoiceRefund> builder)
    {
        builder.ToTable("invoice_refunds");
        builder.HasKey(refund => refund.Id);

        builder.Property(refund => refund.Id).HasMaxLength(40);
        builder.Property(refund => refund.InvoiceId).HasMaxLength(40).IsRequired();
        builder.Property(refund => refund.PaymentId).HasMaxLength(40);
        builder.Property(refund => refund.Reason).HasMaxLength(500).IsRequired();
        builder.Property(refund => refund.RefundedByStaffId).HasMaxLength(40).IsRequired();

        builder.Property(refund => refund.Method)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(refund => refund.RefundedAt);
    }
}

internal sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("receipts");
        builder.HasKey(receipt => receipt.Id);

        builder.Property(receipt => receipt.Id).HasMaxLength(40);
        builder.Property(receipt => receipt.InvoiceId).HasMaxLength(40).IsRequired();
        builder.Property(receipt => receipt.Number).HasMaxLength(32).IsRequired();
        builder.Property(receipt => receipt.TaxId).HasMaxLength(20);
        builder.Property(receipt => receipt.LegalName).HasMaxLength(200);
        builder.Property(receipt => receipt.BillingAddress).HasMaxLength(500);
        builder.Property(receipt => receipt.IssuedByStaffId).HasMaxLength(40).IsRequired();

        // A tax document number is unique or it is not a tax document number.
        builder.HasIndex(receipt => receipt.Number).IsUnique();

        // One receipt per invoice, enforced here as well as in the aggregate: two cashiers pressing
        // the button at the same moment would each read no receipt and each mint one.
        builder.HasIndex(receipt => receipt.InvoiceId).IsUnique();
    }
}
