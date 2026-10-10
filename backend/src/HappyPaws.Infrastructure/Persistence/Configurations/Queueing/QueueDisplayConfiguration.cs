using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Queueing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Queueing;

/// <summary>
/// Waiting-room screens (<c>docs/contract-extensions.md § 30</c>): hashes of the token and of the
/// setup code, never either value. Both are looked up by hash, so both are indexed, unique where set.
/// </summary>
internal sealed class QueueDisplayConfiguration : EntityConfiguration<QueueDisplay>
{
    protected override void ConfigureEntity(EntityTypeBuilder<QueueDisplay> builder)
    {
        builder.ToTable("queue_displays");

        builder.Property(display => display.Name).HasMaxLength(QueueDisplay.NameMaxLength).IsRequired();
        builder.Property(display => display.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(display => display.TokenHash).HasMaxLength(64);
        builder.Property(display => display.SetupCodeHash).HasMaxLength(64);
        builder.Property(display => display.CreatedByStaffId).HasMaxLength(40).IsRequired();

        builder.HasIndex(display => display.TokenHash)
            .IsUnique()
            .HasFilter("token_hash IS NOT NULL");

        builder.HasIndex(display => display.SetupCodeHash)
            .IsUnique()
            .HasFilter("setup_code_hash IS NOT NULL");

        // The list: one branch's screens, newest first.
        builder.HasIndex(display => new { display.BranchId, display.CreatedAt });

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(display => display.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
