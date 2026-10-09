using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Prescriptions;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Inventory;

/// <summary>
/// <c>docs/contract-extensions.md § 25</c>: <c>PATCH /staff/inventory/items/{id}</c>, and what retiring
/// an item does to the drug picker, prescribing, receiving, ordering and the warnings.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ItemEditTests(ApiFixture fixture)
{
    private const long Price = 700;

    private static readonly string[] PharmacyScopes =
        [Scopes.Prescription.Read, Scopes.Prescription.Dispense, Scopes.Invoice.Write, .. ClinicalScopes];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<HttpResponseMessage> PatchAsync(HttpClient client, string itemId, object body, uint? ifMatch = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/staff/inventory/items/{itemId}") { Content = JsonContent.Create(body) };

        if (ifMatch is { } version)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        }

        return await client.SendAsync(request, Token);
    }

    private static async Task<HttpResponseMessage> PrescribeAsync(HttpClient vet, string consultationId, string itemId, int quantity = 5)
    {
        using var request = BookingScenario.Idempotent($"/staff/consultations/{consultationId}/prescriptions", new
        {
            inventory_item_id = itemId,
            dose = "1 เม็ด",
            frequency = "วันละ 2 ครั้ง",
            duration_days = 5,
            quantity,
        });

        return await vet.SendAsync(request, Token);
    }

    private static string LastPrescriptionId(JsonElement consultation) =>
        consultation.GetProperty("prescriptions").EnumerateArray().Last().GetProperty("id").GetString()!;

    [Fact]
    public async Task Editing_needs_inventory_write_and_keeps_the_creation_rules()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);
        var itemId = ItemId(await ReceiveNewAsync(store, clinic, UniqueName("Doxycycline 100mg"), 5, Token));
        using var reader = fixture.Storekeeper(clinic, Scopes.Inventory.Read);

        Assert.Equal(HttpStatusCode.Forbidden, (await PatchAsync(reader, itemId, new { reorder_level = 3 })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PatchAsync(store, itemId, new { name = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PatchAsync(store, itemId, new { unit_price_satang = -1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PatchAsync(store, "itm_does_not_exist", new { reorder_level = 3 })).StatusCode);
    }

    [Fact]
    public async Task A_rename_onto_another_items_name_is_refused_and_a_stale_version_too()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);
        var taken = UniqueName("Enrofloxacin 50mg");
        var takenId = ItemId(await ReceiveNewAsync(store, clinic, taken, 1, Token));
        var itemId = ItemId(await ReceiveNewAsync(store, clinic, UniqueName("Marbofloxacin 20mg"), 1, Token));

        var clash = await PatchAsync(store, itemId, new { name = taken.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        Assert.Equal("inventory_item_exists", await ErrorCodeAsync(clash, Token));
        Assert.Contains(takenId, await clash.Content.ReadAsStringAsync(Token));

        var item = await OkAsync(await PatchAsync(store, itemId, new { reorder_level = 7 }), Token);
        var version = item.GetProperty("version").GetUInt32();
        await OkAsync(await PatchAsync(store, itemId, new { reorder_level = 8 }, version), Token);

        var stale = await PatchAsync(store, itemId, new { reorder_level = 9 }, version);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("version_conflict", await ErrorCodeAsync(stale, Token));
    }

    [Fact]
    public async Task A_new_price_charges_dispenses_from_now_on_and_leaves_what_was_dispensed()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var store = fixture.Storekeeper(open.Clinic);
        var drug = UniqueName("Prednisolone 5mg");
        var itemId = ItemId(await ReceiveNewAsync(store, open.Clinic, drug, 40, Token, priceSatang: Price));
        using var vet = fixture.Vet(open.Clinic, PharmacyScopes);

        var first = LastPrescriptionId(await OkAsync(await PrescribeAsync(vet, open.ConsultationId, itemId), Token));
        var dispensed = await OkAsync(await vet.PostAsync($"/staff/prescriptions/{first}/dispense", null, Token), Token);
        Assert.Equal(5 * Price, dispensed.GetProperty("charge").GetProperty("amount_satang").GetInt64());

        var edited = await OkAsync(await PatchAsync(store, itemId, new { unit_price_satang = 900, unit = "เม็ดเล็ก" }), Token);
        Assert.Equal(900, edited.GetProperty("unit_price_satang").GetInt64());
        Assert.Equal("เม็ดเล็ก", edited.GetProperty("unit").GetString());

        var second = LastPrescriptionId(await OkAsync(await PrescribeAsync(vet, open.ConsultationId, itemId), Token));
        var later = await OkAsync(await vet.PostAsync($"/staff/prescriptions/{second}/dispense", null, Token), Token);
        Assert.Equal(5 * 900, later.GetProperty("charge").GetProperty("amount_satang").GetInt64());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var charged = await db.Set<Prescription>().AsNoTracking()
            .Where(prescription => prescription.Id == first || prescription.Id == second)
            .ToDictionaryAsync(prescription => prescription.Id, prescription => prescription.ChargedUnitPriceSatang, Token);
        Assert.Equal(Price, charged[first]);
        Assert.Equal(900, charged[second]);
        Assert.True(await db.Set<AuditEntry>().AnyAsync(
            entry => entry.Action == AuditActions.InventoryItemUpdated && entry.ResourceId == itemId, Token));
    }

    [Fact]
    public async Task A_retired_item_leaves_the_picker_and_new_use_but_what_was_prescribed_is_still_dispensed()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var store = fixture.Storekeeper(open.Clinic);
        var drug = UniqueName("Furosemide 40mg");
        var itemId = ItemId(await ReceiveNewAsync(store, open.Clinic, drug, 30, Token, priceSatang: Price));
        using var vet = fixture.Vet(open.Clinic, PharmacyScopes);
        var prescribedBefore = LastPrescriptionId(await OkAsync(await PrescribeAsync(vet, open.ConsultationId, itemId), Token));

        var retired = await OkAsync(await PatchAsync(store, itemId, new { active = false }), Token);
        Assert.False(retired.GetProperty("active").GetBoolean());
        Assert.Equal(JsonValueKind.String, retired.GetProperty("retired_at").ValueKind);

        var picker = await vet.GetFromJsonAsync<JsonElement>(
            $"/staff/prescriptions/drug-options?q={Uri.EscapeDataString(drug)}&branch={open.Clinic.BranchId}", Token);
        Assert.Empty(picker.GetProperty("data").EnumerateArray());

        var prescribe = await PrescribeAsync(vet, open.ConsultationId, itemId);
        Assert.Equal(HttpStatusCode.Conflict, prescribe.StatusCode);
        Assert.Equal("inventory_item_retired", await ErrorCodeAsync(prescribe, Token));

        var receive = await MoveAsync(store, new { type = "receive", branch_id = open.Clinic.BranchId, item_id = itemId, quantity = 5 }, Token);
        Assert.Equal("inventory_item_retired", await ErrorCodeAsync(receive, Token));

        using (var order = BookingScenario.Idempotent("/staff/inventory/purchase-orders", new
        {
            supplier_name = "ซัพพลายเออร์",
            lines = new object[] { new { item_id = itemId, quantity = 10, unit_cost_satang = 300 } },
        }))
        {
            var ordered = await store.SendAsync(order, Token);
            Assert.Equal("inventory_item_retired", await ErrorCodeAsync(ordered, Token));
        }

        // The same name cannot be made again as a new item; the answer says the old one is retired.
        var again = await MoveAsync(store, new
        {
            type = "receive",
            branch_id = open.Clinic.BranchId,
            new_item = new { name = drug, unit = "เม็ด", unit_price_satang = 100 },
            quantity = 1,
        }, Token);
        var error = (await again.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("inventory_item_exists", error.GetProperty("code").GetString());
        Assert.True(error.GetProperty("details").GetProperty("retired").GetBoolean());

        // Units on the shelf are still dispensed against what was prescribed before.
        var dispensed = await OkAsync(await vet.PostAsync($"/staff/prescriptions/{prescribedBefore}/dispense", null, Token), Token);
        Assert.Equal("deducted", dispensed.GetProperty("stock").GetProperty("status").GetString());

        // The list still shows it, marked, and can leave it out.
        Assert.False((await ItemAsync(store, open.Clinic.BranchId, drug, Token)).GetProperty("active").GetBoolean());
        var inUse = await store.GetFromJsonAsync<JsonElement>(
            $"/staff/inventory/items?branch={open.Clinic.BranchId}&active=true&q={Uri.EscapeDataString(drug)}", Token);
        Assert.Empty(inUse.GetProperty("data").EnumerateArray());

        await OkAsync(await PatchAsync(store, itemId, new { active = true }), Token);
        var back = await vet.GetFromJsonAsync<JsonElement>(
            $"/staff/prescriptions/drug-options?q={Uri.EscapeDataString(drug)}&branch={open.Clinic.BranchId}", Token);
        Assert.Single(back.GetProperty("data").EnumerateArray());
    }

    [Fact]
    public async Task A_retired_item_raises_no_low_stock_warning_but_its_expiring_lots_still_show()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);
        var itemId = ItemId(await ReceiveNewAsync(
            store, clinic, UniqueName("Metronidazole 250mg"), 3, Token, reorderLevel: 50, lotNumber: "R1", expiresOn: Day(20)));

        async Task<List<string?>> KindsAsync() =>
            [.. (await store.GetFromJsonAsync<JsonElement>($"/staff/inventory/alerts?branch={clinic.BranchId}&within_days=30", Token))
                .GetProperty("data").EnumerateArray()
                .Where(row => row.GetProperty("item_id").GetString() == itemId)
                .Select(row => row.GetProperty("kind").GetString())];

        Assert.Contains("low_stock", await KindsAsync());

        await OkAsync(await PatchAsync(store, itemId, new { active = false }), Token);

        var kinds = await KindsAsync();
        Assert.DoesNotContain("low_stock", kinds);
        Assert.Contains("near_expiry", kinds);
    }
}
