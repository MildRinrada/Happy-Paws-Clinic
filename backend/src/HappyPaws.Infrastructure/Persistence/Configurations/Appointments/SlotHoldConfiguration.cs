using HappyPaws.Domain.Appointments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Appointments;

internal sealed class SlotHoldConfiguration : EntityConfiguration<SlotHold>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SlotHold> builder)
    {
        builder.ToTable("slot_holds");

        builder.Property(hold => hold.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(hold => hold.ServiceId).HasMaxLength(40).IsRequired();
        builder.Property(hold => hold.StaffId).HasMaxLength(40).IsRequired();
        builder.Property(hold => hold.HeldByCustomerId).HasMaxLength(40).IsRequired();

        // Every availability read and every booking check joins on live holds only, so the index
        // is partial: spent and released holds are dead weight the moment they are written, and a
        // busy clinic accumulates far more of those than live ones.
        builder.HasIndex(hold => new { hold.BranchId, hold.StaffId, hold.StartsAt })
            .HasFilter("consumed_at IS NULL AND released_at IS NULL");

        builder.HasIndex(hold => hold.ExpiresAt);
    }
}
