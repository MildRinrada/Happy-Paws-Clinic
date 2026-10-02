using HappyPaws.Domain.Patients;
using HappyPaws.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Patients;

internal sealed class TreatmentPlanConfiguration : EntityConfiguration<TreatmentPlan>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TreatmentPlan> builder)
    {
        builder.ToTable("treatment_plans");

        builder.Property(plan => plan.PetId).HasMaxLength(40).IsRequired();
        builder.Property(plan => plan.Title).HasMaxLength(200).IsRequired();
        builder.Property(plan => plan.ServiceId).HasMaxLength(40).IsRequired();
        builder.Property(plan => plan.ConsultationId).HasMaxLength(40);
        builder.Property(plan => plan.TemplateId).HasMaxLength(40);
        builder.Property(plan => plan.Notes).HasMaxLength(2_000);
        builder.Property(plan => plan.SeriesId).HasMaxLength(40);
        builder.Property(plan => plan.CreatedByStaffId).HasMaxLength(40).IsRequired();

        // Sessions are read and written with the plan; the appointments they point to are the
        // queryable side.
        builder.Property(plan => plan.Sessions)
            .HasField("_sessions")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .IsRequired();

        builder.HasIndex(plan => plan.PetId);

        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(plan => plan.PetId).OnDelete(DeleteBehavior.Restrict);
    }
}
