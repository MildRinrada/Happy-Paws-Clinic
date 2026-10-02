using HappyPaws.Domain.Consultations;
using HappyPaws.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Consultations;

internal sealed class TreatmentTemplateConfiguration : EntityConfiguration<TreatmentTemplate>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TreatmentTemplate> builder)
    {
        builder.ToTable("treatment_templates");

        builder.Property(template => template.Name).HasMaxLength(200).IsRequired();
        builder.Property(template => template.Species).HasMaxLength(60);
        builder.Property(template => template.Description).HasMaxLength(1_000);
        builder.Property(template => template.Plan).HasMaxLength(2_000);

        // A template is read and applied whole; its items are never queried on their own.
        builder.Property(template => template.Procedures)
            .HasField("_procedures")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .IsRequired();

        builder.Property(template => template.Medicines)
            .HasField("_medicines")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .IsRequired();

        builder.HasIndex(template => template.Name).IsUnique();
    }
}
