using HappyPaws.Domain.Patients;
using HappyPaws.Domain.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Prescriptions;

internal sealed class PrescriptionConfiguration : EntityConfiguration<Prescription>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("prescriptions");

        builder.Property(prescription => prescription.ConsultationId).HasMaxLength(40).IsRequired();
        builder.Property(prescription => prescription.PetId).HasMaxLength(40).IsRequired();
        builder.Property(prescription => prescription.PrescribedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(prescription => prescription.DispensedByStaffId).HasMaxLength(40);

        builder.Property(prescription => prescription.DrugName).HasMaxLength(200).IsRequired();
        builder.Property(prescription => prescription.Dose).HasMaxLength(100).IsRequired();
        builder.Property(prescription => prescription.Frequency).HasMaxLength(200).IsRequired();
        builder.Property(prescription => prescription.Instructions).HasMaxLength(1_000);
        builder.Property(prescription => prescription.AllergyAcknowledgement).HasMaxLength(500);

        // "ยาที่เคยจ่ายทั้งหมด · ตรวจซ้ำซ้อน" — every drug this patient has been given, newest first.
        builder.HasIndex(prescription => new { prescription.PetId, prescription.PrescribedAt });

        // The pharmacy's outstanding work.
        builder.HasIndex(prescription => prescription.DispensedAt)
            .HasFilter("dispensed_at IS NULL");

        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(prescription => prescription.PetId).OnDelete(DeleteBehavior.Restrict);
    }
}
