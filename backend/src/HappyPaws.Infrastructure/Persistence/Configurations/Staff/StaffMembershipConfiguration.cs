using Microsoft.EntityFrameworkCore;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Staff;

internal sealed class StaffMembershipConfiguration : EntityConfiguration<StaffMembership>
{
    protected override void ConfigureEntity(EntityTypeBuilder<StaffMembership> builder)
    {
        builder.ToTable("staff_memberships");

        builder.Property(membership => membership.UserId).HasMaxLength(40).IsRequired();
        builder.Property(membership => membership.StaffType).HasMaxLength(64).IsRequired();
        builder.Property(membership => membership.LicenseNo).HasMaxLength(64);
        builder.Property(membership => membership.Department).HasMaxLength(32).IsRequired();

        // The public profile (extension, § 10). Off the website until show_on_website is set.
        builder.Property(membership => membership.JobTitle).HasMaxLength(StaffPublicProfile.JobTitleMaxLength);
        builder.Property(membership => membership.Specialty).HasMaxLength(StaffPublicProfile.SpecialtyMaxLength);
        builder.Property(membership => membership.Bio).HasMaxLength(StaffPublicProfile.BioMaxLength);
        builder.Property(membership => membership.PhotoPath).HasMaxLength(StaffPublicProfile.PhotoPathMaxLength);
        builder.Ignore(membership => membership.PublicProfile);

        builder.PrimitiveCollection(membership => membership.RoleIds)
            .HasColumnName("role_ids")
            .IsRequired();

        builder.PrimitiveCollection(membership => membership.BranchIds)
            .HasColumnName("branch_ids")
            .IsRequired();

        // One staff membership per account in this clinic instance.
        builder.HasIndex(membership => membership.UserId).IsUnique();

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
