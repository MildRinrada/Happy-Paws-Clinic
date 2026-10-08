using System.Net;
using System.Net.Http.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Inventory;

/// <summary>
/// Who may touch stock: each endpoint's own scope, and only the branches the caller works at.
/// </summary>
[Collection(nameof(ApiCollection))]
public class InventoryAccessTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public static TheoryData<string, string, string> Endpoints => new()
    {
        { "GET", "/staff/inventory/items", Scopes.Inventory.Read },
        { "GET", "/staff/inventory/alerts", Scopes.Inventory.Read },
        { "POST", "/staff/inventory/movements", Scopes.Inventory.Write },
        { "POST", "/staff/inventory/adjustments", Scopes.Inventory.Adjust },
        { "POST", "/staff/inventory/purchase-orders", Scopes.Inventory.Purchase },
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Each_endpoint_refuses_a_caller_without_its_scope_and_names_it(string method, string path, string scope)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        // Every inventory scope except the one this endpoint needs.
        using var client = fixture.Storekeeper(clinic, [.. InventoryScopes.Where(held => held != scope)]);

        using var request = method == "GET" ? new HttpRequestMessage(HttpMethod.Get, path) : Idempotent(path, new { });
        var response = await client.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(Token);
        Assert.Equal("insufficient_scope", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains(scope, body.GetProperty("error").GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public async Task Another_branchs_stock_can_be_neither_read_nor_written()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var me = fixture.Storekeeper(mine);
        using var them = fixture.Storekeeper(theirs);

        var name = UniqueName("Doxycycline 100mg");
        var itemId = ItemId(await ReceiveNewAsync(them, theirs, name, 30, Token));

        Assert.Equal(HttpStatusCode.Forbidden, (await me.GetAsync($"/staff/inventory/items?branch={theirs.BranchId}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await me.GetAsync($"/staff/inventory/alerts?branch={theirs.BranchId}", Token)).StatusCode);

        var issue = await MoveAsync(me, new { type = "issue", branch_id = theirs.BranchId, item_id = itemId, quantity = 1 }, Token);
        Assert.Equal(HttpStatusCode.Forbidden, issue.StatusCode);

        var count = await me.PostAsJsonAsync(
            "/staff/inventory/adjustments",
            new { branch_id = theirs.BranchId, item_id = itemId, counted_quantity = 0, reason = "ลอง" },
            Token);
        Assert.Equal(HttpStatusCode.Forbidden, count.StatusCode);

        using var order = Idempotent("/staff/inventory/purchase-orders", new
        {
            branch_id = theirs.BranchId,
            supplier_name = "ซัพพลายเออร์",
            lines = new[] { new { item_id = itemId, quantity = 1, unit_cost_satang = 100 } },
        });
        Assert.Equal(HttpStatusCode.Forbidden, (await me.SendAsync(order, Token)).StatusCode);

        // The catalogue is the clinic's, so the item is listed to me, but only with my own branch's
        // stock beside it — none — and nothing of theirs.
        var seen = await ItemAsync(me, mine.BranchId, name, Token);
        var branch = Assert.Single(seen.GetProperty("branches").EnumerateArray());
        Assert.Equal(mine.BranchId, branch.GetProperty("branch_id").GetString());
        Assert.Equal(0, branch.GetProperty("on_hand").GetInt32());
        Assert.Equal(30, Available(await ItemAsync(them, theirs.BranchId, name, Token), theirs.BranchId));
    }

    [Fact]
    public async Task A_caller_bound_to_one_branch_writes_there_without_naming_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);

        var response = await MoveAsync(client, new
        {
            type = "receive",
            new_item = new { name = UniqueName("Prednisolone 5mg"), unit = "เม็ด", unit_price_satang = 300 },
            quantity = 12,
        }, Token);

        var change = await OkAsync(response, Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(clinic.BranchId, change.GetProperty("movements")[0].GetProperty("branch_id").GetString());
    }

    [Fact]
    public async Task Movements_and_purchase_orders_need_an_idempotency_key()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);

        var movement = await client.PostAsJsonAsync("/staff/inventory/movements", new { type = "receive" }, Token);
        var order = await client.PostAsJsonAsync("/staff/inventory/purchase-orders", new { supplier_name = "x" }, Token);

        Assert.Equal(HttpStatusCode.BadRequest, movement.StatusCode);
        Assert.Equal("idempotency_key_required", await ErrorCodeAsync(movement, Token));
        Assert.Equal(HttpStatusCode.BadRequest, order.StatusCode);
    }

    [Fact]
    public async Task A_retried_receipt_with_the_same_key_puts_the_goods_on_the_shelf_once()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var name = UniqueName("Cefalexin 500mg");
        var body = new
        {
            type = "receive",
            new_item = new { name, unit = "แคปซูล", unit_price_satang = 1_200 },
            quantity = 40,
        };
        var key = Guid.NewGuid().ToString();

        using (var first = Idempotent("/staff/inventory/movements", body, key))
        {
            await OkAsync(await client.SendAsync(first, Token), Token);
        }

        using (var again = Idempotent("/staff/inventory/movements", body, key))
        {
            await OkAsync(await client.SendAsync(again, Token), Token);
        }

        Assert.Equal(40, Available(await ItemAsync(client, clinic.BranchId, name, Token), clinic.BranchId));
    }
}
