using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HappyPaws.Infrastructure.Persistence;

/// <summary>
/// Builds a context for <c>dotnet ef migrations</c> without booting the API.
/// <para>
/// Design-time tooling only needs the provider to generate correct SQL — it never opens the
/// connection. Supplying a placeholder here means migrations can be created and reviewed on a
/// machine with no database and no secrets configured, which is what makes them reviewable in a
/// pull request.
/// </para>
/// <para>
/// Override with <c>HAPPYPAWS_DESIGNTIME_CONNECTION</c> when scaffolding against a real database.
/// </para>
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HappyPawsDbContext>
{
    private const string Placeholder = "Host=localhost;Database=happypaws;Username=happypaws;Password=design-time";

    public HappyPawsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("HAPPYPAWS_DESIGNTIME_CONNECTION") ?? Placeholder;

        var options = new DbContextOptionsBuilder<HappyPawsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsAssembly(typeof(HappyPawsDbContext).Assembly.FullName)
                .MigrationsHistoryTable("__migrations"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new HappyPawsDbContext(options);
    }
}
