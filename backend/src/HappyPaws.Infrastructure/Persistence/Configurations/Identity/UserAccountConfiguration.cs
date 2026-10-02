using Microsoft.EntityFrameworkCore;
using HappyPaws.Domain.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserAccountConfiguration : EntityConfiguration<UserAccount>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");

        builder.Property(account => account.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(account => account.Email).HasMaxLength(256);
        builder.Property(account => account.PhoneNumber).HasMaxLength(32);
        builder.Property(account => account.PasswordHash).HasMaxLength(512);
        builder.Property(account => account.TwoFactorSecret).HasMaxLength(512);
        builder.Property(account => account.PendingTwoFactorSecret).HasMaxLength(512);

        builder.PrimitiveCollection(account => account.RecoveryCodeHashes)
            .HasField("_recoveryCodeHashes")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("recovery_code_hashes")
            .IsRequired();

        // PATCH /me. The migration gives existing accounts the defaults a new one starts with.
        builder.Property(account => account.Language).HasMaxLength(2).IsRequired();

        builder.Property(account => account.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(account => account.TwoFactorMethod)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        // Staff sign in by email and customers by phone, so both must be unique — a duplicate
        // would make "which account did they mean" a coin flip at the login prompt.
        builder.HasIndex(account => account.Email).IsUnique().HasFilter("email IS NOT NULL");
        builder.HasIndex(account => account.PhoneNumber).IsUnique().HasFilter("phone_number IS NOT NULL");
    }
}
