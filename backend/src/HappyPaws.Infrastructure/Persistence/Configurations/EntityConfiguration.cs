using HappyPaws.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base configuration every aggregate root inherits, so the mechanics the contract requires are
/// applied once instead of being re-typed — and forgotten — per entity.
/// <para>
/// It wires the <c>version</c> / <c>If-Match</c> mechanic to PostgreSQL's system column
/// <c>xmin</c>, which PostgreSQL already maintains on every row. No trigger, no manual increment,
/// and no way for a hand-written UPDATE to bypass it.
/// </para>
/// </summary>
public abstract class EntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : Entity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasMaxLength(40)
            .IsRequired();

        // Maps Entity.Version onto PostgreSQL's xmin. EF then adds it to every UPDATE's WHERE
        // clause and raises DbUpdateConcurrencyException when no row matches — which the API
        // turns into 409 version_conflict.
        builder.Property(entity => entity.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.UpdatedAt);

        ConfigureEntity(builder);
    }

    /// <summary>Per-entity mapping: columns, relationships, indexes.</summary>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
