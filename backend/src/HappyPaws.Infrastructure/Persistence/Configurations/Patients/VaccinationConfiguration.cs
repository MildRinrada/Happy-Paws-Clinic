using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Patients;

internal sealed class VaccinationConfiguration : EntityConfiguration<Vaccination>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Vaccination> builder)
    {
        builder.ToTable("pet_vaccinations");

        builder.Property(dose => dose.PetId).HasMaxLength(40).IsRequired();
        builder.Property(dose => dose.VaccineName).HasMaxLength(200).IsRequired();
        builder.Property(dose => dose.Manufacturer).HasMaxLength(200);
        builder.Property(dose => dose.LotNumber).HasMaxLength(100).IsRequired();
        builder.Property(dose => dose.AdministeredByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(dose => dose.ConsultationId).HasMaxLength(40);
        builder.Property(dose => dose.Note).HasMaxLength(1_000);

        // A patient's card, newest dose first.
        builder.HasIndex(dose => new { dose.PetId, dose.AdministeredAt });

        // "Which patients had lot X" — the question a manufacturer's recall asks.
        builder.HasIndex(dose => dose.LotNumber);

        // What falls due when, for the reminder rules the Automation module will run.
        builder.HasIndex(dose => dose.NextDueOn).HasFilter("next_due_on IS NOT NULL");

        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(dose => dose.PetId).OnDelete(DeleteBehavior.Restrict);
    }
}
