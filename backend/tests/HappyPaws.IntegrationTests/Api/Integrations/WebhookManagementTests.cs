using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// <c>docs/contract-extensions.md § 22</c> for webhooks: the list, the edit with the create's URL
/// rules, switching off and deleting (nothing more is sent, the log stays), the new secret shown
/// once, and sending a failed delivery again exactly once.
/// </summary>
[Collection(nameof(IntegrationsCollection))]
public class WebhookManagementTests(IntegrationsFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Every_new_route_needs_integration_manage()
    {
        var (clinic, _, webhook) = await SubscribeAsync(["customer.created"]);
        var id = Id(webhook);
        using var outsider = fixture.Authenticated(TestTokens.Staff(fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Settings.Write], [clinic.BranchId]));

        var responses = new[]
        {
            await outsider.GetAsync("/staff/webhooks", Token),
            await outsider.PatchAsJsonAsync($"/staff/webhooks/{id}", new { active = false }, Token),
            await outsider.DeleteAsync($"/staff/webhooks/{id}", Token),
            await outsider.PostAsync($"/staff/webhooks/{id}/rotate-secret", null, Token),
            await outsider.PostAsync($"/staff/webhooks/{id}/deliveries/whd_x/redeliver", null, Token),
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        foreach (var response in responses)
        {
            Assert.Equal("insufficient_scope", await IntegrationsScenario.ErrorCodeAsync(response, Token));
        }
    }

    [Fact]
    public async Task The_list_shows_each_webhook_with_its_version_and_never_a_secret()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created", "invoice.issued"]);
        using var integrator = fixture.Integrator(clinic);

        var response = await integrator.GetAsync("/staff/webhooks", Token);
        var raw = await response.Content.ReadAsStringAsync(Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(webhook.GetProperty("secret").GetString()!, raw, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", raw, StringComparison.OrdinalIgnoreCase);

        var row = Row(JsonDocument.Parse(raw).RootElement, Id(webhook));
        Assert.Equal($"https://{host}/hooks/clinic", row.GetProperty("url").GetString());
        Assert.Equal(["customer.created", "invoice.issued"], row.GetProperty("events").EnumerateArray().Select(e => e.GetString()!));
        Assert.True(row.GetProperty("active").GetBoolean());
        Assert.True(row.GetProperty("version").GetUInt32() > 0);
        Assert.Equal(clinic.StaffId, row.GetProperty("created_by_staff_id").GetString());
    }

    [Theory]
    [InlineData("""{ "url": "http://hooks.example.com/x" }""", "url")]
    [InlineData("""{ "url": "https://10.0.0.5/x" }""", "url")]
    [InlineData("""{ "url": "https://169.254.169.254/latest/meta-data" }""", "url")]
    [InlineData("""{ "url": "https://db.internal/x" }""", "url")]
    [InlineData("""{ "url": "https://user:secret@hooks.example.com/x" }""", "url")]
    [InlineData("""{ "events": [] }""", "events")]
    [InlineData("""{ "events": ["*"] }""", "events")]
    public async Task An_edit_is_checked_by_the_create_rules(string body, string field)
    {
        var (clinic, _, webhook) = await SubscribeAsync(["customer.created"]);
        using var integrator = fixture.Integrator(clinic);

        var response = await integrator.PatchAsync(
            $"/staff/webhooks/{Id(webhook)}", new StringContent(body, System.Text.Encoding.UTF8, "application/json"), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("validation_failed", error.GetProperty("code").GetString());
        Assert.Equal(field, error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task An_edit_returns_the_webhook_honours_if_match_and_is_audited_once()
    {
        var (clinic, _, webhook) = await SubscribeAsync(["customer.created"]);
        using var integrator = fixture.Integrator(clinic);
        var id = Id(webhook);
        var version = Row(await integrator.GetFromJsonAsync<JsonElement>("/staff/webhooks", Token), id).GetProperty("version").GetUInt32();
        var newHost = IntegrationsScenario.Host();

        using var stale = new HttpRequestMessage(HttpMethod.Patch, $"/staff/webhooks/{id}") { Content = JsonContent.Create(new { description = "x" }) };
        stale.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version + 1000}\""));
        var conflict = await integrator.SendAsync(stale, Token);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("version_conflict", await IntegrationsScenario.ErrorCodeAsync(conflict, Token));

        using var edit = new HttpRequestMessage(HttpMethod.Patch, $"/staff/webhooks/{id}")
        {
            Content = JsonContent.Create(new { url = $"https://{newHost}/v2?receiver_token=abc", events = new[] { "pet.created", "customer.created" }, description = "" }),
        };
        edit.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
        var response = await integrator.SendAsync(edit, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal($"https://{newHost}/v2?receiver_token=abc", updated.GetProperty("url").GetString());
        Assert.Equal(["customer.created", "pet.created"], updated.GetProperty("events").EnumerateArray().Select(e => e.GetString()!));
        Assert.False(updated.TryGetProperty("description", out var description) && description.ValueKind != JsonValueKind.Null);
        Assert.NotEqual(version, updated.GetProperty("version").GetUInt32());
        Assert.Equal($"\"{updated.GetProperty("version").GetUInt32()}\"", response.Headers.ETag?.Tag);

        // Nothing changes, nothing is written.
        var same = await integrator.PatchAsJsonAsync($"/staff/webhooks/{id}", new { events = new[] { "customer.created", "pet.created" } }, Token);
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);

        var audits = await AuditsAsync(AuditActions.WebhookUpdated, id);
        var audit = Assert.Single(audits);
        Assert.Contains(newHost, audit.DetailsJson!, StringComparison.Ordinal);
        Assert.DoesNotContain("receiver_token", audit.DetailsJson!, StringComparison.Ordinal);
        Assert.Contains("\"url\"", audit.DetailsJson!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_switched_off_webhook_stops_what_is_queued_and_receives_nothing_new()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        var id = Id(webhook);
        fixture.Receiver.Answer(host, () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var integrator = fixture.Integrator(clinic);
        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);

        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);
        var waiting = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, id, Token));
        Assert.Equal(WebhookDeliveryStatus.Pending, waiting.Status);

        var off = await integrator.PatchAsJsonAsync($"/staff/webhooks/{id}", new { active = false }, Token);
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.False((await off.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("active").GetBoolean());

        var halted = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, id, Token));
        Assert.Equal(WebhookDeliveryStatus.Failed, halted.Status);
        Assert.Equal(WebhookDelivery.HaltedDisabled, halted.LastError);
        Assert.Equal(1, halted.Attempts);

        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await IntegrationsScenario.MakeDueAsync(fixture, halted.Id, Token);
        await fixture.DrainAsync(Token);

        Assert.Single(fixture.Receiver.To(host));
        Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, id, Token));

        // On again: new events flow; what happened while it was off is not replayed.
        fixture.Receiver.Answer(host, () => new HttpResponseMessage(HttpStatusCode.NoContent));
        Assert.Equal(HttpStatusCode.OK, (await integrator.PatchAsJsonAsync($"/staff/webhooks/{id}", new { active = true }, Token)).StatusCode);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);

        Assert.Equal(2, fixture.Receiver.To(host).Count);
        Assert.Equal(2, (await IntegrationsScenario.DeliveriesAsync(fixture, id, Token)).Count);
    }

    [Fact]
    public async Task A_delivery_claimed_before_the_switch_is_stopped_by_the_sender()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        var id = Id(webhook);
        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);

        await IntegrationsScenario.CreateCustomerAsync(counter, Token);

        using (var scope = fixture.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<HappyPaws.Application.Integrations.DispatchWebhooks.DispatchWebhooksHandler>().FanOutAsync(Token);

            // Off behind the sender's back, as if the switch were pressed between claim and send.
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            await db.Set<WebhookSubscription>().Where(s => s.Id == id).ExecuteUpdateAsync(set => set.SetProperty(s => s.Active, false), Token);
        }

        await fixture.DrainAsync(Token);

        Assert.Empty(fixture.Receiver.To(host));
        var stopped = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, id, Token));
        Assert.Equal(WebhookDeliveryStatus.Failed, stopped.Status);
        Assert.Equal(WebhookDelivery.HaltedDisabled, stopped.LastError);
        Assert.Equal(0, stopped.Attempts);
    }

    [Fact]
    public async Task A_deleted_webhook_receives_nothing_more_loses_its_secret_and_keeps_its_log()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        var id = Id(webhook);
        using var integrator = fixture.Integrator(clinic);
        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);

        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);
        fixture.Receiver.Answer(host, () => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);
        Assert.Equal(2, fixture.Receiver.To(host).Count);

        var deleted = await integrator.DeleteAsync($"/staff/webhooks/{id}", Token);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        foreach (var delivery in await IntegrationsScenario.DeliveriesAsync(fixture, id, Token))
        {
            await IntegrationsScenario.MakeDueAsync(fixture, delivery.Id, Token);
        }

        await fixture.DrainAsync(Token);
        Assert.Equal(2, fixture.Receiver.To(host).Count);

        var list = await integrator.GetFromJsonAsync<JsonElement>("/staff/webhooks", Token);
        Assert.DoesNotContain(id, list.GetProperty("data").EnumerateArray().Select(row => row.GetProperty("id").GetString()));

        var log = await integrator.GetFromJsonAsync<JsonElement>($"/staff/webhooks/{id}/deliveries", Token);
        var rows = log.GetProperty("data").EnumerateArray().ToList();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, row => row.GetProperty("status").GetString() == "succeeded");
        Assert.Contains(rows, row => row.GetProperty("last_error").GetString() == WebhookDelivery.HaltedDeleted);

        Assert.Equal(HttpStatusCode.NotFound, (await integrator.DeleteAsync($"/staff/webhooks/{id}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await integrator.PatchAsJsonAsync($"/staff/webhooks/{id}", new { active = true }, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await integrator.PostAsync($"/staff/webhooks/{id}/rotate-secret", null, Token)).StatusCode);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var stored = await db.Set<WebhookSubscription>().AsNoTracking().SingleAsync(s => s.Id == id, Token);
        Assert.Equal(string.Empty, stored.SecretCiphertext);
        Assert.False(stored.Active);
        Assert.Single(await AuditsAsync(AuditActions.WebhookDeleted, id));
    }

    [Fact]
    public async Task A_new_secret_is_shown_once_and_signs_from_then_on()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        var id = Id(webhook);
        var oldSecret = webhook.GetProperty("secret").GetString()!;
        using var integrator = fixture.Integrator(clinic);

        var response = await integrator.PostAsync($"/staff/webhooks/{id}/rotate-secret", null, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var rotated = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var newSecret = rotated.GetProperty("secret").GetString()!;
        Assert.StartsWith("whsec_", newSecret, StringComparison.Ordinal);
        Assert.NotEqual(oldSecret, newSecret);
        Assert.Equal("X-Signature", rotated.GetProperty("signing").GetProperty("header").GetString());

        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);

        var received = Assert.Single(fixture.Receiver.To(host));
        var timestamp = long.Parse(received.Headers["X-Webhook-Timestamp"], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(WebhookSignature.Verify(newSecret, timestamp, received.Body, received.Headers["X-Signature"]));
        Assert.False(WebhookSignature.Verify(oldSecret, timestamp, received.Body, received.Headers["X-Signature"]));

        // Nowhere else: not the list, not the audit row.
        var list = await integrator.GetStringAsync("/staff/webhooks", Token);
        Assert.DoesNotContain(newSecret, list, StringComparison.Ordinal);
        var audit = Assert.Single(await AuditsAsync(AuditActions.WebhookSecretRotated, id));
        Assert.DoesNotContain(newSecret, audit.DetailsJson ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_delivery_is_sent_again_once_with_the_same_event()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        var id = Id(webhook);
        using var integrator = fixture.Integrator(clinic);
        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);

        fixture.Receiver.Answer(host, () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);

        // Off and on again: the waiting delivery is now failed, ready to be sent by hand.
        await integrator.PatchAsJsonAsync($"/staff/webhooks/{id}", new { active = false }, Token);
        await integrator.PatchAsJsonAsync($"/staff/webhooks/{id}", new { active = true }, Token);
        fixture.Receiver.Answer(host, () => new HttpResponseMessage(HttpStatusCode.OK));
        var delivery = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, id, Token));

        var response = await integrator.PostAsync($"/staff/webhooks/{id}/deliveries/{delivery.Id}/redeliver", null, Token);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("pending", (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("status").GetString());

        var again = await integrator.PostAsync($"/staff/webhooks/{id}/deliveries/{delivery.Id}/redeliver", null, Token);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("state_conflict", await IntegrationsScenario.ErrorCodeAsync(again, Token));

        await fixture.DrainAsync(Token);
        await fixture.DrainAsync(Token);

        var attempts = fixture.Receiver.To(host);
        Assert.Equal(2, attempts.Count);
        Assert.Equal(attempts[0].Headers["X-Webhook-Id"], attempts[1].Headers["X-Webhook-Id"]);
        Assert.Equal(attempts[0].Body, attempts[1].Body);

        var done = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, id, Token));
        Assert.Equal(WebhookDeliveryStatus.Succeeded, done.Status);
        Assert.Single(await AuditsAsync(AuditActions.WebhookRedelivered, id));

        // Succeeded is not failed: nothing to send again.
        Assert.Equal(HttpStatusCode.Conflict, (await integrator.PostAsync($"/staff/webhooks/{id}/deliveries/{delivery.Id}/redeliver", null, Token)).StatusCode);
    }

    [Fact]
    public async Task A_delivery_that_used_up_its_retries_gets_one_more_attempt()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        var id = Id(webhook);
        fixture.Receiver.Answer(host, () => new HttpResponseMessage(HttpStatusCode.BadGateway));
        using var integrator = fixture.Integrator(clinic);
        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);

        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);
        var delivery = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, id, Token));

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            await db.Set<WebhookDelivery>().Where(d => d.Id == delivery.Id).ExecuteUpdateAsync(
                set => set
                    .SetProperty(d => d.Status, WebhookDeliveryStatus.Failed)
                    .SetProperty(d => d.Attempts, WebhookRetrySchedule.MaxAttempts)
                    .SetProperty(d => d.NextAttemptAt, (DateTimeOffset?)null),
                Token);
        }

        var response = await integrator.PostAsync($"/staff/webhooks/{id}/deliveries/{delivery.Id}/redeliver", null, Token);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await fixture.DrainAsync(Token);
        await fixture.DrainAsync(Token);

        var after = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, id, Token));
        Assert.Equal(WebhookDeliveryStatus.Failed, after.Status);
        Assert.Equal(WebhookRetrySchedule.MaxAttempts + 1, after.Attempts);
        Assert.Null(after.NextAttemptAt);
        Assert.Equal(2, fixture.Receiver.To(host).Count);
    }

    [Fact]
    public async Task Redelivery_needs_the_webhooks_own_failed_delivery_and_the_webhook_switched_on()
    {
        var (clinic, host, webhook) = await SubscribeAsync(["customer.created"]);
        var (_, _, other) = await SubscribeAsync(["customer.created"]);
        var id = Id(webhook);
        using var integrator = fixture.Integrator(clinic);
        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);

        fixture.Receiver.Answer(host, () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        await fixture.DrainAsync(Token);
        await integrator.PatchAsJsonAsync($"/staff/webhooks/{id}", new { active = false }, Token);
        var delivery = Assert.Single(await IntegrationsScenario.DeliveriesAsync(fixture, id, Token));

        var whileOff = await integrator.PostAsync($"/staff/webhooks/{id}/deliveries/{delivery.Id}/redeliver", null, Token);
        Assert.Equal(HttpStatusCode.Conflict, whileOff.StatusCode);
        Assert.Equal("state_conflict", await IntegrationsScenario.ErrorCodeAsync(whileOff, Token));

        Assert.Equal(HttpStatusCode.NotFound, (await integrator.PostAsync($"/staff/webhooks/{Id(other)}/deliveries/{delivery.Id}/redeliver", null, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await integrator.PostAsync($"/staff/webhooks/{id}/deliveries/whd_nothing/redeliver", null, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await integrator.PostAsync($"/staff/webhooks/whk_nothing/deliveries/{delivery.Id}/redeliver", null, Token)).StatusCode);
    }

    private static string Id(JsonElement webhook) => webhook.GetProperty("id").GetString()!;

    private static JsonElement Row(JsonElement list, string id) =>
        list.GetProperty("data").EnumerateArray().Single(row => row.GetProperty("id").GetString() == id);

    private async Task<IReadOnlyList<AuditEntry>> AuditsAsync(string action, string resourceId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.Action == action && entry.ResourceId == resourceId)
            .ToListAsync(Token);
    }

    private async Task<(SeededClinic Clinic, string Host, JsonElement Webhook)> SubscribeAsync(IReadOnlyList<string> events)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var host = IntegrationsScenario.Host();

        using var integrator = fixture.Integrator(clinic);
        var webhook = await IntegrationsScenario.CreateWebhookAsync(integrator, host, events, Token);

        return (clinic, host, webhook);
    }
}
