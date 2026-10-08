using HappyPaws.Domain.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Reporting;

internal sealed class ReportExportConfiguration : EntityConfiguration<ReportExport>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ReportExport> builder)
    {
        builder.ToTable("report_exports");

        builder.Property(export => export.Report).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(export => export.Format).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(export => export.BranchId).HasMaxLength(40);
        builder.Property(export => export.RequestedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(export => export.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(export => export.FileKey).HasMaxLength(250);
        builder.Property(export => export.FileName).HasMaxLength(120);
        builder.Property(export => export.ErrorCode).HasMaxLength(60);

        builder.PrimitiveCollection(export => export.BranchIds)
            .HasField("_branchIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("branch_ids")
            .IsRequired();

        // The worker's queue: the oldest queued job first. Partial, because finished jobs pile up
        // and are never queued again.
        builder.HasIndex(export => export.CreatedAt)
            .HasFilter("status = 'Queued'")
            .HasDatabaseName("ix_report_exports_queue");

        builder.HasIndex(export => export.StartedAt)
            .HasFilter("status = 'Running'")
            .HasDatabaseName("ix_report_exports_running");
    }
}
