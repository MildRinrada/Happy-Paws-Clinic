using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.Prescriptions;
using HappyPaws.Domain.Queueing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Consultations;

/// <summary>
/// The consultation aggregate and its five child tables.
/// <para>
/// The children cascade from the root, which is safe precisely because a consultation is never
/// deleted once closed — the cascade exists for the one case that can happen, an abandoned draft
/// being cleaned up, and never touches a signed record. The record itself is a separate table with
/// no cascade at all.
/// </para>
/// </summary>
internal sealed class ConsultationConfiguration : EntityConfiguration<Consultation>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Consultation> builder)
    {
        builder.ToTable("consultations");

        builder.Property(consultation => consultation.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(consultation => consultation.PetId).HasMaxLength(40).IsRequired();
        builder.Property(consultation => consultation.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(consultation => consultation.VetStaffId).HasMaxLength(40).IsRequired();
        builder.Property(consultation => consultation.AppointmentId).HasMaxLength(40);
        builder.Property(consultation => consultation.QueueTicketId).HasMaxLength(40);
        builder.Property(consultation => consultation.MedicalRecordId).HasMaxLength(40);

        builder.Property(consultation => consultation.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(consultation => consultation.ChiefComplaint).HasMaxLength(1_000);
        builder.Property(consultation => consultation.Subjective).HasMaxLength(4_000);
        builder.Property(consultation => consultation.Objective).HasMaxLength(4_000);
        builder.Property(consultation => consultation.Assessment).HasMaxLength(4_000);
        builder.Property(consultation => consultation.Plan).HasMaxLength(4_000);

        // One open case per visit. Partial, because a case opened from a walk-in ticket has no
        // appointment — and two vets opening the same appointment would otherwise produce two
        // parallel records of one consultation.
        builder.HasIndex(consultation => consultation.AppointmentId)
            .IsUnique()
            .HasFilter("appointment_id IS NOT NULL");

        builder.HasIndex(consultation => consultation.QueueTicketId)
            .IsUnique()
            .HasFilter("queue_ticket_id IS NOT NULL");

        // The patient's clinical timeline, newest first.
        builder.HasIndex(consultation => new { consultation.PetId, consultation.OpenedAt });
        builder.HasIndex(consultation => new { consultation.BranchId, consultation.Status });
        builder.HasIndex(consultation => new { consultation.VetStaffId, consultation.OpenedAt });

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(consultation => consultation.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(consultation => consultation.PetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Appointment>().WithMany()
            .HasForeignKey(consultation => consultation.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QueueTicket>().WithMany()
            .HasForeignKey(consultation => consultation.QueueTicketId).OnDelete(DeleteBehavior.Restrict);

        ConfigureChildren(builder);
    }

    private static void ConfigureChildren(EntityTypeBuilder<Consultation> builder)
    {
        builder.HasMany(consultation => consultation.Vitals)
            .WithOne()
            .HasForeignKey(reading => reading.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(consultation => consultation.Findings)
            .WithOne()
            .HasForeignKey(finding => finding.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(consultation => consultation.Diagnoses)
            .WithOne()
            .HasForeignKey(diagnosis => diagnosis.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(consultation => consultation.Treatments)
            .WithOne()
            .HasForeignKey(treatment => treatment.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(consultation => consultation.Prescriptions)
            .WithOne()
            .HasForeignKey(prescription => prescription.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Collections are exposed as IReadOnlyList over private backing fields, so EF must read and
        // write the field rather than the property — otherwise it would try to call a setter that
        // deliberately does not exist.
        foreach (var navigation in new[]
                 {
                     nameof(Consultation.Vitals),
                     nameof(Consultation.Findings),
                     nameof(Consultation.Diagnoses),
                     nameof(Consultation.Treatments),
                     nameof(Consultation.Prescriptions),
                 })
        {
            builder.Navigation(navigation).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
