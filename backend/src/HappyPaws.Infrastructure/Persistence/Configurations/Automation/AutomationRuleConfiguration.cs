using HappyPaws.Domain.Automation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Automation;

internal sealed class AutomationRuleConfiguration : EntityConfiguration<AutomationRule>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AutomationRule> builder)
    {
        builder.ToTable("automation_rules", table =>
        {
            // The two timing shapes are exclusive; the domain says so, and so does the table, so a
            // hand-written row cannot be half of each.
            table.HasCheckConstraint(
                "ck_automation_rules_one_offset",
                "(offset_days IS NULL) <> (offset_hours IS NULL)");
        });

        builder.Property(rule => rule.Name).HasMaxLength(AutomationRule.NameMax).IsRequired();
        builder.Property(rule => rule.Trigger).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(rule => rule.BranchId).HasMaxLength(40);
        builder.Property(rule => rule.Species).HasMaxLength(AutomationRule.FilterMax);
        builder.Property(rule => rule.VaccineName).HasMaxLength(AutomationRule.FilterMax);
        builder.Property(rule => rule.ServiceId).HasMaxLength(40);
        builder.Property(rule => rule.Channel).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(rule => rule.TemplateKey).HasMaxLength(60).IsRequired();
        builder.Property(rule => rule.CreatedByStaffId).HasMaxLength(40);

        // Deleted rules stay for their history (docs/contract-extensions.md § 19); every read skips them.
        builder.Property(rule => rule.DeletedAt);
        builder.Property(rule => rule.DeletedByStaffId).HasMaxLength(40);
        builder.Ignore(rule => rule.IsDeleted);

        builder.Ignore(rule => rule.Timing);
        builder.Ignore(rule => rule.SubjectKind);

        // The scheduler reads the switched-on rules every tick.
        builder.HasIndex(rule => rule.Enabled).HasFilter("enabled");
        builder.HasIndex(rule => rule.BranchId);
    }
}
