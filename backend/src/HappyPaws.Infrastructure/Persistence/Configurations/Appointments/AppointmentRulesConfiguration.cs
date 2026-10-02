using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.ServiceCatalog;
using HappyPaws.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Appointments;

/// <summary>
/// The booking module's Tier-2 tables: two one-row rule sets and the waiting list. The rule sets
/// carry the same singleton check as the clinic's other settings, so a second row cannot exist.
/// </summary>
internal sealed class AppointmentTypeCatalogConfiguration : EntityConfiguration<AppointmentTypeCatalog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AppointmentTypeCatalog> builder)
    {
        builder.ToTable("appointment_types");

        builder.Property(catalog => catalog.Types)
            .HasField("_types")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .IsRequired();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_appointment_types_singleton",
            $"id = '{AppointmentTypeCatalog.SingletonId}'"));
    }
}

internal sealed class CancellationPolicyConfiguration : EntityConfiguration<CancellationPolicy>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CancellationPolicy> builder)
    {
        builder.ToTable("cancellation_policy");

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_cancellation_policy_singleton",
            $"id = '{CancellationPolicy.SingletonId}'"));
    }
}

internal sealed class WaitingListEntryConfiguration : EntityConfiguration<WaitingListEntry>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WaitingListEntry> builder)
    {
        builder.ToTable("waiting_list");

        builder.Property(entry => entry.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(entry => entry.PetId).HasMaxLength(40).IsRequired();
        builder.Property(entry => entry.ServiceId).HasMaxLength(40).IsRequired();
        builder.Property(entry => entry.BranchId).HasMaxLength(40);
        builder.Property(entry => entry.StaffId).HasMaxLength(40);
        builder.Property(entry => entry.Note).HasMaxLength(500);
        builder.Property(entry => entry.AddedByStaffId).HasMaxLength(40).IsRequired();

        // "Who is waiting for this service on this day", asked on every cancellation.
        builder.HasIndex(entry => new { entry.ServiceId, entry.LatestOn });
        builder.HasIndex(entry => new { entry.PetId, entry.ServiceId });

        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(entry => entry.PetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(entry => entry.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ClinicService>().WithMany()
            .HasForeignKey(entry => entry.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
