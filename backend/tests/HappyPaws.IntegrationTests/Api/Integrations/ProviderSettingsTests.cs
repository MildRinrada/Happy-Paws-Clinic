using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// <c>PUT /staff/integrations/payment</c> and <c>/lab</c>: settings stored and checked for shape,
/// secrets written but never read back, encrypted at rest, <c>If-Match</c> honoured, audited.
/// </summary>
[Collection(nameof(IntegrationsCollection))]
public class ProviderSettingsTests(IntegrationsFixture fixture)
{
    private const string SecretKey = "skey_test_5xvkB9aLm2Qp7RtZ";
    private const string WebhookSecret = "whsec_provider_8Hq2Lk";
    private const string LabKey = "lab_key_3nG7Ws1Yd";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/staff/integrations/payment")]
    [InlineData("/staff/integrations/lab")]
    public async Task Configuring_a_provider_needs_integration_manage(string path)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Settings.Write], [clinic.BranchId]));

        var response = await client.PutAsJsonAsync(path, new { provider = "opn" }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("""{ "provider": "Opn Payments!" }""")]
    [InlineData("""{ "mode": "production" }""")]
    [InlineData("""{ "secret_key": "has a space" }""")]
    [InlineData("""{ "public_key": "pk with space" }""")]
    public async Task Malformed_payment_settings_are_refused(string body)
    {
        using var client = await ManagerAsync();

        var response = await client.PutAsync("/staff/integrations/payment", Json(body), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("""{ "base_url": "http://api.lab.example" }""")]
    [InlineData("""{ "base_url": "https://10.0.0.9/api" }""")]
    [InlineData("""{ "base_url": "https://localhost/api" }""")]
    [InlineData("""{ "provider": "-" }""")]
    public async Task Malformed_lab_settings_are_refused(string body)
    {
        using var client = await ManagerAsync();

        var response = await client.PutAsync("/staff/integrations/lab", Json(body), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Payment_secrets_are_written_never_read_and_stored_encrypted()
    {
        using var client = await ManagerAsync();

        var response = await client.PutAsJsonAsync("/staff/integrations/payment", new
        {
            provider = "opn",
            mode = "test",
            public_key = "pkey_test_public",
            secret_key = SecretKey,
            webhook_secret = WebhookSecret,
            enabled = true,
        }, Token);

        var raw = await response.Content.ReadAsStringAsync(Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(SecretKey, raw, StringComparison.Ordinal);
        Assert.DoesNotContain(WebhookSecret, raw, StringComparison.Ordinal);
        Assert.NotNull(response.Headers.ETag);

        var settings = JsonDocument.Parse(raw).RootElement;
        Assert.Equal("opn", settings.GetProperty("provider").GetString());
        Assert.Equal("test", settings.GetProperty("mode").GetString());
        Assert.Equal("pkey_test_public", settings.GetProperty("public_key").GetString());
        Assert.True(settings.GetProperty("has_secret_key").GetBoolean());
        Assert.True(settings.GetProperty("has_webhook_secret").GetBoolean());
        Assert.True(settings.GetProperty("enabled").GetBoolean());
        Assert.False(settings.GetProperty("connected").GetBoolean());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var stored = await db.Set<PaymentProviderSettings>().AsNoTracking().SingleAsync(Token);
        Assert.DoesNotContain(SecretKey, stored.SecretKeyCiphertext!, StringComparison.Ordinal);
        Assert.DoesNotContain(WebhookSecret, stored.WebhookSecretCiphertext!, StringComparison.Ordinal);

        var audit = await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.Action == AuditActions.PaymentIntegrationConfigured)
            .OrderByDescending(entry => entry.OccurredAt)
            .FirstAsync(Token);
        Assert.DoesNotContain(SecretKey, audit.DetailsJson!, StringComparison.Ordinal);
        Assert.Contains("secret_key_changed", audit.DetailsJson!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_empty_body_reads_back_a_stale_version_conflicts_and_an_empty_string_clears()
    {
        using var client = await ManagerAsync();

        var written = await (await client.PutAsJsonAsync("/staff/integrations/payment", new { provider = "opn", secret_key = SecretKey }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);
        var version = written.GetProperty("version").GetUInt32();

        var read = await client.PutAsJsonAsync("/staff/integrations/payment", new { }, Token);
        Assert.Equal(version, (await read.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("version").GetUInt32());

        using var stale = new HttpRequestMessage(HttpMethod.Put, "/staff/integrations/payment") { Content = JsonContent.Create(new { mode = "live" }) };
        stale.Headers.TryAddWithoutValidation("If-Match", $"\"{version + 1000}\"");
        var conflict = await client.SendAsync(stale, Token);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("version_conflict", await IntegrationsScenario.ErrorCodeAsync(conflict, Token));

        var cleared = await (await client.PutAsJsonAsync("/staff/integrations/payment", new { enabled = false, secret_key = "" }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.False(cleared.GetProperty("has_secret_key").GetBoolean());
        Assert.Equal("opn", cleared.GetProperty("provider").GetString());
    }

    [Fact]
    public async Task Payments_cannot_be_switched_on_without_a_provider_and_a_secret()
    {
        using var client = await ManagerAsync();
        await client.PutAsJsonAsync("/staff/integrations/payment", new { enabled = false, provider = "", secret_key = "" }, Token);

        var response = await client.PutAsJsonAsync("/staff/integrations/payment", new { enabled = true }, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("invalid_payment_settings", await IntegrationsScenario.ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task The_lab_key_is_written_never_read_and_stored_encrypted()
    {
        using var client = await ManagerAsync();

        var response = await client.PutAsJsonAsync("/staff/integrations/lab", new
        {
            provider = "idexx",
            account_id = "TH-12345",
            base_url = "https://api.lab.example/v2",
            api_key = LabKey,
            enabled = true,
        }, Token);

        var raw = await response.Content.ReadAsStringAsync(Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(LabKey, raw, StringComparison.Ordinal);

        var settings = JsonDocument.Parse(raw).RootElement;
        Assert.Equal("idexx", settings.GetProperty("provider").GetString());
        Assert.Equal("TH-12345", settings.GetProperty("account_id").GetString());
        Assert.Equal("https://api.lab.example/v2", settings.GetProperty("base_url").GetString());
        Assert.True(settings.GetProperty("has_api_key").GetBoolean());
        Assert.False(settings.GetProperty("connected").GetBoolean());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var stored = await db.Set<LabProviderSettings>().AsNoTracking().SingleAsync(Token);
        Assert.DoesNotContain(LabKey, stored.ApiKeyCiphertext!, StringComparison.Ordinal);

        Assert.True(await db.Set<AuditEntry>().AnyAsync(entry => entry.Action == AuditActions.LabIntegrationConfigured, Token));
    }

    private async Task<HttpClient> ManagerAsync()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        return fixture.Integrator(clinic);
    }

    private static StringContent Json(string body) => new(body, System.Text.Encoding.UTF8, "application/json");
}
