using Microsoft.EntityFrameworkCore;
using HappyPaws.Domain.Access;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Access;

internal sealed class RoleConfiguration : EntityConfiguration<Role>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.Property(role => role.Name).HasMaxLength(64).IsRequired();
        builder.HasIndex(role => role.Name).IsUnique();

        // Scopes are a value list owned by the role, replaced as a whole set by
        // PUT /staff/roles/{id}/permissions — not an association with independent entities. A
        // PostgreSQL text[] models that directly and is read in the same query as the role.
        builder.PrimitiveCollection(role => role.Scopes)
            .HasColumnName("scopes")
            .IsRequired();
    }
}
