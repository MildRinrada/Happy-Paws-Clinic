using HappyPaws.Domain.Automation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Automation;

internal sealed class AutomationDispatchConfiguration : EntityConfiguration<AutomationDispatch>
{
    /// <summary>The "already sent" key. Named, because the claim's <c>ON CONFLICT</c> refers to it.</summary>
    public const string UniqueKey = "ux_automation_dispatches_reminder";

    protected override void ConfigureEntity(EntityTypeBuilder<AutomationDispatch> builder)
    {
        builder.ToTable("automation_dispatches");

        builder.Property(item => item.RuleId).HasMaxLength(40).IsRequired();
        builder.Property(item => item.SubjectKind).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(item => item.SubjectId).HasMaxLength(60).IsRequired();
        builder.Property(item => item.DueKey).HasMaxLength(AutomationDispatch.DueKeyMax).IsRequired();
        builder.Property(item => item.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(item => item.PetId).HasMaxLength(40);
        builder.Property(item => item.BranchId).HasMaxLength(40);
        builder.Property(item => item.ClaimedBy).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(item => item.ErrorCode).HasMaxLength(60);

        // The guarantee that one reminder goes out once, across every API instance: a worker owns
        // a reminder only if its insert of this key succeeded.
        builder.HasIndex(item => new { item.RuleId, item.SubjectKind, item.SubjectId, item.DueKey })
            .IsUnique()
            .HasDatabaseName(UniqueKey);

        // Stale claims are looked for every tick; the set is tiny, so the index is partial.
        builder.HasIndex(item => item.ClaimedAt)
            .HasFilter("status = 'Claimed'")
            .HasDatabaseName("ix_automation_dispatches_open_claims");

        // The per-rule history on the rule list.
        builder.HasIndex(item => new { item.RuleId, item.ClaimedAt });

        builder.HasOne<AutomationRule>().WithMany()
            .HasForeignKey(item => item.RuleId).OnDelete(DeleteBehavior.Restrict);
    }
}
