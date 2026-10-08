using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Inventory;

/// <summary>"ปรับยอดหลังนับสต๊อก · ต้องระบุเหตุผล" — and the alerts that tell someone to count.</summary>
[Collection(nameof(ApiCollection))]
public class StockCountTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> CountAsync(HttpClient client, object body) =>
        client.PostAsJsonAsync("/staff/inventory/adjustments", body, Token);

    [Fact]
    public async Task A_count_sets_the_lot_to_what_was_counted_records_the_difference_and_is_audited()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var itemId = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Famotidine 10mg"), 50, Token));

        var response = await CountAsync(client, new { item_id = itemId, counted_quantity = 47, reason = "นับสิ้นเดือน พบเม็ดแตก" });
        var change = await OkAsync(response, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var row = Assert.Single(change.GetProperty("movements").EnumerateArray());
        Assert.Equal("adjustment", row.GetProperty("type").GetString());
        Assert.Equal(-3, row.GetProperty("quantity").GetInt32());
        Assert.Equal("นับสิ้นเดือน พบเม็ดแตก", row.GetProperty("note").GetString());
        Assert.Equal(47, Available(change.GetProperty("item"), clinic.BranchId));

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var entry = await db.Set<AuditEntry>().AsNoTracking()
            .SingleAsync(audit => audit.Action == AuditActions.InventoryAdjusted && audit.BranchId == clinic.BranchId, Token);
        var details = JsonDocument.Parse(entry.DetailsJson!).RootElement;
        Assert.Equal(-3, details.GetProperty("difference").GetInt32());
        Assert.Equal(50, details.GetProperty("before").GetInt32());

        // The same count again changes nothing, and says so with a row of zero.
        var again = await OkAsync(await CountAsync(client, new { item_id = itemId, counted_quantity = 47, reason = "นับซ้ำ" }), Token);
        Assert.Equal(0, again.GetProperty("movements")[0].GetProperty("quantity").GetInt32());
    }

    [Theory]
    [InlineData(null, 5, "reason")]
    [InlineData("  ", 5, "reason")]
    [InlineData("นับ", -1, "counted_quantity")]
    [InlineData("นับ", null, "counted_quantity")]
    public async Task A_count_without_a_reason_or_a_sane_quantity_is_refused(string? reason, int? counted, string field)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);

        var response = await CountAsync(client, new { item_id = "itm_any", counted_quantity = counted, reason });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains($"\"{field}\"", await response.Content.ReadAsStringAsync(Token));
    }

    [Fact]
    public async Task With_several_lots_on_the_shelf_the_count_must_say_which()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var itemId = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Cetirizine 10mg"), 10, Token, lotNumber: "X1", expiresOn: Day(90)));
        var second = await ReceiveAsync(client, clinic.BranchId, itemId, 10, Token, lotNumber: "X2", expiresOn: Day(180));
        var lotId = second.GetProperty("movements")[0].GetProperty("lot_id").GetString();

        var unnamed = await CountAsync(client, new { item_id = itemId, counted_quantity = 9, reason = "นับ" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unnamed.StatusCode);

        var named = await OkAsync(await CountAsync(client, new { item_id = itemId, lot_id = lotId, counted_quantity = 9, reason = "นับ" }), Token);
        Assert.Equal(19, Available(named.GetProperty("item"), clinic.BranchId));
    }

    [Fact]
    public async Task Counting_an_unknown_item_is_not_found()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);

        var response = await CountAsync(client, new { item_id = "itm_missing", counted_quantity = 1, reason = "นับ" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Alerts_list_expired_lots_running_out_near_expiry_and_low_stock_worst_first()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);

        var low = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Low Omeprazole"), 3, Token, reorderLevel: 5));
        var soon = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Soon Insulin"), 40, Token, lotNumber: "S1", expiresOn: Day(10)));
        var gone = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Gone Atropine"), 2, Token));
        await OkAsync(await MoveAsync(client, new { type = "issue", item_id = gone, quantity = 2 }, Token), Token);
        var staleName = UniqueName("Stale Vitamin");
        var stale = ItemId(await ReceiveNewAsync(client, clinic, staleName, 20, Token));
        await SeedLotAsync(fixture, stale, clinic.BranchId, 6, AnalyticsScenario.Today.AddDays(-3), "OLD", Token);
        var fine = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Fine Saline"), 100, Token, reorderLevel: 10, expiresOn: Day(300)));

        var alerts = await client.GetFromJsonAsync<JsonElement>($"/staff/inventory/alerts?branch={clinic.BranchId}&within_days=30", Token);
        var rows = alerts.GetProperty("data").EnumerateArray()
            .Select(row => $"{row.GetProperty("kind").GetString()}:{row.GetProperty("item_id").GetString()}")
            .ToList();

        Assert.Equal([$"expired:{stale}", $"out_of_stock:{gone}", $"near_expiry:{soon}", $"low_stock:{low}"], rows);

        var expired = alerts.GetProperty("data")[0];
        Assert.Equal("OLD", expired.GetProperty("lot_number").GetString());
        Assert.Equal(6, expired.GetProperty("lot_quantity").GetInt32());
        Assert.Equal(-3, expired.GetProperty("days_to_expiry").GetInt32());

        // Expired units are on the books but not usable.
        var listed = BranchOf(await ItemAsync(client, clinic.BranchId, staleName, Token), clinic.BranchId);
        Assert.Equal(26, listed.GetProperty("on_hand").GetInt32());
        Assert.Equal(20, listed.GetProperty("available").GetInt32());

        var status = await client.GetFromJsonAsync<JsonElement>($"/staff/inventory/items?branch={clinic.BranchId}&status=low", Token);
        Assert.Contains(status.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetString() == low);
        Assert.DoesNotContain(status.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetString() == fine);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/staff/inventory/alerts?within_days=0", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync("/staff/inventory/items?status=plenty", Token)).StatusCode);
    }
}
