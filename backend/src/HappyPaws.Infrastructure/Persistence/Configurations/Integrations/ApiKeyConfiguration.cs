using HappyPaws.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Integrations;

/// <summary>
/// API keys: the public prefix and the SHA-256 of the whole key. No column can hold the key
/// itself.
/// </summary>
internal sealed class ApiKeyConfiguration : EntityConfiguration<ApiKey>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");

        builder.Property(key => key.Name).HasMaxLength(ApiKey.NameMaxLength).IsRequired();
        builder.Property(key => key.Prefix).HasMaxLength(ApiKey.PrefixLength).IsRequired();
        builder.Property(key => key.SecretHash).HasMaxLength(64).IsRequired();
        builder.Property(key => key.ExpiresAt).IsRequired();
        builder.Property(key => key.CreatedByUserId).HasMaxLength(40).IsRequired();
        builder.Property(key => key.CreatedByStaffId).HasMaxLength(40).IsRequired();

        builder.PrimitiveCollection(key => key.Scopes)
            .HasField("_scopes")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("scopes")
            .IsRequired();

        builder.PrimitiveCollection(key => key.BranchIds)
            .HasField("_branchIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("branch_ids")
            .IsRequired();

        builder.HasIndex(key => key.Prefix).IsUnique();
        builder.HasIndex(key => key.SecretHash).IsUnique();
    }
}
