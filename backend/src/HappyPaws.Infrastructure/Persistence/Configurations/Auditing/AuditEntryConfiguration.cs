using HappyPaws.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Auditing;

/// <summary>
/// The audit table.
/// <para>
/// Deliberately carries <b>no</b> foreign keys to the rows it describes. An audit entry must
/// survive the deletion of whatever it points at — that is most of its value — and a cascade or a
/// restrict would either erase the evidence or block a legitimate erasure request under the PDPA.
/// The ids are recorded as text, and a dangling one is a fact worth keeping.
/// </para>
/// </summary>
internal sealed class AuditEntryConfiguration : EntityConfiguration<AuditEntry>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.Property(entry => entry.Action).HasMaxLength(64).IsRequired();
        builder.Property(entry => entry.ResourceType).HasMaxLength(40).IsRequired();
        builder.Property(entry => entry.ResourceId).HasMaxLength(40).IsRequired();
        builder.Property(entry => entry.ActorUserId).HasMaxLength(40);
        builder.Property(entry => entry.ActorStaffId).HasMaxLength(40);
        builder.Property(entry => entry.ActorCustomerId).HasMaxLength(40);
        builder.Property(entry => entry.BranchId).HasMaxLength(40);
        builder.Property(entry => entry.PetId).HasMaxLength(40);
        builder.Property(entry => entry.IpAddress).HasMaxLength(64);
        builder.Property(entry => entry.UserAgent).HasMaxLength(400);
        builder.Property(entry => entry.RequestId).HasMaxLength(80);
        builder.Property(entry => entry.OccurredAt).IsRequired();

        builder.Property(entry => entry.DetailsJson).HasColumnType("jsonb");

        // The page order of both audit endpoints.
        builder.HasIndex(entry => entry.OccurredAt).IsDescending();

        // "การเปิดดูเวชระเบียนของคนไข้แต่ละราย" — the access log is read per patient.
        builder.HasIndex(entry => new { entry.PetId, entry.OccurredAt });

        // "ใครแก้อะไรเมื่อไหร่" for one record, and the who-did-what view.
        builder.HasIndex(entry => new { entry.ResourceType, entry.ResourceId, entry.OccurredAt });
        builder.HasIndex(entry => new { entry.ActorUserId, entry.OccurredAt });
        builder.HasIndex(entry => new { entry.Action, entry.OccurredAt });
    }
}
