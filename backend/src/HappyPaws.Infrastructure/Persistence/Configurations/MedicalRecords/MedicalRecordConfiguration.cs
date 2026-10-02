using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.MedicalRecords;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.MedicalRecords;

/// <summary>
/// The signed record and its amendments.
/// <para>
/// Note what is <b>not</b> configured: no cascade, anywhere. "ลบไม่ได้ · ทุกฉบับเก็บถาวรพร้อม
/// ผู้ลงนาม" is a rule about permanence, and a cascade from the pet or the consultation would be a
/// path by which a signed record disappears without anyone intending it.
/// </para>
/// </summary>
internal sealed class MedicalRecordConfiguration : EntityConfiguration<MedicalRecord>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("medical_records");

        builder.Property(record => record.ConsultationId).HasMaxLength(40).IsRequired();
        builder.Property(record => record.PetId).HasMaxLength(40).IsRequired();
        builder.Property(record => record.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(record => record.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(record => record.SignedByStaffId).HasMaxLength(40).IsRequired();

        builder.Property(record => record.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        // text, deliberately, where every other JSON column in this schema is jsonb. PostgreSQL
        // normalises jsonb on the way in — key order is not preserved and whitespace is stripped —
        // so a document written as jsonb comes back as different bytes from the ones that were
        // hashed, and every signed record would read as tampered with. A signed document is stored
        // exactly as it was signed or it is not signed at all. Nothing queries inside it: the
        // structured facts live in the consultation tables.
        builder.Property(record => record.ContentJson).HasColumnType("text").IsRequired();
        builder.Property(record => record.ContentHash).HasMaxLength(64).IsRequired();

        // One record per consultation. Closing a case twice must not produce two signed documents
        // of the same visit.
        builder.HasIndex(record => record.ConsultationId).IsUnique();

        // The patient's history, newest first.
        builder.HasIndex(record => new { record.PetId, record.SignedAt });

        builder.Ignore(record => record.CurrentContentJson);

        builder.HasOne<Consultation>().WithMany()
            .HasForeignKey(record => record.ConsultationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(record => record.PetId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(record => record.Amendments)
            .WithOne()
            .HasForeignKey(amendment => amendment.MedicalRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(record => record.Amendments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MedicalRecordAmendmentConfiguration : IEntityTypeConfiguration<MedicalRecordAmendment>
{
    public void Configure(EntityTypeBuilder<MedicalRecordAmendment> builder)
    {
        builder.ToTable("medical_record_amendments");
        builder.HasKey(amendment => amendment.Id);

        builder.Property(amendment => amendment.Id).HasMaxLength(40);
        builder.Property(amendment => amendment.MedicalRecordId).HasMaxLength(40).IsRequired();
        builder.Property(amendment => amendment.ByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(amendment => amendment.Reason).HasMaxLength(1_000).IsRequired();

        // Same reasoning as the record's own content: an amendment is a version of a signed
        // document and is stored verbatim. The diff is jsonb, because unlike the document it is
        // something a reviewer will want to query across.
        builder.Property(amendment => amendment.ContentJson).HasColumnType("text").IsRequired();
        builder.Property(amendment => amendment.ChangesJson).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(amendment => new { amendment.MedicalRecordId, amendment.CreatedAt });
    }
}
