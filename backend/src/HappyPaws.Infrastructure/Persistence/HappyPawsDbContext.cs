using HappyPaws.Infrastructure.Persistence.Converters;
using HappyPaws.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context for this clinic instance.
/// <para>
/// Entities arrive per module and are picked up by the configuration scan below, so this file
/// never becomes a thousand-line registry of <c>DbSet</c> properties. The only set declared here
/// is the idempotency table, which is owned by the infrastructure itself rather than by any
/// module.
/// </para>
/// <para>
/// This type is internal to Infrastructure. Controllers must never depend on it — they call
/// Application services, which call repositories.
/// </para>
/// </summary>
public class HappyPawsDbContext(DbContextOptions<HappyPawsDbContext> options) : DbContext(options)
{
    internal DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Trigram indexes for the counter's search boxes. "ค้นด้วยชื่อ เบอร์ HN หรือชื่อสัตว์" is an
        // infix match — someone types "ลูน่า", not "ลูน่า%" — and no b-tree can serve `ILIKE '%…%'`.
        // Declared here rather than in a migration so the model and the database agree about it.
        modelBuilder.HasPostgresExtension("pg_trgm");

        // Each module owns its configuration class under Persistence/Configurations/<Module>/.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HappyPawsDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Money crosses the API as an integer number of satang and is stored the same way.
        // Registered as a convention so no mapping can quietly reintroduce a floating point column.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);

        // Every timestamp is absolute. PostgreSQL timestamptz plus DateTimeOffset means a value
        // cannot be stored without knowing what instant it refers to.
        //
        // The converter is not optional decoration: Npgsql throws on any offset but zero, and the
        // contract's wire format is +07:00. See UtcDateTimeOffsetConverter.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveColumnType("timestamptz")
            .HaveConversion<UtcDateTimeOffsetConverter>();
    }
}
