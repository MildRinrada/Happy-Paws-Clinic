using HappyPaws.Domain.Scheduling;
using HappyPaws.Domain.Staff;
using HappyPaws.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Scheduling;

internal sealed class StaffTimeOffConfiguration : EntityConfiguration<StaffTimeOff>
{
    protected override void ConfigureEntity(EntityTypeBuilder<StaffTimeOff> builder)
    {
        builder.ToTable("staff_time_off");

        builder.Property(timeOff => timeOff.StaffId).HasMaxLength(40).IsRequired();
        builder.Property(timeOff => timeOff.Reason).HasMaxLength(StaffTimeOff.ReasonMaxLength);
        builder.Property(timeOff => timeOff.RecordedByStaffId).HasMaxLength(40);

        // "Is this person away during this window?" is asked per person, by time.
        builder.HasIndex(timeOff => new { timeOff.StaffId, timeOff.StartsAt });

        builder.HasOne<StaffMembership>()
            .WithMany()
            .HasForeignKey(timeOff => timeOff.StaffId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AssignmentRulesConfiguration : EntityConfiguration<AssignmentRules>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AssignmentRules> builder)
    {
        builder.ToTable("assignment_rules");

        // The rules are one ordered value, written whole by PUT and read whole by booking.
        builder.Property(rules => rules.Rules)
            .HasField("_rules")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .IsRequired();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_assignment_rules_singleton",
            $"id = '{AssignmentRules.SingletonId}'"));
    }
}
