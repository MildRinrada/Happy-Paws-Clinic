using HappyPaws.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Customers;

/// <summary>
/// Portal invitations.
/// <para>
/// One live row per customer, and re-inviting refreshes it rather than adding another. The table
/// answers "have we asked this person, and is that still open" — a working set, not a log. What
/// happened historically is in the audit trail under <c>customer.invited</c>, which is the right
/// place for it and the one nobody can edit.
/// </para>
/// </summary>
internal sealed class CustomerInvitationConfiguration : EntityConfiguration<CustomerInvitation>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CustomerInvitation> builder)
    {
        builder.ToTable("customer_invitations");

        builder.Property(invitation => invitation.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(invitation => invitation.SentTo).HasMaxLength(256).IsRequired();
        builder.Property(invitation => invitation.InvitedByStaffId).HasMaxLength(40).IsRequired();

        builder.HasIndex(invitation => invitation.CustomerId).IsUnique();

        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(invitation => invitation.CustomerId)
            // Cascade: an invitation is meaningless without the record it invites, and unlike a
            // clinical row there is nothing here anyone needs to read back afterwards.
            .OnDelete(DeleteBehavior.Cascade);
    }
}
