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
/// <c>POST /staff/webhooks</c> and <c>GET /staff/webhooks/{id}/deliveries</c>: who may, which URLs,
/// and the signing secret that is shown once and stored encrypted.
/// </summary>
[Collection(nameof(IntegrationsCollection))]
public class WebhookSubscriptionTests(IntegrationsFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Creating_a_webhook_needs_integration_manage()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Settings.Write], [clinic.BranchId]));

        var response = await client.PostAsJsonAsync(
            "/staff/webhooks",
            new { url = "https://hooks.example.com/x", events = new[] { "invoice.issued" } },
            Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("insufficient_scope", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains(
            Scopes.Integration.Manage,
            body.GetProperty("error").GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(scope => scope.GetString()));
    }

    [Theory]
    [InlineData("http://hooks.example.com/x")]
    [InlineData("https://localhost/x")]
    [InlineData("https://10.0.0.5/x")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://[::1]/x")]
    [InlineData("https://user:secret@hooks.example.com/x")]
    [InlineData("https://db.internal/x")]
    public async Task Urls_that_are_not_https_to_a_public_host_are_refused(string url)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Integrator(clinic);

        var response = await client.PostAsJsonAsync("/staff/webhooks", new { url, events = new[] { "invoice.issued" } }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("validation_failed", await IntegrationsScenario.ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task Events_must_be_named_and_known()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Integrator(clinic);

        var none = await client.PostAsJsonAsync("/staff/webhooks", new { url = "https://hooks.example.com/x", events = Array.Empty<string>() }, Token);
        var unknown = await client.PostAsJsonAsync("/staff/webhooks", new { url = "https://hooks.example.com/x", events = new[] { "*" } }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, none.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
    }

    [Fact]
    public async Task The_secret_is_shown_once_stored_encrypted_and_kept_out_of_the_audit_log()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Integrator(clinic);
        var host = IntegrationsScenario.Host();

        var response = await client.PostAsJsonAsync(
            "/staff/webhooks",
            new { url = $"https://{host}/hooks?receiver_token=abc", events = new[] { "invoice.issued", "customer.created" } },
            Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var id = created.GetProperty("id").GetString()!;
        var secret = created.GetProperty("secret").GetString()!;

        Assert.StartsWith("whk_", id, StringComparison.Ordinal);
        Assert.StartsWith("whsec_", secret, StringComparison.Ordinal);
        Assert.Equal(["customer.created", "invoice.issued"], created.GetProperty("events").EnumerateArray().Select(e => e.GetString()!));
        Assert.Equal("X-Signature", created.GetProperty("signing").GetProperty("header").GetString());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var stored = await db.Set<WebhookSubscription>().AsNoTracking().SingleAsync(s => s.Id == id, Token);
        Assert.DoesNotContain(secret, stored.SecretCiphertext, StringComparison.Ordinal);
        Assert.DoesNotContain(secret["whsec_".Length..], stored.SecretCiphertext, StringComparison.Ordinal);

        var audit = await db.Set<AuditEntry>().AsNoTracking()
            .SingleAsync(entry => entry.Action == AuditActions.WebhookCreated && entry.ResourceId == id, Token);
        Assert.DoesNotContain(secret, audit.DetailsJson!, StringComparison.Ordinal);
        Assert.DoesNotContain("receiver_token", audit.DetailsJson!, StringComparison.Ordinal);
        Assert.Contains(host, audit.DetailsJson!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_delivery_log_needs_the_scope_and_an_existing_webhook()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var integrator = fixture.Integrator(clinic);
        var webhook = await IntegrationsScenario.CreateWebhookAsync(integrator, IntegrationsScenario.Host(), ["invoice.issued"], Token);
        var id = webhook.GetProperty("id").GetString();

        using var outsider = fixture.Authenticated(TestTokens.Staff(fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Audit.Read], [clinic.BranchId]));

        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/staff/webhooks/{id}/deliveries", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await integrator.GetAsync("/staff/webhooks/whk_nothing/deliveries", Token)).StatusCode);

        var empty = await integrator.GetFromJsonAsync<JsonElement>($"/staff/webhooks/{id}/deliveries", Token);
        Assert.Empty(empty.GetProperty("data").EnumerateArray());
    }
}
