using HappyPaws.Domain.ServiceCatalog;
using HappyPaws.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.ServiceCatalog;

internal sealed class ClinicServiceConfiguration : EntityConfiguration<ClinicService>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClinicService> builder)
    {
        builder.ToTable("clinic_services");

        builder.Property(service => service.Code).HasMaxLength(60).IsRequired();
        builder.Property(service => service.Name).HasMaxLength(200).IsRequired();
        builder.Property(service => service.CategoryCode).HasMaxLength(60).IsRequired();

        // A primitive collection, so a service's eligible staff can be read in the same row as the
        // service. A join table would be correct if the relationship carried data; it does not.
        builder.PrimitiveCollection(service => service.EligibleStaffIds)
            .HasField("_eligibleStaffIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnType("text[]");

        // Same shape, same reasoning: which branches offer the service. The default keeps every
        // row that predates the column meaning what it always meant — offered everywhere.
        builder.PrimitiveCollection(service => service.BranchIds)
            .HasField("_branchIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnType("text[]")
            .HasDefaultValueSql("'{}'")
            .IsRequired();

        builder.Property(service => service.BookingWindows)
            .HasField("_bookingWindows")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .HasDefaultValueSql("'[]'")
            .IsRequired();

        builder.Property(service => service.PriceSchedule)
            .HasField("_priceSchedule")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .HasDefaultValueSql("'[]'")
            .IsRequired();

        builder.HasIndex(service => service.Code).IsUnique();

        // The public catalogue and the availability feed both read exactly this predicate.
        builder.HasIndex(service => new { service.OnlineBookingEnabled, service.IsActive });
    }
}
