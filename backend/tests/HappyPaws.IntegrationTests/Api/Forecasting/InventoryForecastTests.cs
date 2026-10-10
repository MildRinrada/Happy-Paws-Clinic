using System.Net;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Inventory;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Forecasting;

/// <summary>
/// <c>GET /staff/inventory/forecast</c> (<c>docs/contract-extensions.md § 33</c>): averages from the
/// branch's own ledger, days until out, the suggested order, confidence, and who may ask.
/// </summary>
[Collection(nameof(ApiCollection))]
public class InventoryForecastTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Use_is_averaged_from_issues_only_and_the_order_covers_lead_time_and_cover_days()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        // 200 in 100 days ago; 30 issued 60 days ago, 60 issued 10 days ago, a count 5 days ago that
        // found 10 fewer, and 5 issued today. Only the two past issues are use: 60 in 30 days, 90 in 90.
        var steady = await ForecastScenario.StockHistoryAsync(
            fixture,
            clinic,
            UniqueName("Amoxicillin 250mg"),
            received: 200,
            receivedDaysAgo: 100,
            [(60, StockMovementKind.Issue, 30), (10, StockMovementKind.Issue, 60), (5, StockMovementKind.Adjustment, 100), (0, StockMovementKind.Issue, 5)],
            Token);

        // Stocked five days ago, 10 used since: two a day over the five days it has existed.
        var fresh = await ForecastScenario.StockHistoryAsync(
            fixture, clinic, UniqueName("Meloxicam 1mg"), received: 20, receivedDaysAgo: 5, [(3, StockMovementKind.Issue, 10)], Token);

        var idle = await ForecastScenario.StockHistoryAsync(
            fixture, clinic, UniqueName("Gauze"), received: 50, receivedDaysAgo: 200, [], Token);

        using var client = fixture.Storekeeper(clinic, Scopes.Inventory.Read);
        var forecast = await OkAsync(await client.GetAsync($"/staff/inventory/forecast?branch={clinic.BranchId}&cover_days=60", Token), Token);

        Assert.Equal(clinic.BranchId, forecast.GetProperty("branch_id").GetString());
        Assert.Equal(60, forecast.GetProperty("cover_days").GetInt32());
        Assert.Equal(7, forecast.GetProperty("lead_time_days").GetInt32());

        var rows = forecast.GetProperty("data").EnumerateArray().ToList();
        var a = rows.Single(row => row.GetProperty("item_id").GetString() == steady);
        var b = rows.Single(row => row.GetProperty("item_id").GetString() == fresh);
        var c = rows.Single(row => row.GetProperty("item_id").GetString() == idle);

        Assert.Equal(95, a.GetProperty("available").GetInt32());
        Assert.Equal(60, a.GetProperty("used_30d").GetInt32());
        Assert.Equal(90, a.GetProperty("used_90d").GetInt32());
        Assert.Equal(2.0, a.GetProperty("avg_daily_30d").GetDouble());
        Assert.Equal(1.0, a.GetProperty("avg_daily_90d").GetDouble());
        Assert.Equal(2.0, a.GetProperty("daily_rate").GetDouble());
        Assert.Equal(47, a.GetProperty("days_until_out").GetInt32());
        Assert.Equal(ForecastScenario.Today.AddDays(47).ToString("yyyy-MM-dd"), a.GetProperty("runs_out_on").GetString());

        // 2 a day for 7 + 60 days is 134, less the 95 on the shelf.
        Assert.Equal(39, a.GetProperty("suggested_order_qty").GetInt32());
        Assert.Equal(100, a.GetProperty("history_days").GetInt32());
        Assert.Equal("high", a.GetProperty("confidence").GetString());

        Assert.Equal(2.0, b.GetProperty("avg_daily_30d").GetDouble());
        Assert.Equal(5, b.GetProperty("days_until_out").GetInt32());
        Assert.Equal("low", b.GetProperty("confidence").GetString());

        Assert.False(c.TryGetProperty("days_until_out", out _));
        Assert.Equal(0, c.GetProperty("suggested_order_qty").GetInt32());
        Assert.Equal("high", c.GetProperty("confidence").GetString());

        // Soonest out first; nothing-used last.
        Assert.True(rows.IndexOf(b) < rows.IndexOf(a));
        Assert.True(rows.IndexOf(a) < rows.IndexOf(c));
    }

    [Fact]
    public async Task A_branch_bound_caller_gets_their_own_branch_without_naming_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic, Scopes.Inventory.Read);

        var forecast = await OkAsync(await client.GetAsync("/staff/inventory/forecast", Token), Token);

        Assert.Equal(clinic.BranchId, forecast.GetProperty("branch_id").GetString());
        Assert.Equal(30, forecast.GetProperty("cover_days").GetInt32());
    }

    [Theory]
    [InlineData("cover_days=0")]
    [InlineData("cover_days=181")]
    public async Task Cover_days_outside_one_to_180_are_refused(string query)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic, Scopes.Inventory.Read);

        var response = await client.GetAsync($"/staff/inventory/forecast?branch={clinic.BranchId}&{query}", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("validation_failed", await ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task An_unknown_branch_is_refused_and_another_branch_is_forbidden()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(mine, Scopes.Inventory.Read);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/staff/inventory/forecast?branch=brn_nowhere", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/staff/inventory/forecast?branch={theirs.BranchId}", Token)).StatusCode);
    }

    [Fact]
    public async Task It_needs_a_session_and_inventory_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/inventory/forecast", Token)).StatusCode);

        using var other = fixture.Storekeeper(clinic, Scopes.Inventory.Write, Scopes.Inventory.Purchase);
        var response = await other.GetAsync($"/staff/inventory/forecast?branch={clinic.BranchId}", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("insufficient_scope", await ErrorCodeAsync(response, Token));
    }

    internal static JsonElement Row(JsonElement forecast, string itemId) =>
        forecast.GetProperty("data").EnumerateArray().Single(row => row.GetProperty("item_id").GetString() == itemId);
}
