using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Billing;

/// <summary>
/// The invoice aggregate and its five child tables.
/// <para>
/// Children cascade from the root because a bill is either raised or it is not; there is no state
/// in which a payment row should outlive the invoice it settled. What keeps that safe is that
/// invoices are <b>never deleted</b> — a bill raised in error is voided, which leaves the document
/// and its reason in place. The cascade exists for the schema's sake, not as a workflow.
/// </para>
/// <para>
/// Every money column is <c>bigint</c> satang. There is no <c>decimal</c> anywhere in this schema,
/// and no computed total column either: totals are derived in the aggregate, so a hand-written
/// UPDATE cannot leave a stored total disagreeing with the rows it is supposed to sum.
/// </para>
/// </summary>
internal sealed class InvoiceConfiguration : EntityConfiguration<Invoice>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.Property(invoice => invoice.Number).HasMaxLength(32).IsRequired();
        builder.Property(invoice => invoice.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(invoice => invoice.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(invoice => invoice.PetId).HasMaxLength(40);
        builder.Property(invoice => invoice.ConsultationId).HasMaxLength(40);
        builder.Property(invoice => invoice.IssuedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(invoice => invoice.VoidedByStaffId).HasMaxLength(40);
        builder.Property(invoice => invoice.VoidReason).HasMaxLength(500);
        builder.Property(invoice => invoice.Note).HasMaxLength(1_000);

        builder.Property(invoice => invoice.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // A document number is quoted over the phone and printed on a customer's copy. Two bills
        // sharing one is not a display bug; it is two transactions the clinic cannot tell apart.
        builder.HasIndex(invoice => invoice.Number).IsUnique();

        // One bill per case. Partial, because a bill raised at the counter has no consultation —
        // and a retried close must not charge the customer twice.
        builder.HasIndex(invoice => invoice.ConsultationId)
            .IsUnique()
            .HasFilter("consultation_id IS NOT NULL");

        // The counter's working list: this branch, this status, newest first.
        builder.HasIndex(invoice => new { invoice.BranchId, invoice.Status, invoice.IssuedAt });

        // The portal's list, and "ยอดค้างชำระ" on a customer record.
        builder.HasIndex(invoice => new { invoice.CustomerId, invoice.IssuedAt });

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(invoice => invoice.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(invoice => invoice.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(invoice => invoice.PetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Consultation>().WithMany()
            .HasForeignKey(invoice => invoice.ConsultationId).OnDelete(DeleteBehavior.Restrict);

        ConfigureChildren(builder);
    }

    private static void ConfigureChildren(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasMany(invoice => invoice.Lines)
            .WithOne()
            .HasForeignKey(line => line.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(invoice => invoice.Discounts)
            .WithOne()
            .HasForeignKey(discount => discount.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(invoice => invoice.Payments)
            .WithOne()
            .HasForeignKey(payment => payment.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(invoice => invoice.Refunds)
            .WithOne()
            .HasForeignKey(refund => refund.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(invoice => invoice.Receipt)
            .WithOne()
            .HasForeignKey<Receipt>(receipt => receipt.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Collections are IReadOnlyList over private backing fields, so EF reads and writes the
        // field rather than looking for a setter that deliberately does not exist.
        foreach (var navigation in new[]
                 {
                     nameof(Invoice.Lines),
                     nameof(Invoice.Discounts),
                     nameof(Invoice.Payments),
                     nameof(Invoice.Refunds),
                 })
        {
            builder.Navigation(navigation).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
