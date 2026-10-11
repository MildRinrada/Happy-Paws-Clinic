using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Laboratory;
using HappyPaws.Domain.Patients;
using HappyPaws.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Laboratory;

/// <summary>
/// Lab requests and their results. The tests asked for are one <c>jsonb</c> value, written once with
/// the request and never queried into; the results are rows, because the timeline counts the
/// abnormal ones and a result is added in parts.
/// </summary>
internal sealed class LabRequestConfiguration : EntityConfiguration<LabRequest>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LabRequest> builder)
    {
        builder.ToTable("lab_requests");

        builder.Property(request => request.PetId).HasMaxLength(40).IsRequired();
        builder.Property(request => request.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(request => request.ConsultationId).HasMaxLength(40);
        builder.Property(request => request.ExternalLabName).HasMaxLength(200);
        builder.Property(request => request.ClinicalNotes).HasMaxLength(2_000);
        builder.Property(request => request.RequestedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(request => request.ReviewedByStaffId).HasMaxLength(40);
        builder.Property(request => request.ReviewNote).HasMaxLength(LabRequest.MaxReviewNoteLength);

        builder.Property(request => request.Destination).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(request => request.Priority).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.Property(request => request.Tests)
            .HasField("_tests")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .IsRequired();

        // The worklist: one status at a branch, newest first.
        builder.HasIndex(request => new { request.BranchId, request.Status, request.RequestedAt });
        builder.HasIndex(request => new { request.PetId, request.RequestedAt });

        builder.HasMany(request => request.Results)
            .WithOne()
            .HasForeignKey(result => result.LabRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(request => request.Results).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Files from the lab (docs/contract-extensions.md § 54): rows, one per file, gone with
        // their request.
        builder.HasMany(request => request.Attachments)
            .WithOne()
            .HasForeignKey(attachment => attachment.LabRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(request => request.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(request => request.PetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(request => request.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LabResultConfiguration : IEntityTypeConfiguration<LabResult>
{
    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.ToTable("lab_results");
        builder.HasKey(result => result.Id);

        builder.Property(result => result.Id).HasMaxLength(40);
        builder.Property(result => result.LabRequestId).HasMaxLength(40).IsRequired();
        builder.Property(result => result.Analyte).HasMaxLength(100).IsRequired();
        builder.Property(result => result.ValueText).HasMaxLength(500);
        builder.Property(result => result.Unit).HasMaxLength(30);
        builder.Property(result => result.ReferenceText).HasMaxLength(200);
        builder.Property(result => result.RecordedByStaffId).HasMaxLength(40).IsRequired();

        // Lab values carry more decimals than money; the convention's (18, 2) would round 0.005.
        builder.Property(result => result.ValueNumeric).HasPrecision(18, 6);
        builder.Property(result => result.ReferenceLow).HasPrecision(18, 6);
        builder.Property(result => result.ReferenceHigh).HasPrecision(18, 6);

        builder.Property(result => result.Flag).HasConversion<string>().HasMaxLength(16);

        // Corrections (docs/contract-extensions.md § 52). The values a correction replaced are one
        // jsonb list on the row: read with it, written whole, never queried into.
        builder.Property(result => result.AmendedByStaffId).HasMaxLength(40);
        builder.Property(result => result.AmendReason).HasMaxLength(LabResult.MaxReasonLength);
        builder.Property(result => result.VoidedByStaffId).HasMaxLength(40);
        builder.Property(result => result.VoidReason).HasMaxLength(LabResult.MaxReasonLength);

        builder.Property(result => result.PreviousValues)
            .HasField("_previousValues")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .IsRequired();
    }
}

internal sealed class LabAttachmentConfiguration : IEntityTypeConfiguration<LabAttachment>
{
    public void Configure(EntityTypeBuilder<LabAttachment> builder)
    {
        builder.ToTable("lab_attachments");
        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id).HasMaxLength(40);
        builder.Property(attachment => attachment.LabRequestId).HasMaxLength(40).IsRequired();
        builder.Property(attachment => attachment.StorageKey).HasMaxLength(300).IsRequired();
        builder.Property(attachment => attachment.FileName).HasMaxLength(LabFile.MaxFileNameLength).IsRequired();
        builder.Property(attachment => attachment.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(attachment => attachment.AttachedByStaffId).HasMaxLength(40).IsRequired();
    }
}
