using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.StaffAi;

/// <summary>
/// The staff AI assistant's three tables (<c>docs/contract-extensions.md § 34</c>). Not domain
/// entities: a usage counter, a cache and a stored report carry no clinic rule of their own, so they
/// live here, like the idempotency records.
/// </summary>
internal sealed class StaffAiUsageRow
{
    /// <summary>The feature name of the row holding the month's total, which is what the limit is checked on.</summary>
    public const string Total = "*";

    /// <summary>Clinic month, <c>2026-10</c>.</summary>
    public required string Month { get; init; }

    /// <summary>A <c>StaffAiFeatures</c> value, or <see cref="Total"/>.</summary>
    public required string Feature { get; init; }

    public int Used { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>The newest AI summary of one pet, and the fingerprint of the data it was written from.</summary>
internal sealed class PetSummaryRow
{
    public required string PetId { get; init; }

    /// <summary>SHA-256 hex of the prompt version and the data block.</summary>
    public required string Fingerprint { get; set; }

    public required string Text { get; set; }

    /// <summary>The citations as JSON, <c>[{ "ref", "kind", "id", "date" }]</c>.</summary>
    public required string Citations { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }
}

/// <summary>One branch's morning brief for one clinic day.</summary>
internal sealed class DailyBriefRow
{
    public required string BranchId { get; init; }

    public DateOnly BriefDate { get; init; }

    /// <summary><c>generating</c>, <c>ready</c> or <c>failed</c>.</summary>
    public required string Status { get; set; }

    public string? FailureReason { get; set; }

    /// <summary>How many times a worker has claimed it. A failure is retried while this is under three.</summary>
    public int Attempts { get; set; }

    /// <summary>The numbers as JSON, snake_case, as the API returns them.</summary>
    public string? Numbers { get; set; }

    public string? Text { get; set; }

    public DateTimeOffset ClaimedAt { get; set; }

    public DateTimeOffset? GeneratedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

internal sealed class StaffAiUsageRowConfiguration : IEntityTypeConfiguration<StaffAiUsageRow>
{
    public void Configure(EntityTypeBuilder<StaffAiUsageRow> builder)
    {
        builder.ToTable("staff_ai_usage", table =>
            table.HasCheckConstraint("ck_staff_ai_usage_used_not_negative", "used >= 0"));

        // One row per month and feature: the key is what "INSERT ... ON CONFLICT" counts against.
        builder.HasKey(row => new { row.Month, row.Feature });
        builder.Property(row => row.Month).HasMaxLength(7);
        builder.Property(row => row.Feature).HasMaxLength(40);
    }
}

internal sealed class PetSummaryRowConfiguration : IEntityTypeConfiguration<PetSummaryRow>
{
    public void Configure(EntityTypeBuilder<PetSummaryRow> builder)
    {
        builder.ToTable("staff_ai_pet_summaries");

        builder.HasKey(row => row.PetId);
        builder.Property(row => row.PetId).HasMaxLength(40);
        builder.Property(row => row.Fingerprint).HasMaxLength(64).IsRequired();
        builder.Property(row => row.Text).IsRequired();
        builder.Property(row => row.Citations).HasColumnType("jsonb").IsRequired();

        builder.HasOne<Pet>().WithMany()
            .HasForeignKey(row => row.PetId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DailyBriefRowConfiguration : IEntityTypeConfiguration<DailyBriefRow>
{
    public void Configure(EntityTypeBuilder<DailyBriefRow> builder)
    {
        builder.ToTable("staff_ai_daily_briefs");

        // Once per branch per clinic day is the key itself: a second claim conflicts.
        builder.HasKey(row => new { row.BranchId, row.BriefDate });
        builder.Property(row => row.BranchId).HasMaxLength(40);
        builder.Property(row => row.Status).HasMaxLength(20).IsRequired();
        builder.Property(row => row.FailureReason).HasMaxLength(40);
        builder.Property(row => row.Numbers).HasColumnType("jsonb");

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(row => row.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
