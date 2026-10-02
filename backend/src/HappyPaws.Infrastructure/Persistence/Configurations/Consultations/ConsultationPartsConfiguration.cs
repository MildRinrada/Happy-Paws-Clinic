using HappyPaws.Domain.Consultations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Consultations;

/// <summary>
/// The four child tables of a consultation. Grouped in one file because each is a handful of
/// columns and a single foreign key; splitting them into four files would add navigation cost
/// without adding a boundary.
/// </summary>
internal sealed class VitalsReadingConfiguration : IEntityTypeConfiguration<VitalsReading>
{
    public void Configure(EntityTypeBuilder<VitalsReading> builder)
    {
        builder.ToTable("consultation_vitals");
        builder.HasKey(reading => reading.Id);

        builder.Property(reading => reading.Id).HasMaxLength(40);
        builder.Property(reading => reading.ConsultationId).HasMaxLength(40).IsRequired();
        builder.Property(reading => reading.PetId).HasMaxLength(40).IsRequired();
        builder.Property(reading => reading.RecordedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(reading => reading.Note).HasMaxLength(1_000);

        builder.Property(reading => reading.WeightKg).HasPrecision(6, 3);
        builder.Property(reading => reading.TemperatureC).HasPrecision(4, 1);

        // The weight graph: "ชุดข้อมูลกราฟน้ำหนักย้อนหลัง", read by patient over time.
        builder.HasIndex(reading => new { reading.PetId, reading.RecordedAt });
    }
}

internal sealed class ExaminationFindingConfiguration : IEntityTypeConfiguration<ExaminationFinding>
{
    public void Configure(EntityTypeBuilder<ExaminationFinding> builder)
    {
        builder.ToTable("consultation_findings");
        builder.HasKey(finding => finding.Id);

        builder.Property(finding => finding.Id).HasMaxLength(40);
        builder.Property(finding => finding.ConsultationId).HasMaxLength(40).IsRequired();
        builder.Property(finding => finding.BodySystem).HasMaxLength(80).IsRequired();
        builder.Property(finding => finding.Note).HasMaxLength(2_000);
    }
}

internal sealed class DiagnosisConfiguration : IEntityTypeConfiguration<Diagnosis>
{
    public void Configure(EntityTypeBuilder<Diagnosis> builder)
    {
        builder.ToTable("consultation_diagnoses");
        builder.HasKey(diagnosis => diagnosis.Id);

        builder.Property(diagnosis => diagnosis.Id).HasMaxLength(40);
        builder.Property(diagnosis => diagnosis.ConsultationId).HasMaxLength(40).IsRequired();
        builder.Property(diagnosis => diagnosis.Code).HasMaxLength(40);
        builder.Property(diagnosis => diagnosis.Description).HasMaxLength(500).IsRequired();
        builder.Property(diagnosis => diagnosis.Note).HasMaxLength(2_000);

        // At most one primary diagnosis per case, enforced by the database rather than only by the
        // aggregate — two concurrent writers would each see zero primaries and both promote.
        builder.HasIndex(diagnosis => diagnosis.ConsultationId)
            .IsUnique()
            .HasFilter("is_primary")
            .HasDatabaseName("ux_consultation_primary_diagnosis");

        builder.HasIndex(diagnosis => diagnosis.Code);
    }
}

internal sealed class TreatmentConfiguration : IEntityTypeConfiguration<Treatment>
{
    public void Configure(EntityTypeBuilder<Treatment> builder)
    {
        builder.ToTable("consultation_treatments");
        builder.HasKey(treatment => treatment.Id);

        builder.Property(treatment => treatment.Id).HasMaxLength(40);
        builder.Property(treatment => treatment.ConsultationId).HasMaxLength(40).IsRequired();
        builder.Property(treatment => treatment.ServiceId).HasMaxLength(40);
        builder.Property(treatment => treatment.Name).HasMaxLength(200).IsRequired();
        builder.Property(treatment => treatment.PerformedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(treatment => treatment.Note).HasMaxLength(1_000);

        // LineTotal is computed from these two and is not stored — a persisted total is one more
        // thing that can disagree with its own inputs.
        builder.Ignore(treatment => treatment.LineTotal);
    }
}
