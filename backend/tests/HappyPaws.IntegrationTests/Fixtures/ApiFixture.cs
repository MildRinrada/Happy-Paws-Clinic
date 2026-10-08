using HappyPaws.Application.Identity;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Staff;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Probes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Boots the real API against a real PostgreSQL container, with migrations applied.
/// <para>
/// A real database rather than an in-memory provider, because the things most worth testing here
/// only exist in PostgreSQL: the <c>xmin</c> concurrency token behind <c>If-Match</c>, the
/// primary-key race that makes idempotency claims atomic, and the actual DDL the migrations
/// produce. An in-memory provider would pass while all three were broken.
/// </para>
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("happypaws_test")
        .Build();

    private ClinicApiFactory? _factory;

    public IServiceProvider Services => _factory!.Services;

    public HttpClient CreateClient() => _factory!.CreateClient();

    /// <summary>For a test that must control the cookies it sends, rather than replay what it was given.</summary>
    public HttpClient CreateClient(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions options) =>
        _factory!.CreateClient(options);

    public IServiceScope CreateScope() => Services.CreateScope();

    /// <summary>The stand-in channel, for asserting on what did and did not reach a customer.</summary>
    public RecordingNotificationChannel Notifications =>
        (RecordingNotificationChannel)Services.GetRequiredService<INotificationChannel>();

    /// <summary>The stand-in email adapter, for following the link a staff invitation carried.</summary>
    public RecordingStaffInvitationSender Invitations =>
        (RecordingStaffInvitationSender)Services.GetRequiredService<IStaffInvitationSender>();

    /// <summary>The stand-in SMS and email for customer sign-in, for reading codes and reset links.</summary>
    public RecordingSignInSenders SignIn => Services.GetRequiredService<RecordingSignInSenders>();

    /// <summary>The stand-in model server behind the website assistant.</summary>
    public ModelScript Model => Services.GetRequiredService<ModelScript>();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new ClinicApiFactory(_postgres.GetConnectionString(), ConfigureServices);

        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Runs after the standard test wiring, for a fixture that needs a different stand-in.</summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}

/// <summary>The host itself, configured for tests.</summary>
internal sealed class ClinicApiFactory(string connectionString, Action<IServiceCollection>? configure = null)
    : WebApplicationFactory<Program>
{
    /// <summary>Meets the 32-character minimum the options validator enforces.</summary>
    public const string SigningKey = "integration-test-signing-key-0123456789";

    public const string Issuer = "https://test.happypaws.local";
    public const string Audience = "happypaws-api";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development turns on validate-on-build, so a broken service graph fails here rather
        // than on a production request.
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,
                ["Auth:Jwt:Issuer"] = Issuer,
                ["Auth:Jwt:Audience"] = Audience,
                ["Auth:Jwt:SigningKey"] = SigningKey,
                ["Clinic:TimeZone"] = "Asia/Bangkok",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",

                // No scheduler in tests: a reminder job firing on its own would send messages to
                // other tests' customers at moments nobody chose. Tests drive the jobs directly.
                ["Jobs:Enabled"] = "false",

                // The assistant talks to a scripted stand-in through the real OpenAI-format adapter.
                ["Assistant:Provider"] = "openai",
                ["Assistant:BaseUrl"] = "http://model.test/v1",
                ["Assistant:Model"] = "test-model",
                ["Assistant:ApiKey"] = "test-key",
                ["Assistant:MessagesPerMinute"] = "1000",
                ["Assistant:DailyConversationTurns"] = "0",
            }));

        // Unhandled faults become a 500 with no detail in the body, by design. Without a log sink
        // the test run would report "expected OK, got InternalServerError" and nothing else, so
        // the host's own logging is kept and written where a failing run can be read.
        builder.ConfigureLogging(logging => logging
            .ClearProviders()
            .AddSimpleConsole(options => options.SingleLine = true)
            .SetMinimumLevel(LogLevel.Warning));

        builder.ConfigureServices(services =>
        {
            services.Configure<ServiceProviderOptions>(options =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            });

            // Probe endpoints live in this test assembly and never ship. They exercise the
            // cross-cutting pipeline before any real controller exists.
            services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);

            // The channel is replaced; the dispatcher in front of it is not. Everything still goes
            // through the real consent gate, which is what the suppression tests are actually about.
            services.RemoveAll<INotificationChannel>();
            services.AddSingleton<INotificationChannel, RecordingNotificationChannel>();

            // Same idea for staff invitations: the outermost adapter is swapped so a test can read
            // the link, and everything that decides whether to send one is left real.
            services.RemoveAll<IStaffInvitationSender>();
            services.AddSingleton<IStaffInvitationSender, RecordingStaffInvitationSender>();

            // And for the customer's phone and inbox: codes and reset links are read back here.
            services.RemoveAll<IOtpSender>();
            services.RemoveAll<IPasswordResetSender>();
            services.AddSingleton<RecordingSignInSenders>();
            services.AddSingleton<IOtpSender>(provider => provider.GetRequiredService<RecordingSignInSenders>());
            services.AddSingleton<IPasswordResetSender>(provider => provider.GetRequiredService<RecordingSignInSenders>());

            // The model server is the one outside dependency of the assistant; it is scripted.
            services.AddSingleton<ModelScript>();
            services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(
                provider => new ScriptedModelHandler(provider.GetRequiredService<ModelScript>())));

            configure?.Invoke(services);
        });
    }
}

[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;

/// <summary>
/// A second, separate clinic: its own container, its own database, its own host.
/// <para>
/// For the tests that change something there is only one of. Queue numbering, the medical-record
/// rules, the clinic's opening hours and "is there an owner" are all instance-wide, so a test that
/// changed them in <see cref="ApiCollection"/> would change them underneath two hundred other
/// tests — and those would then pass or fail depending on what happened to run first.
/// </para>
/// <para>
/// The same fixture type, deliberately. xUnit builds one fixture per collection, so naming a second
/// collection is all it takes to get a second instance, and the two never see each other's rows.
/// </para>
/// </summary>
[CollectionDefinition(nameof(FreshInstanceCollection))]
public sealed class FreshInstanceCollection : ICollectionFixture<ApiFixture>;
