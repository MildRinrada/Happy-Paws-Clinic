using Microsoft.EntityFrameworkCore;
using HappyPaws.Domain.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserSessionConfiguration : EntityConfiguration<UserSession>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.Property(session => session.UserId).HasMaxLength(40).IsRequired();
        builder.Property(session => session.RefreshTokenHash).HasMaxLength(64).IsRequired();
        builder.Property(session => session.PreviousRefreshTokenHash).HasMaxLength(64);
        builder.Property(session => session.UserAgent).HasMaxLength(256);
        builder.Property(session => session.IpAddress).HasMaxLength(64);

        // Every refresh is a lookup by this hash, on the hot path of every client every 15
        // minutes. Unique because a rotated token must never resolve to two sessions.
        builder.HasIndex(session => session.RefreshTokenHash).IsUnique();

        // Reuse detection looks the presented token up here too, so it needs its own index.
        builder.HasIndex(session => session.PreviousRefreshTokenHash);

        // Drives GET /me/sessions and the "revoke all others" sweep.
        builder.HasIndex(session => new { session.UserId, session.RevokedAt });

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
