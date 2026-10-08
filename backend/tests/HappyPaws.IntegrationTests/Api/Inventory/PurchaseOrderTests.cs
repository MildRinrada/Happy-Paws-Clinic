using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Inventory;

/// <summary>"สั่งซื้อจากซัพพลายเออร์ · รับของบางส่วนได้".</summary>
[Collection(nameof(ApiCollection))]
public class PurchaseOrderTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<HttpResponseMessage> OrderAsync(HttpClient client, object body)
    {
        using var request = Idempotent("/staff/inventory/purchase-orders", body);
        return await client.SendAsync(request, Token);
    }

    [Fact]
    public async Task An_order_is_numbered_creates_new_items_changes_no_stock_and_is_audited()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var existingName = UniqueName("Ivermectin 3mg");
        var existing = ItemId(await ReceiveNewAsync(client, clinic, existingName, 5, Token));
        var newName = UniqueName("Toltrazuril 50mg");

        var response = await OrderAsync(client, new
        {
            supplier_name = "บริษัท ยาสัตว์ จำกัด",
            expected_on = Day(7),
            lines = new object[]
            {
                new { item_id = existing, quantity = 100, unit_cost_satang = 250 },
                new { new_item = new { name = newName, unit = "ml", unit_price_satang = 1_500, reorder_level = 10 }, quantity = 20, unit_cost_satang = 900 },
            },
        });

        var order = await OkAsync(response, Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Matches(new Regex(@"^PO-\d{6}-\d{4}$"), order.GetProperty("number").GetString());
        Assert.Equal("ordered", order.GetProperty("status").GetString());
        Assert.Equal(clinic.BranchId, order.GetProperty("branch_id").GetString());
        Assert.Equal((100 * 250) + (20 * 900), order.GetProperty("total_cost_satang").GetInt64());

        var lines = order.GetProperty("lines").EnumerateArray().ToList();
        Assert.Equal(existingName, lines[0].GetProperty("item_name").GetString());
        Assert.Equal(newName, lines[1].GetProperty("item_name").GetString());
        Assert.Equal(20, lines[1].GetProperty("outstanding").GetInt32());

        // Ordered is not received: the shelf is unchanged, but the list shows it on order.
        var listed = BranchOf(await ItemAsync(client, clinic.BranchId, existingName, Token), clinic.BranchId);
        Assert.Equal(5, listed.GetProperty("available").GetInt32());
        Assert.Equal(100, listed.GetProperty("on_order").GetInt32());
        Assert.Equal(order.GetProperty("id").GetString(), listed.GetProperty("open_orders")[0].GetProperty("purchase_order_id").GetString());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var id = order.GetProperty("id").GetString();
        Assert.True(await db.Set<AuditEntry>().AnyAsync(entry => entry.Action == AuditActions.PurchaseOrderRaised && entry.ResourceId == id, Token));
        Assert.True(await db.Set<AuditEntry>().AnyAsync(entry => entry.Action == AuditActions.InventoryItemCreated && entry.ResourceId == lines[1].GetProperty("item_id").GetString(), Token));
    }

    [Fact]
    public async Task Goods_are_received_against_the_order_in_parts_and_never_beyond_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var name = UniqueName("Silver sulfadiazine cream");
        var itemId = ItemId(await ReceiveNewAsync(client, clinic, name, 1, Token));

        var order = await OkAsync(await OrderAsync(client, new
        {
            supplier_name = "ซัพพลายเออร์ A",
            lines = new[] { new { item_id = itemId, quantity = 10, unit_cost_satang = 4_000 } },
        }), Token);
        var orderId = order.GetProperty("id").GetString();

        var part = await OkAsync(await MoveAsync(client, new
        {
            type = "receive", item_id = itemId, quantity = 4, purchase_order_id = orderId, lot_number = "P1", expires_on = Day(400),
        }, Token), Token);

        Assert.Equal("partially_received", part.GetProperty("purchase_order").GetProperty("status").GetString());
        Assert.Equal(6, part.GetProperty("purchase_order").GetProperty("lines")[0].GetProperty("outstanding").GetInt32());

        // The order's cost and number go on the ledger row when the receipt does not say otherwise.
        var row = part.GetProperty("movements")[0];
        Assert.Equal(4_000, row.GetProperty("unit_cost_satang").GetInt64());
        Assert.Equal(order.GetProperty("number").GetString(), row.GetProperty("reference").GetString());

        var over = await MoveAsync(client, new { type = "receive", item_id = itemId, quantity = 7, purchase_order_id = orderId }, Token);
        Assert.Equal(HttpStatusCode.Conflict, over.StatusCode);
        Assert.Equal("over_receipt", await ErrorCodeAsync(over, Token));

        var rest = await OkAsync(await MoveAsync(client, new { type = "receive", item_id = itemId, quantity = 6, purchase_order_id = orderId }, Token), Token);
        Assert.Equal("received", rest.GetProperty("purchase_order").GetProperty("status").GetString());

        var listed = BranchOf(await ItemAsync(client, clinic.BranchId, name, Token), clinic.BranchId);
        Assert.Equal(11, listed.GetProperty("available").GetInt32());
        Assert.Equal(0, listed.GetProperty("on_order").GetInt32());

        // An item that is not on the order cannot be received against it.
        var other = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Not ordered"), 1, Token));
        var stray = await MoveAsync(client, new { type = "receive", item_id = other, quantity = 1, purchase_order_id = orderId }, Token);
        Assert.Equal(HttpStatusCode.Conflict, stray.StatusCode);
    }

    [Fact]
    public async Task Goods_cannot_land_in_another_branch_than_the_order_is_for()
    {
        var a = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var b = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, a.StaffUserId, a.StaffId, InventoryScopes, [a.BranchId, b.BranchId]));
        var itemId = ItemId(await ReceiveNewAsync(client, a, UniqueName("Branch bound"), 1, Token));

        var order = await OkAsync(await OrderAsync(client, new
        {
            branch_id = a.BranchId,
            supplier_name = "ซัพพลายเออร์",
            lines = new[] { new { item_id = itemId, quantity = 5, unit_cost_satang = 100 } },
        }), Token);

        var elsewhere = await MoveAsync(client, new
        {
            type = "receive", branch_id = b.BranchId, item_id = itemId, quantity = 5, purchase_order_id = order.GetProperty("id").GetString(),
        }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, elsewhere.StatusCode);
    }

    [Theory]
    [InlineData("""{ "lines": [ { "item_id": "itm_a", "quantity": 1, "unit_cost_satang": 1 } ] }""", "supplier_name")]
    [InlineData("""{ "supplier_name": "A", "lines": [] }""", "lines")]
    [InlineData("""{ "supplier_name": "A", "lines": [ { "item_id": "itm_a", "quantity": 1, "unit_cost_satang": 1 }, { "item_id": "itm_a", "quantity": 2, "unit_cost_satang": 1 } ] }""", "lines")]
    [InlineData("""{ "supplier_name": "A", "lines": [ { "item_id": "itm_a", "quantity": 0, "unit_cost_satang": 1 } ] }""", "lines[0].quantity")]
    [InlineData("""{ "supplier_name": "A", "lines": [ { "quantity": 1, "unit_cost_satang": 1 } ] }""", "lines[0].item_id")]
    public async Task A_malformed_order_is_refused_naming_the_field(string json, string field)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);

        using var request = Idempotent("/staff/inventory/purchase-orders");
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains($"\"{field}\"", await response.Content.ReadAsStringAsync(Token));
    }

    [Fact]
    public async Task Ordering_an_unknown_item_is_not_found()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);

        var response = await OrderAsync(client, new
        {
            supplier_name = "A",
            lines = new[] { new { item_id = "itm_missing", quantity = 1, unit_cost_satang = 1 } },
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("not_found", (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("code").GetString());
    }
}
