using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Inventory;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// The stock room: a client holding the inventory scopes, and the receipts and reads the inventory
/// and dispensing tests share.
/// <para>
/// The catalogue is clinic-wide and the suite shares one database, so every item a test creates has
/// a name of its own (<see cref="UniqueName"/>). A test that created "Amoxicillin" would match every
/// other test's prescription of it.
/// </para>
/// </summary>
public static class InventoryScenario
{
    public static readonly string[] InventoryScopes =
    [
        Scopes.Inventory.Read,
        Scopes.Inventory.Write,
        Scopes.Inventory.Adjust,
        Scopes.Inventory.Purchase,
    ];

    /// <summary>A member of staff bound to the clinic's branch, holding the inventory scopes unless told otherwise.</summary>
    public static HttpClient Storekeeper(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? InventoryScopes : scopes,
            [clinic.BranchId]));

    public static string UniqueName(string drug) => $"{drug} {Guid.NewGuid().ToString("N")[..10]}";

    public static string Day(int daysFromToday) => AnalyticsScenario.Today.AddDays(daysFromToday).ToString("yyyy-MM-dd");

    /// <summary>POSTs a movement with a fresh <c>Idempotency-Key</c> and hands back the raw response.</summary>
    public static async Task<HttpResponseMessage> MoveAsync(HttpClient client, object body, CancellationToken cancellationToken)
    {
        using var request = Idempotent("/staff/inventory/movements", body);
        return await client.SendAsync(request, cancellationToken);
    }

    /// <summary>Receives a new item into the clinic's branch and returns the answer. Fails the test if refused.</summary>
    public static async Task<JsonElement> ReceiveNewAsync(
        HttpClient client,
        SeededClinic clinic,
        string name,
        int quantity,
        CancellationToken cancellationToken,
        long priceSatang = 1_000,
        int reorderLevel = 0,
        string? lotNumber = null,
        string? expiresOn = null)
    {
        var response = await MoveAsync(client, new
        {
            type = "receive",
            branch_id = clinic.BranchId,
            new_item = new { name, unit = "เม็ด", unit_price_satang = priceSatang, reorder_level = reorderLevel },
            quantity,
            lot_number = lotNumber,
            expires_on = expiresOn,
            unit_cost_satang = priceSatang / 2,
        }, cancellationToken);

        return await OkAsync(response, cancellationToken);
    }

    /// <summary>Receives more of an existing item.</summary>
    public static async Task<JsonElement> ReceiveAsync(
        HttpClient client,
        string branchId,
        string itemId,
        int quantity,
        CancellationToken cancellationToken,
        string? lotNumber = null,
        string? expiresOn = null)
    {
        var response = await MoveAsync(client, new
        {
            type = "receive",
            branch_id = branchId,
            item_id = itemId,
            quantity,
            lot_number = lotNumber,
            expires_on = expiresOn,
        }, cancellationToken);

        return await OkAsync(response, cancellationToken);
    }

    public static string ItemId(JsonElement change) => change.GetProperty("item").GetProperty("id").GetString()!;

    /// <summary>The item as <c>GET /staff/inventory/items</c> shows it at one branch.</summary>
    public static async Task<JsonElement> ItemAsync(
        HttpClient client,
        string branchId,
        string name,
        CancellationToken cancellationToken)
    {
        var list = await client.GetFromJsonAsync<JsonElement>(
            $"/staff/inventory/items?branch={branchId}&q={Uri.EscapeDataString(name)}", cancellationToken);

        return list.GetProperty("data").EnumerateArray().Single(item => item.GetProperty("name").GetString() == name);
    }

    public static JsonElement BranchOf(JsonElement item, string branchId) =>
        item.GetProperty("branches").EnumerateArray().Single(branch => branch.GetProperty("branch_id").GetString() == branchId);

    public static int Available(JsonElement item, string branchId) => BranchOf(item, branchId).GetProperty("available").GetInt32();

    /// <summary>
    /// Puts a lot on the shelf straight through the domain, for what the API rightly refuses to
    /// receive — an already expired lot. Writes the ledger row with it, as the API would.
    /// </summary>
    public static async Task SeedLotAsync(
        ApiFixture fixture,
        string itemId,
        string branchId,
        int quantity,
        DateOnly? expiresOn,
        string lotNumber,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var lot = StockLot.Open(itemId, branchId, lotNumber, expiresOn, DateTimeOffset.UtcNow.AddDays(-400));
        var movement = StockMovement.Receive(lot, quantity, "stf_seed", DateTimeOffset.UtcNow.AddDays(-400));

        db.Set<StockLot>().Add(lot);
        db.Set<StockMovement>().Add(movement);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Every lot of the item at the branch, and the ledger rows behind them, read from the database.</summary>
    public static async Task<(IReadOnlyList<StockLot> Lots, IReadOnlyList<StockMovement> Movements)> LedgerAsync(
        ApiFixture fixture,
        string itemId,
        string branchId,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var lots = await db.Set<StockLot>().AsNoTracking()
            .Where(lot => lot.ItemId == itemId && lot.BranchId == branchId)
            .ToListAsync(cancellationToken);
        var movements = await db.Set<StockMovement>().AsNoTracking()
            .Where(movement => movement.ItemId == itemId && movement.BranchId == branchId)
            .ToListAsync(cancellationToken);

        return (lots, movements);
    }

    public static async Task<JsonElement> OkAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    public static async Task<string> ErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return body.GetProperty("error").GetProperty("code").GetString()!;
    }
}
