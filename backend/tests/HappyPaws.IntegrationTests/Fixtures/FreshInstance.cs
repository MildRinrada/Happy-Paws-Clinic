using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HappyPaws.Application.Clinics.Bootstrap;
using HappyPaws.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>The owner the bootstrap command created, and what that first run reported.</summary>
public sealed record BootstrappedOwner(
    string Email,
    string Password,
    string TotpSecret,
    string StaffId,
    string UserId,
    BootstrapResult FirstRun);

/// <summary>
/// Base for the tests that run on the separate instance.
/// <para>
/// Bootstraps it before the first test body runs, whichever class that turns out to be in. That is
/// the order a real clinic meets: the instance is bootstrapped, then it is used. It also keeps the
/// bootstrap's own report honest — "created the first branch" is only true if no test seeded a
/// branch ahead of it.
/// </para>
/// </summary>
public abstract class FreshInstanceTest : IAsyncLifetime
{
    /// <summary>The collection's fixture, as the test class received it.</summary>
    protected abstract ApiFixture Instance { get; }

    public async ValueTask InitializeAsync() => await FreshInstance.OwnerAsync(Instance);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Bootstraps an instance once, through the production handler, and signs people in the way a
/// browser would.
/// <para>
/// Bootstrapping is a once-per-database act, and the tests in a collection run in no promised
/// order. So the first test to ask triggers it and every later one is handed the same result —
/// which is also the first run's own report, the only one in which "seven roles were created" is
/// true.
/// </para>
/// </summary>
public static class FreshInstance
{
    public const string OwnerEmail = "owner@happypaws.test";
    public const string OwnerPassword = "correct-horse-battery-staple";

    private static readonly ConditionalWeakTable<ApiFixture, Lazy<Task<BootstrappedOwner>>> Owners = [];

    public static Task<BootstrappedOwner> OwnerAsync(ApiFixture fixture) =>
        Owners.GetValue(fixture, key => new Lazy<Task<BootstrappedOwner>>(() => BootstrapAsync(key))).Value;

    /// <summary>Runs the bootstrap command again, for asserting that a second run changes nothing.</summary>
    public static async Task<BootstrapResult> BootstrapAgainAsync(ApiFixture fixture, BootstrapClinicCommand command)
    {
        using var scope = fixture.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<BootstrapClinicHandler>().HandleAsync(command);
    }

    /// <summary>
    /// Password, then second factor, exactly as <c>/auth/staff/login</c> and <c>/auth/2fa/verify</c>
    /// require — so a test holding the returned client holds what a real session would.
    /// </summary>
    public static async Task<HttpClient> SignInAsync(
        ApiFixture fixture,
        string email,
        string password,
        string totpSecret,
        CancellationToken cancellationToken)
    {
        var client = fixture.CreateClient();

        var login = await client.PostAsJsonAsync("/auth/staff/login", new { email, password }, cancellationToken);
        login.EnsureSuccessStatusCode();

        var challenge = (await login.Content.ReadFromJsonAsync<JsonElement>(cancellationToken))
            .GetProperty("challenge").GetString();

        var verify = await client.PostAsJsonAsync(
            "/auth/2fa/verify",
            new { challenge, code = new TotpService().GenerateCode(totpSecret, DateTimeOffset.UtcNow) },
            cancellationToken);

        verify.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            (await verify.Content.ReadFromJsonAsync<JsonElement>(cancellationToken))
                .GetProperty("access_token").GetString());

        return client;
    }

    public static async Task<HttpClient> SignInAsOwnerAsync(ApiFixture fixture, CancellationToken cancellationToken)
    {
        var owner = await OwnerAsync(fixture);

        return await SignInAsync(fixture, owner.Email, owner.Password, owner.TotpSecret, cancellationToken);
    }

    private static async Task<BootstrappedOwner> BootstrapAsync(ApiFixture fixture)
    {
        var result = await BootstrapAgainAsync(fixture, new BootstrapClinicCommand(
            OwnerEmail, "คุณเจ้าของ คลินิก", OwnerPassword, "คลินิกทดสอบ", "main", "สาขาหลัก", ["room_1", "room_2"]));

        var issued = result.Owner
            ?? throw new InvalidOperationException("Bootstrap did not create an owner on a fresh database.");

        return new BootstrappedOwner(
            issued.Email, OwnerPassword, issued.TotpSecret, result.OwnerStaffId!, issued.UserId, result);
    }
}
