using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Scheduling;
using HappyPaws.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Scheduling;

internal sealed class StaffShiftConfiguration : EntityConfiguration<StaffShift>
{
    protected override void ConfigureEntity(EntityTypeBuilder<StaffShift> builder)
    {
        builder.ToTable("staff_shifts");

        builder.Property(shift => shift.StaffId).HasMaxLength(40).IsRequired();
        builder.Property(shift => shift.BranchId).HasMaxLength(40).IsRequired();

        // Availability asks "which shifts overlap this window at this branch?" on every booking
        // page load, so the index leads with the two columns that predicate uses.
        builder.HasIndex(shift => new { shift.BranchId, shift.StartsAt, shift.EndsAt });
        builder.HasIndex(shift => new { shift.StaffId, shift.StartsAt });

        builder.HasOne<StaffMembership>()
            .WithMany()
            .HasForeignKey(shift => shift.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(shift => shift.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
