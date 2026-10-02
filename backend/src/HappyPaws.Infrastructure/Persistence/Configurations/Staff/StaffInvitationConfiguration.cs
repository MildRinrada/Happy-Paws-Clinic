using HappyPaws.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Staff;

internal sealed class StaffInvitationConfiguration : EntityConfiguration<StaffInvitation>
{
    protected override void ConfigureEntity(EntityTypeBuilder<StaffInvitation> builder)
    {
        builder.ToTable("staff_invitations");

        builder.Property(invitation => invitation.Email).HasMaxLength(256).IsRequired();
        builder.Property(invitation => invitation.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(invitation => invitation.StaffType).HasMaxLength(64).IsRequired();
        builder.Property(invitation => invitation.LicenseNo).HasMaxLength(64);

        // Hex SHA-256. The token itself is never in this table — see StaffInvitation.
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(64).IsRequired();

        builder.Property(invitation => invitation.InvitedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(invitation => invitation.AcceptedUserId).HasMaxLength(40);
        builder.Property(invitation => invitation.PendingTwoFactorSecret).HasMaxLength(512);

        builder.PrimitiveCollection(invitation => invitation.RoleIds)
            .HasColumnName("role_ids")
            .IsRequired();

        builder.PrimitiveCollection(invitation => invitation.BranchIds)
            .HasColumnName("branch_ids")
            .IsRequired();

        // One open invitation per address. Re-inviting refreshes the row in place, and this is
        // what stops two concurrent invitations from each finding none and each writing one —
        // which would leave two live links to become the same person.
        builder.HasIndex(invitation => invitation.Email)
            .IsUnique()
            .HasFilter("accepted_at IS NULL");

        // The "invited" page of the staff list: open invitations, newest first.
        builder.HasIndex(invitation => new { invitation.AcceptedAt, invitation.CreatedAt });
    }
}
