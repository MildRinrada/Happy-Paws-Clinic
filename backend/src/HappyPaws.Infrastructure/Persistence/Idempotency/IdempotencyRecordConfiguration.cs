using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Idempotency;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        // The composite key is what makes the claim atomic: two concurrent retries race to INSERT
        // and exactly one wins on the primary key, so the loser knows a request is already in
        // flight without needing an advisory lock.
        builder.HasKey(record => new { record.Key, record.Endpoint, record.Subject });

        builder.Property(record => record.Key).HasMaxLength(80);
        builder.Property(record => record.Endpoint).HasMaxLength(200);
        builder.Property(record => record.Subject).HasMaxLength(40);
        builder.Property(record => record.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(record => record.ResponseBody).HasColumnType("jsonb");

        // Drives the expiry sweep.
        builder.HasIndex(record => record.ExpiresAt);
    }
}
