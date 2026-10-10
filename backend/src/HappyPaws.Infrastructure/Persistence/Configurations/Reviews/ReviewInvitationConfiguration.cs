using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Reviews;

/// <summary>
/// Review invitations, at most one per visit (<c>docs/contract-extensions.md § 36</c>). The token is
/// never stored: <c>token_hash</c> is its SHA-256, and null for a customer sent the portal link.
/// </summary>
internal sealed class ReviewInvitationConfiguration : EntityConfiguration<ReviewInvitation>
{
    /// <summary>One invitation per visit, the guarantee behind "one invitation per visit at most".</summary>
    public const string VisitIndex = "ix_review_invitations_visit";

    protected override void ConfigureEntity(EntityTypeBuilder<ReviewInvitation> builder)
    {
        builder.ToTable("review_invitations");

        builder.Property(invitation => invitation.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(invitation => invitation.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(invitation => invitation.VisitId).HasMaxLength(40).IsRequired();
        builder.Property(invitation => invitation.PetId).HasMaxLength(40);
        builder.Property(invitation => invitation.ServiceName).HasMaxLength(300);
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(64);
        builder.Property(invitation => invitation.DispatchId).HasMaxLength(40);
        builder.Property(invitation => invitation.ReviewId).HasMaxLength(40);

        builder.HasOne<Customer>().WithMany().HasForeignKey(invitation => invitation.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(invitation => invitation.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invitation => new { invitation.CustomerId, invitation.BranchId, invitation.VisitOn })
            .IsUnique()
            .HasDatabaseName(VisitIndex);

        builder.HasIndex(invitation => invitation.TokenHash)
            .IsUnique()
            .HasFilter("token_hash IS NOT NULL");
    }
}
