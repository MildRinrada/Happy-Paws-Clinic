using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Inventory;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Inventory;

/// <summary>"รับเข้า จ่ายออก โอนสาขา" — and the ledger staying equal to the shelf.</summary>
[Collection(nameof(ApiCollection))]
public class StockMovementTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Receiving_a_new_item_creates_it_and_puts_the_lot_on_the_shelf()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var name = UniqueName("Amoxicillin 250mg");

        var change = await ReceiveNewAsync(client, clinic, name, 100, Token, priceSatang: 500, reorderLevel: 20, lotNumber: "A1", expiresOn: Day(300));

        var item = change.GetProperty("item");
        Assert.Equal(name, item.GetProperty("name").GetString());
        Assert.Equal("medicine", item.GetProperty("kind").GetString());
        Assert.Equal(500, item.GetProperty("unit_price_satang").GetInt64());

        var movement = Assert.Single(change.GetProperty("movements").EnumerateArray());
        Assert.Equal("receive", movement.GetProperty("type").GetString());
        Assert.Equal(100, movement.GetProperty("quantity").GetInt32());
        Assert.Equal(100, movement.GetProperty("balance_after").GetInt32());
        Assert.Equal("A1", movement.GetProperty("lot_number").GetString());

        var listed = await ItemAsync(client, clinic.BranchId, name, Token);
        var branch = BranchOf(listed, clinic.BranchId);
        Assert.Equal(100, branch.GetProperty("on_hand").GetInt32());
        Assert.Equal("ok", branch.GetProperty("status").GetString());
        Assert.Equal(Day(300), branch.GetProperty("next_expiry").GetString());
    }

    [Fact]
    public async Task A_new_item_with_a_name_already_in_the_catalogue_is_refused_with_the_existing_id()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var name = UniqueName("Furosemide 40mg");
        var itemId = ItemId(await ReceiveNewAsync(client, clinic, name, 10, Token));

        // Spacing and case do not make a different drug.
        var again = await MoveAsync(client, new
        {
            type = "receive",
            new_item = new { name = $"  {name.ToUpperInvariant()} ", unit = "เม็ด", unit_price_satang = 100 },
            quantity = 5,
        }, Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        var error = (await again.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("inventory_item_exists", error.GetProperty("code").GetString());
        Assert.Equal(itemId, error.GetProperty("details").GetProperty("item_id").GetString());
    }

    [Theory]
    [InlineData("""{ "type": "lend", "quantity": 1, "item_id": "itm_x" }""", "type")]
    [InlineData("""{ "type": "receive", "item_id": "itm_x" }""", "quantity")]
    [InlineData("""{ "type": "receive", "quantity": 1 }""", "item_id")]
    [InlineData("""{ "type": "receive", "quantity": 0, "item_id": "itm_x" }""", "quantity")]
    [InlineData("""{ "type": "issue", "quantity": 1, "item_id": "itm_x", "lot_number": "A" }""", "lot_number")]
    [InlineData("""{ "type": "transfer", "quantity": 1, "item_id": "itm_x" }""", "to_branch_id")]
    [InlineData("""{ "type": "receive", "quantity": 1, "new_item": { "name": "ยา", "unit": "เม็ด" } }""", "new_item.unit_price_satang")]
    public async Task A_malformed_movement_is_refused_naming_the_field(string json, string field)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);

        using var request = BookingScenario.Idempotent("/staff/inventory/movements");
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(Token);
        Assert.Contains($"\"{field}\"", body);
    }

    [Fact]
    public async Task An_already_expired_lot_cannot_be_received()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var itemId = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Enrofloxacin 50mg"), 5, Token));

        var response = await MoveAsync(client, new
        {
            type = "receive", item_id = itemId, quantity = 5, lot_number = "OLD", expires_on = Day(-1),
        }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_issue_takes_the_soonest_expiry_first_and_refuses_more_than_is_usable()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var name = UniqueName("Metronidazole 200mg");

        var itemId = ItemId(await ReceiveNewAsync(client, clinic, name, 10, Token, lotNumber: "LATE", expiresOn: Day(200)));
        await ReceiveAsync(client, clinic.BranchId, itemId, 4, Token, lotNumber: "SOON", expiresOn: Day(20));

        var issued = await OkAsync(await MoveAsync(client, new { type = "issue", item_id = itemId, quantity = 6, note = "ใช้ในห้องผ่าตัด" }, Token), Token);

        var rows = issued.GetProperty("movements").EnumerateArray().ToList();
        Assert.Equal(["SOON", "LATE"], rows.Select(row => row.GetProperty("lot_number").GetString()));
        Assert.Equal([-4, -2], rows.Select(row => row.GetProperty("quantity").GetInt32()));
        Assert.Equal(8, Available(issued.GetProperty("item"), clinic.BranchId));

        var tooMany = await MoveAsync(client, new { type = "issue", item_id = itemId, quantity = 9 }, Token);
        Assert.Equal(HttpStatusCode.Conflict, tooMany.StatusCode);
        var error = (await tooMany.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("insufficient_stock", error.GetProperty("code").GetString());
        Assert.Equal(8, error.GetProperty("details").GetProperty("available").GetInt32());

        await AssertLedgerMatchesShelfAsync(itemId, clinic.BranchId);
    }

    [Fact]
    public async Task A_transfer_moves_the_lot_to_the_other_branch_and_keeps_its_number_and_expiry()
    {
        var from = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var to = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        // A member of staff who works at both branches.
        using var client = fixture.Authenticated(TestTokens.Staff(
            fixture, from.StaffUserId, from.StaffId, InventoryScopes, [from.BranchId, to.BranchId]));

        var name = UniqueName("Carprofen 50mg");
        var itemId = ItemId(await ReceiveNewAsync(client, from, name, 30, Token, lotNumber: "C9", expiresOn: Day(150)));

        var moved = await OkAsync(await MoveAsync(client, new
        {
            type = "transfer", branch_id = from.BranchId, to_branch_id = to.BranchId, item_id = itemId, quantity = 12, reference = "TR-001",
        }, Token), Token);

        var rows = moved.GetProperty("movements").EnumerateArray().ToList();
        Assert.Equal(["transfer_out", "transfer_in"], rows.Select(row => row.GetProperty("type").GetString()));
        Assert.Equal(rows[0].GetProperty("transfer_id").GetString(), rows[1].GetProperty("transfer_id").GetString());
        Assert.All(rows, row => Assert.Equal("C9", row.GetProperty("lot_number").GetString()));

        Assert.Equal(18, Available(moved.GetProperty("item"), from.BranchId));
        Assert.Equal(12, Available(moved.GetProperty("item"), to.BranchId));

        await AssertLedgerMatchesShelfAsync(itemId, from.BranchId);
        await AssertLedgerMatchesShelfAsync(itemId, to.BranchId);
    }

    [Fact]
    public async Task A_transfer_to_the_same_branch_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var itemId = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Gabapentin 100mg"), 5, Token));

        var response = await MoveAsync(client, new { type = "transfer", to_branch_id = clinic.BranchId, item_id = itemId, quantity = 1 }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_issues_of_the_last_units_never_take_the_shelf_below_zero()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Storekeeper(clinic);
        var itemId = ItemId(await ReceiveNewAsync(client, clinic, UniqueName("Maropitant 16mg"), 5, Token));

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            MoveAsync(client, new { type = "issue", item_id = itemId, quantity = 1 }, Token)));

        Assert.Equal(5, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

        var (lots, _) = await AssertLedgerMatchesShelfAsync(itemId, clinic.BranchId);
        Assert.Equal(0, lots.Sum(lot => lot.QuantityOnHand));
    }

    private async Task<(IReadOnlyList<StockLot> Lots, IReadOnlyList<StockMovement> Movements)> AssertLedgerMatchesShelfAsync(string itemId, string branchId)
    {
        var (lots, movements) = await LedgerAsync(fixture, itemId, branchId, Token);

        foreach (var lot in lots)
        {
            Assert.Equal(lot.QuantityOnHand, movements.Where(movement => movement.LotId == lot.Id).Sum(movement => movement.Quantity));
            Assert.True(lot.QuantityOnHand >= 0);
        }

        return (lots, movements);
    }
}
