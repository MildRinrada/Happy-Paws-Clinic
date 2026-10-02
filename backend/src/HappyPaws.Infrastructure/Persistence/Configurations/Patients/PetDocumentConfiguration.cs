using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Patients;

internal sealed class PetDocumentConfiguration : EntityConfiguration<PetDocument>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PetDocument> builder)
    {
        builder.ToTable("pet_documents");

        builder.Property(document => document.PetId).HasMaxLength(40).IsRequired();
        builder.Property(document => document.Source).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(document => document.AddedBy).HasMaxLength(40).IsRequired();
        builder.Property(document => document.Kind).HasMaxLength(20).IsRequired();
        builder.Property(document => document.Title).HasMaxLength(200).IsRequired();
        builder.Property(document => document.FileName).HasMaxLength(200).IsRequired();
        builder.Property(document => document.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.StorageKey).HasMaxLength(255).IsRequired();
        builder.Property(document => document.ConsultationId).HasMaxLength(40);

        builder.HasIndex(document => document.StorageKey).IsUnique();
        builder.HasIndex(document => new { document.PetId, document.CreatedAt });

        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(document => document.PetId).OnDelete(DeleteBehavior.Restrict);
    }
}
