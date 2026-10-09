using HappyPaws.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Integrations;

/// <summary>The payment provider's one row. Credentials are ciphertext.</summary>
internal sealed class PaymentProviderSettingsConfiguration : EntityConfiguration<PaymentProviderSettings>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PaymentProviderSettings> builder)
    {
        builder.ToTable("payment_provider_settings");

        builder.Property(settings => settings.Provider).HasMaxLength(30);
        builder.Property(settings => settings.Mode).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(settings => settings.Enabled).IsRequired();
        builder.Property(settings => settings.PublicKey).HasMaxLength(PaymentProviderSettings.PublicKeyMaxLength);
        builder.Property(settings => settings.SecretKeyCiphertext).HasMaxLength(2_000);
        builder.Property(settings => settings.WebhookSecretCiphertext).HasMaxLength(2_000);

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_payment_provider_settings_singleton",
            $"id = '{PaymentProviderSettings.SingletonId}'"));
    }
}

/// <summary>The lab provider's one row. The API key is ciphertext.</summary>
internal sealed class LabProviderSettingsConfiguration : EntityConfiguration<LabProviderSettings>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LabProviderSettings> builder)
    {
        builder.ToTable("lab_provider_settings");

        builder.Property(settings => settings.Provider).HasMaxLength(30);
        builder.Property(settings => settings.Enabled).IsRequired();
        builder.Property(settings => settings.AccountId).HasMaxLength(LabProviderSettings.AccountIdMaxLength);
        builder.Property(settings => settings.BaseUrl).HasMaxLength(WebhookUrl.MaxLength);
        builder.Property(settings => settings.ApiKeyCiphertext).HasMaxLength(2_000);

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_lab_provider_settings_singleton",
            $"id = '{LabProviderSettings.SingletonId}'"));
    }
}

/// <summary>Calendar links: one per staff member, found by the token's hash.</summary>
internal sealed class CalendarFeedConfiguration : EntityConfiguration<CalendarFeed>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CalendarFeed> builder)
    {
        builder.ToTable("calendar_feeds");

        builder.Property(feed => feed.StaffId).HasMaxLength(40).IsRequired();
        builder.Property(feed => feed.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(feed => feed.TokenCiphertext).HasMaxLength(1_000).IsRequired();

        builder.HasIndex(feed => feed.StaffId).IsUnique();
        builder.HasIndex(feed => feed.TokenHash).IsUnique();
    }
}
