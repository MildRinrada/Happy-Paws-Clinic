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

        builder.PrimitiveCollection(package => package.ServiceIds)
            .HasField("_serviceIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Writes that race on the last use are caught by the version, like every other aggregate.
        builder.ToTable(table => table.HasCheckConstraint("ck_customer_packages_uses", "used_count >= 0 AND used_count <= total_uses"));

        builder.HasIndex(package => package.CustomerId);

        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(package => package.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
