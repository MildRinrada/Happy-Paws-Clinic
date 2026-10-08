using HappyPaws.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Integrations;

/// <summary>
/// <c>line_followers</c>: recent followers of the clinic's LINE account, for the counter to pick a
/// customer's LINE address from (<c>docs/contract-extensions.md § 18</c>).
/// </summary>
internal sealed class LineFollowerConfiguration : EntityConfiguration<LineFollower>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LineFollower> builder)
    {
        builder.ToTable("line_followers");

        builder.Property(follower => follower.LineUserId).HasMaxLength(LineFollower.LineUserIdMax).IsRequired();
        builder.Property(follower => follower.DisplayName).HasMaxLength(LineFollower.DisplayNameMax);
        builder.Property(follower => follower.PictureUrl).HasMaxLength(LineFollower.PictureUrlMax);
        builder.Property(follower => follower.FollowedAt).IsRequired();
        builder.Property(follower => follower.LastSeenAt).IsRequired();
        builder.Property(follower => follower.UnfollowedAt);

        builder.Ignore(follower => follower.IsFollowing);

        // One row per LINE address: a follow, an unfollow and a message all find the same row.
        builder.HasIndex(follower => follower.LineUserId).IsUnique();

        // The counter's list (most recently seen first) and the hourly clean-up both read by this.
        builder.HasIndex(follower => follower.LastSeenAt);
    }
}
