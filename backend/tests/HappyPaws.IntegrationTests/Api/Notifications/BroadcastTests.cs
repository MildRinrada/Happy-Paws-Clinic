using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Notifications;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Notifications;

/// <summary>
/// <c>POST /staff/integrations/line/broadcast</c>: marketing consent decides who receives it, LINE
/// only with no SMS fallback, the monthly quota, and the audit row.
/// </summary>
[Collection(nameof(DeliveryCollection))]
public class BroadcastTests(DeliveryFixture fixture) : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        fixture.Transports.Reset();
        fixture.Line.Status = HttpStatusCode.OK;
        await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private Task<HttpResponseMessage> BroadcastAsync(params string[] customerIds) =>
        fixture.Notifier(Scopes.Notify.Broadcast).PostAsJsonAsync(
            "/staff/integrations/line/broadcast",
            new { customer_ids = customerIds, message = "คลินิกหยุดวันจันทร์ที่ 13 ต.ค." },
            Token);

    [Fact]
    public async Task Only_customers_who_agreed_to_marketing_and_have_LINE_receive_it()
    {
        var agreed = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        await NotificationScenario.ConsentAsync(fixture, agreed.CustomerId, ConsentPurpose.Marketing, true, Token);

        var notAsked = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);

        var noLine = await NotificationScenario.CustomerAsync(fixture, line: false, cancellationToken: Token);
        await NotificationScenario.ConsentAsync(fixture, noLine.CustomerId, ConsentPurpose.Marketing, true, Token);

        var response = await BroadcastAsync(agreed.CustomerId, notAsked.CustomerId, noLine.CustomerId);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(1, body.GetProperty("sent").GetInt32());
        Assert.Equal(2, body.GetProperty("skipped").GetInt32());

        var results = body.GetProperty("results").EnumerateArray().ToDictionary(
            result => result.GetProperty("customer_id").GetString()!,
            result => result.TryGetProperty("error_code", out var code) ? code.GetString() : null);
        Assert.Null(results[agreed.CustomerId]);
        Assert.Equal("no_consent", results[notAsked.CustomerId]);
        Assert.Equal("no_address", results[noLine.CustomerId]);

        Assert.Equal("คลินิกหยุดวันจันทร์ที่ 13 ต.ค.", Assert.Single(fixture.Line.PushesTo(agreed.LineUserId!)).Text);
        Assert.Empty(fixture.Line.PushesTo(notAsked.LineUserId!));

        // An announcement is not worth an SMS credit: no fallback for the one without LINE.
        Assert.Empty(fixture.Transports.SmsTo(noLine.PhoneDigits));

        var quota = body.GetProperty("quota");
        Assert.Equal(300, quota.GetProperty("limit").GetInt32());
        Assert.Equal(quota.GetProperty("limit").GetInt32() - quota.GetProperty("used").GetInt32(), quota.GetProperty("remaining").GetInt32());

        var row = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, agreed.CustomerId, Token));
        Assert.Equal((DeliverySource.Broadcast, "clinic.announcement"), (row.Source, row.TemplateKey));

        using var scope = fixture.CreateScope();
        var audit = await scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>().Set<AuditEntry>().AsNoTracking()
            .SingleAsync(entry => entry.Action == AuditActions.NotificationBroadcast && entry.ResourceId == body.GetProperty("batch_id").GetString(), Token);
        Assert.Contains("\"sent\":1", audit.DetailsJson!.Replace(" ", string.Empty));
    }

    [Fact]
    public async Task A_broadcast_that_would_pass_the_months_quota_is_refused_whole()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        await NotificationScenario.ConsentAsync(fixture, customer.CustomerId, ConsentPurpose.Marketing, true, Token);
        await NotificationScenario.ConfigureLineAsync(fixture, new { broadcast_monthly_limit = 0 }, Token);

        var response = await BroadcastAsync(customer.CustomerId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("quota_exceeded", error.GetProperty("code").GetString());
        Assert.Equal(0, error.GetProperty("details").GetProperty("limit").GetInt32());
        Assert.Empty(fixture.Line.PushesTo(customer.LineUserId!));
    }

    [Fact]
    public async Task A_broadcast_with_LINE_switched_off_is_refused()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        await NotificationScenario.ConfigureLineAsync(fixture, new { enabled = false }, Token);

        var response = await BroadcastAsync(customer.CustomerId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "line_not_configured",
            (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_broadcast_to_a_customer_who_does_not_exist_is_404()
    {
        var response = await BroadcastAsync("cus_nobody");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
