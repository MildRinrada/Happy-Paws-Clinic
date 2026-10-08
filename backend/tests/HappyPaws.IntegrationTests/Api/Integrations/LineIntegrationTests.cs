using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// <c>PUT /staff/integrations/line</c>: what it stores, what it never gives back, and the empty body
/// that reads the settings because the contract has no <c>GET</c>.
/// </summary>
[Collection(nameof(DeliveryCollection))]
public class LineIntegrationTests(DeliveryFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Manager() => fixture.Notifier(Scopes.Integration.Manage);

    [Fact]
    public async Task The_answer_says_a_secret_is_set_and_never_what_it_is()
    {
        var raw = await (await Manager().PutAsJsonAsync("/staff/integrations/line", new
        {
            channel_id = "1650000000",
            channel_secret = NotificationScenario.ChannelSecret,
            channel_access_token = NotificationScenario.AccessToken,
            enabled = true,
        }, Token)).Content.ReadAsStringAsync(Token);

        Assert.DoesNotContain(NotificationScenario.ChannelSecret, raw);
        Assert.DoesNotContain(NotificationScenario.AccessToken, raw);

        var settings = JsonDocument.Parse(raw).RootElement;
        Assert.Equal("1650000000", settings.GetProperty("channel_id").GetString());
        Assert.True(settings.GetProperty("has_channel_secret").GetBoolean());
        Assert.True(settings.GetProperty("has_channel_access_token").GetBoolean());
        Assert.Equal("database", settings.GetProperty("secret_source").GetString());
        Assert.True(settings.GetProperty("can_push").GetBoolean());
        Assert.True(settings.GetProperty("can_verify").GetBoolean());
        Assert.Equal("/webhooks/line", settings.GetProperty("webhook_path").GetString());
    }

    [Fact]
    public async Task An_empty_body_reads_the_settings_without_writing()
    {
        var before = await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);

        var response = await Manager().PutAsJsonAsync("/staff/integrations/line", new { }, Token);
        var read = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(before.GetProperty("version").GetUInt32(), read.GetProperty("version").GetUInt32());
        Assert.Equal($"\"{read.GetProperty("version").GetUInt32()}\"", response.Headers.ETag?.Tag);
    }

    [Fact]
    public async Task Changing_one_switch_keeps_the_credentials_and_the_other_switches()
    {
        await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);

        var settings = await (await Manager().PutAsJsonAsync(
            "/staff/integrations/line",
            new { notification_types = new Dictionary<string, bool> { ["receipt.issued"] = false } },
            Token)).Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.True(settings.GetProperty("has_channel_secret").GetBoolean());
        Assert.False(settings.GetProperty("notification_types").GetProperty("receipt.issued").GetBoolean());
        Assert.True(settings.GetProperty("notification_types").GetProperty("queue.called").GetBoolean());

        await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);
    }

    [Fact]
    public async Task An_empty_string_clears_a_stored_token_and_LINE_can_no_longer_push()
    {
        await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);

        var settings = await (await Manager().PutAsJsonAsync(
            "/staff/integrations/line", new { channel_access_token = "" }, Token)).Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.False(settings.GetProperty("has_channel_access_token").GetBoolean());
        Assert.Equal("none", settings.GetProperty("token_source").GetString());
        Assert.False(settings.GetProperty("can_push").GetBoolean());

        await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);
    }

    [Fact]
    public async Task A_stale_version_is_refused()
    {
        var current = await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);

        using var request = new HttpRequestMessage(HttpMethod.Put, "/staff/integrations/line")
        {
            Content = JsonContent.Create(new { enabled = true }),
        };
        request.Headers.IfMatch.ParseAdd($"\"{current.GetProperty("version").GetUInt32() + 1000}\"");

        var response = await Manager().SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_change_is_audited_without_the_secret()
    {
        await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var entry = await db.Set<AuditEntry>().AsNoTracking()
            .Where(item => item.Action == AuditActions.LineIntegrationConfigured)
            .OrderByDescending(item => item.OccurredAt)
            .FirstAsync(Token);

        var details = JsonSerializer.Serialize(entry);
        Assert.DoesNotContain(NotificationScenario.ChannelSecret, details);
        Assert.DoesNotContain(NotificationScenario.AccessToken, details);
        Assert.Contains("channel_secret_changed", details);
    }
}
