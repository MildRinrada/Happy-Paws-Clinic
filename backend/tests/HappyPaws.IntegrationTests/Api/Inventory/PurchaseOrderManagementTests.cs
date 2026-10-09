using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Inventory;

/// <summary>
/// <c>docs/contract-extensions.md § 25</c>: <c>GET /staff/inventory/purchase-orders</c> and
/// <c>POST /staff/inventory/purchase-orders/{id}/cancel</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class PurchaseOrderManagementTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> OrderAsync(HttpClient client, string itemId, int quantity = 10, string supplier = "บริษัท ยาสัตว์ จำกัด")
    {
        using var request = Idempotent("/staff/inventory/purchase-orders", new
        {
            supplier_name = supplier,
            lines = new object[] { new { item_id = itemId, quantity, unit_cost_satang = 300 } },
        });

        return await OkAsync(await client.SendAsync(request, Token), Token);
    }

    private static async Task<HttpResponseMessage> CancelAsync(HttpClient client, string orderId, string? reason = null) =>
        await client.PostAsJsonAsync($"/staff/inventory/purchase-orders/{orderId}/cancel", new { reason }, Token);

    private static async Task<List<JsonElement>> ListAsync(HttpClient client, string query = "") =>
        [.. (await OkAsync(await client.GetAsync($"/staff/inventory/purchase-orders{query}", Token), Token))
            .GetProperty("data").EnumerateArray()];

    private static string Id(JsonElement order) => order.GetProperty("id").GetString()!;

    [Fact]
    public async Task Reading_and_cancelling_orders_need_inventory_purchase()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);
        var itemId = ItemId(await ReceiveNewAsync(store, clinic, UniqueName("Cephalexin 500mg"), 1, Token));
        var order = await OrderAsync(store, itemId);
        using var reader = fixture.Storekeeper(clinic, Scopes.Inventory.Read, Scopes.Inventory.Write);

        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/staff/inventory/purchase-orders", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await CancelAsync(reader, Id(order))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await store.GetAsync("/staff/inventory/purchase-orders?status=open", Token)).StatusCode);
    }

    [Fact]
    public async Task Orders_are_listed_newest_first_with_their_lines_and_filtered_by_status_and_supplier()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);
        var name = UniqueName("Gabapentin 100mg");
        var itemId = ItemId(await ReceiveNewAsync(store, clinic, name, 1, Token));
        var first = await OrderAsync(store, itemId, supplier: "Alpha Vet Supply");
        var second = await OrderAsync(store, itemId, supplier: "Beta Pharma");
        await ReceiveAgainstAsync(store, clinic.BranchId, itemId, Id(second), 10);

        var all = await ListAsync(store, $"?branch={clinic.BranchId}");

        Assert.Equal([Id(second), Id(first)], all.Select(Id));
        Assert.Equal("received", all[0].GetProperty("status").GetString());
        Assert.Equal(name, all[0].GetProperty("lines")[0].GetProperty("item_name").GetString());
        Assert.Equal(3_000, all[0].GetProperty("received_cost_satang").GetInt64());

        Assert.Equal([Id(first)], (await ListAsync(store, "?status=ordered")).Select(Id));
        Assert.Equal([Id(second)], (await ListAsync(store, "?supplier=beta")).Select(Id));
    }

    [Fact]
    public async Task Branch_staff_see_and_cancel_only_their_branchs_orders()
    {
        var home = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var theirStore = fixture.Storekeeper(other);
        var theirItem = ItemId(await ReceiveNewAsync(theirStore, other, UniqueName("Tramadol 50mg"), 1, Token));
        var theirs = await OrderAsync(theirStore, theirItem);
        using var store = fixture.Storekeeper(home);

        Assert.DoesNotContain(Id(theirs), (await ListAsync(store, "?limit=100")).Select(Id));
        Assert.Equal(HttpStatusCode.Forbidden, (await store.GetAsync($"/staff/inventory/purchase-orders?branch={other.BranchId}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await CancelAsync(store, Id(theirs))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await CancelAsync(store, "po_does_not_exist")).StatusCode);
    }

    [Fact]
    public async Task Cancelling_an_order_nothing_arrived_for_takes_it_off_order_and_refuses_receipts()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);
        var name = UniqueName("Clindamycin 75mg");
        var itemId = ItemId(await ReceiveNewAsync(store, clinic, name, 1, Token));
        var order = await OrderAsync(store, itemId, quantity: 20);
        Assert.Equal(20, BranchOf(await ItemAsync(store, clinic.BranchId, name, Token), clinic.BranchId).GetProperty("on_order").GetInt32());

        var cancelled = await OkAsync(await CancelAsync(store, Id(order), "ซัพพลายเออร์แจ้งว่าของหมด"), Token);

        Assert.Equal("cancelled", cancelled.GetProperty("status").GetString());
        Assert.Equal("ซัพพลายเออร์แจ้งว่าของหมด", cancelled.GetProperty("cancel_reason").GetString());
        Assert.Equal(clinic.StaffId, cancelled.GetProperty("cancelled_by_staff_id").GetString());
        Assert.Equal(20, cancelled.GetProperty("lines")[0].GetProperty("cancelled_quantity").GetInt32());
        Assert.Equal(0, cancelled.GetProperty("lines")[0].GetProperty("outstanding").GetInt32());
        Assert.Equal(0, BranchOf(await ItemAsync(store, clinic.BranchId, name, Token), clinic.BranchId).GetProperty("on_order").GetInt32());

        var receipt = await MoveAsync(store, new
        {
            type = "receive", branch_id = clinic.BranchId, item_id = itemId, quantity = 5, purchase_order_id = Id(order),
        }, Token);
        Assert.Equal(HttpStatusCode.Conflict, receipt.StatusCode);
        Assert.Equal("state_conflict", await ErrorCodeAsync(receipt, Token));

        var again = await CancelAsync(store, Id(order));
        Assert.Equal("state_conflict", await ErrorCodeAsync(again, Token));

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.Equal(1, await db.Set<AuditEntry>().CountAsync(
            entry => entry.Action == AuditActions.PurchaseOrderCancelled && entry.ResourceId == Id(order), Token));
    }

    [Fact]
    public async Task Cancelling_a_partly_received_order_keeps_what_arrived_in_stock()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);
        var name = UniqueName("Famotidine 10mg");
        var itemId = ItemId(await ReceiveNewAsync(store, clinic, name, 1, Token));
        var order = await OrderAsync(store, itemId, quantity: 10);
        await ReceiveAgainstAsync(store, clinic.BranchId, itemId, Id(order), 4);

        var cancelled = await OkAsync(await CancelAsync(store, Id(order)), Token);

        var line = cancelled.GetProperty("lines")[0];
        Assert.Equal("cancelled", cancelled.GetProperty("status").GetString());
        Assert.Equal(4, line.GetProperty("received_quantity").GetInt32());
        Assert.Equal(6, line.GetProperty("cancelled_quantity").GetInt32());
        Assert.Equal(1_200, cancelled.GetProperty("received_cost_satang").GetInt64());
        Assert.Equal(5, Available(await ItemAsync(store, clinic.BranchId, name, Token), clinic.BranchId));
    }

    [Fact]
    public async Task An_order_that_fully_arrived_cannot_be_cancelled_and_a_stale_version_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);
        var itemId = ItemId(await ReceiveNewAsync(store, clinic, UniqueName("Omeprazole 20mg"), 1, Token));
        var received = await OrderAsync(store, itemId, quantity: 2);
        await ReceiveAgainstAsync(store, clinic.BranchId, itemId, Id(received), 2);

        var refused = await CancelAsync(store, Id(received));
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("state_conflict", await ErrorCodeAsync(refused, Token));

        var open = await OrderAsync(store, itemId, quantity: 2);
        using var stale = new HttpRequestMessage(HttpMethod.Post, $"/staff/inventory/purchase-orders/{Id(open)}/cancel");
        stale.Headers.TryAddWithoutValidation("If-Match", $"\"{open.GetProperty("version").GetUInt32() + 1}\"");
        var conflict = await store.SendAsync(stale, Token);

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("version_conflict", await ErrorCodeAsync(conflict, Token));
    }

    private static async Task ReceiveAgainstAsync(HttpClient client, string branchId, string itemId, string orderId, int quantity) =>
        await OkAsync(await MoveAsync(client, new
        {
            type = "receive", branch_id = branchId, item_id = itemId, quantity, purchase_order_id = orderId,
        }, Token), Token);
}
