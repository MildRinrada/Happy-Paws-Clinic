using HappyPaws.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Identity;

internal sealed class PhoneVerificationConfiguration : EntityConfiguration<PhoneVerification>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PhoneVerification> builder)
    {
        builder.ToTable("phone_verifications");

        builder.Property(verification => verification.PhoneDigits).HasMaxLength(15).IsRequired();
        builder.Property(verification => verification.Purpose).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(verification => verification.CodeHash).HasMaxLength(64).IsRequired();

        // The rate limit counts recent codes per number; sign-in finds the newest one.
        builder.HasIndex(verification => new { verification.PhoneDigits, verification.CreatedAt });
    }
}

internal sealed class PasswordResetRequestConfiguration : EntityConfiguration<PasswordResetRequest>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PasswordResetRequest> builder)
    {
        builder.ToTable("password_reset_requests");

        builder.Property(request => request.UserId).HasMaxLength(40).IsRequired();
        builder.Property(request => request.TokenHash).HasMaxLength(64).IsRequired();

        builder.HasIndex(request => request.TokenHash).IsUnique();
        builder.HasIndex(request => new { request.UserId, request.CreatedAt });

        builder.HasOne<UserAccount>().WithMany()
            .HasForeignKey(request => request.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
