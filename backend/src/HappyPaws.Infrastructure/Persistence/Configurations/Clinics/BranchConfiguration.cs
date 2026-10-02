using HappyPaws.Domain.Clinics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Clinics;

internal sealed class BranchConfiguration : EntityConfiguration<Branch>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");

        builder.Property(branch => branch.Code).HasMaxLength(40).IsRequired();
        builder.Property(branch => branch.Name).HasMaxLength(200).IsRequired();
        builder.Property(branch => branch.Address).HasMaxLength(500);

        // A primitive collection: exam rooms are labels the clinic chooses, with no data of their
        // own. A table would buy nothing and cost a join on every queue assignment.
        builder.PrimitiveCollection(branch => branch.ExamRooms)
            .HasField("_examRooms")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnType("text[]");

        builder.HasIndex(branch => branch.Code).IsUnique();

        // Half a coordinate is a pin in the ocean. The entity refuses it; this is for the UPDATE
        // someone writes by hand.
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_branches_coordinates_paired",
            "(latitude IS NULL) = (longitude IS NULL)"));
    }
}

internal sealed class BookingSettingsConfiguration : EntityConfiguration<BookingSettings>
{
    protected override void ConfigureEntity(EntityTypeBuilder<BookingSettings> builder)
    {
        builder.ToTable("booking_settings");

        // One clinic, one row. The check constraint makes that structural: a second row cannot be
        // inserted even by hand, so no code path has to remember to look for "the" settings.
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_booking_settings_singleton",
            $"id = '{BookingSettings.SingletonId}'"));
    }
}
