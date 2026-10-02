using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Patients;

internal sealed class PetAllergyConfiguration : EntityConfiguration<PetAllergy>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PetAllergy> builder)
    {
        builder.ToTable("pet_allergies");

        builder.Property(allergy => allergy.PetId).HasMaxLength(40).IsRequired();
        builder.Property(allergy => allergy.Substance).HasMaxLength(200).IsRequired();
        builder.Property(allergy => allergy.Reaction).HasMaxLength(1_000);
        builder.Property(allergy => allergy.RecordedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(allergy => allergy.WithdrawnReason).HasMaxLength(500);

        builder.Property(allergy => allergy.Severity)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        // Read on every prescription, so it is on the hot path of the examination room.
        builder.HasIndex(allergy => allergy.PetId).HasFilter("withdrawn_at IS NULL");

        // The same substance recorded twice for one patient is a data-entry slip, not a second
        // allergy. Partial so a withdrawn entry does not block re-recording it later.
        builder.HasIndex(allergy => new { allergy.PetId, allergy.Substance })
            .IsUnique()
            .HasFilter("withdrawn_at IS NULL");

        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(allergy => allergy.PetId).OnDelete(DeleteBehavior.Cascade);
    }
}
