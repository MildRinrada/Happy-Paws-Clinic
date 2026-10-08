using HappyPaws.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Integrations;

/// <summary>
/// The LINE channel's one row. Same singleton check as the clinic's other settings tables.
/// <para>
/// The secret and the access token are plain <c>text</c>. Encrypting them at rest is open work,
/// as it is for the TOTP secrets; see <c>docs/system-overview.md</c>.
/// </para>
/// </summary>
internal sealed class LineChannelSettingsConfiguration : EntityConfiguration<LineChannelSettings>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LineChannelSettings> builder)
    {
        builder.ToTable("line_channel_settings");

        builder.Property(settings => settings.ChannelId).HasMaxLength(20);
        builder.Property(settings => settings.ChannelSecret).HasMaxLength(200);
        builder.Property(settings => settings.ChannelAccessToken).HasMaxLength(1_000);
        builder.Property(settings => settings.Enabled).IsRequired();
        builder.Property(settings => settings.BroadcastMonthlyLimit).IsRequired();

        builder.PrimitiveCollection(settings => settings.DisabledTemplates)
            .HasColumnName("disabled_templates")
            .IsRequired();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_line_channel_settings_singleton",
            $"id = '{LineChannelSettings.SingletonId}'"));
    }
}
