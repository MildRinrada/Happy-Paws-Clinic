using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Crm;
using HappyPaws.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Crm;

internal sealed class CustomerPackageConfiguration : EntityConfiguration<CustomerPackage>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CustomerPackage> builder)
    {
        builder.ToTable("customer_packages");

        builder.Property(package => package.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(package => package.PetId).HasMaxLength(40);
        builder.Property(package => package.Name).HasMaxLength(200).IsRequired();
        builder.Property(package => package.BranchId).HasMaxLength(40);
        builder.Property(package => package.InvoiceId).HasMaxLength(40);
        builder.Property(package => package.SoldByStaffId).HasMaxLength(40);
        builder.Property(package => package.CancelReason).HasMaxLength(200);

        builder.PrimitiveCollection(package => package.ServiceIds)
            .HasField("_serviceIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Writes that race on the last use are serialised by the per-customer lock and caught by
        // the version as a backstop, like every other aggregate.
        builder.ToTable(table => table.HasCheckConstraint("ck_customer_packages_uses", "used_count >= 0 AND used_count <= total_uses"));
        builder.ToTable(table => table.HasCheckConstraint("ck_customer_packages_price", "price_satang >= 0"));

        builder.HasIndex(package => package.CustomerId);

        // "Which package did this bill sell?" — read on every payment, refund and void.
        builder.HasIndex(package => package.InvoiceId).HasFilter("invoice_id IS NOT NULL");

        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(package => package.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany()
            .HasForeignKey(package => package.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(package => package.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PackageUseConfiguration : EntityConfiguration<PackageUse>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PackageUse> builder)
    {
        builder.ToTable("package_uses");

        builder.Property(use => use.PackageId).HasMaxLength(40).IsRequired();
        builder.Property(use => use.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(use => use.InvoiceId).HasMaxLength(40).IsRequired();
        builder.Property(use => use.InvoiceLineId).HasMaxLength(40).IsRequired();
        builder.Property(use => use.ServiceId).HasMaxLength(40).IsRequired();
        builder.Property(use => use.PetId).HasMaxLength(40);

        builder.ToTable(table => table.HasCheckConstraint("ck_package_uses_quantity", "quantity > 0"));

        builder.HasIndex(use => use.PackageId);
        builder.HasIndex(use => use.InvoiceId);

        builder.HasOne<CustomerPackage>().WithMany()
            .HasForeignKey(use => use.PackageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany()
            .HasForeignKey(use => use.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
