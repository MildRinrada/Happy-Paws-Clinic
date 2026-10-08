using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Prescriptions;

/// <summary>
/// <c>docs/contract-extensions.md § 20</c>: a prescriber picks the drug from stock, and the dispense
/// takes and charges that item whatever the name says. The picker shows a prescriber only what
/// writing a drug needs, under <c>rx:write</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class DrugPickerTests(ApiFixture fixture)
{
    private const long Price = 700;

    private static readonly string[] PharmacyScopes =
        [Scopes.Prescription.Read, Scopes.Prescription.Dispense, Scopes.Invoice.Write, .. ClinicalScopes];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string Options(string q, string? branch = null) =>
        $"/staff/prescriptions/drug-options?q={Uri.EscapeDataString(q)}" + (branch is null ? string.Empty : $"&branch={branch}");

    private static async Task<HttpResponseMessage> PrescribeAsync(HttpClient vet, string consultationId, object body)
    {
        using var request = BookingScenario.Idempotent($"/staff/consultations/{consultationId}/prescriptions", body);
        return await vet.SendAsync(request, Token);
    }

    private static object Rx(string? drugName = null, string? itemId = null, int quantity = 10, string? acknowledge = null) => new
    {
        drug_name = drugName,
        inventory_item_id = itemId,
        dose = "1 เม็ด",
        frequency = "วันละ 2 ครั้ง",
        duration_days = 5,
        quantity,
        acknowledge_allergy = acknowledge,
    };

    private static JsonElement LastPrescription(JsonElement consultation) =>
        consultation.GetProperty("prescriptions").EnumerateArray().Last();

    [Fact]
    public async Task The_picker_needs_rx_write_and_inventory_read_does_not_open_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var storekeeper = fixture.Storekeeper(clinic);

        var response = await storekeeper.GetAsync(Options("x", clinic.BranchId), Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("insufficient_scope", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains(
            Scopes.Prescription.Write,
            body.GetProperty("error").GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public async Task A_vet_without_inventory_read_sees_medicines_with_price_and_status_and_nothing_about_cost()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);
        var tag = Guid.NewGuid().ToString("N")[..10];
        var drug = $"Meloxicam 1mg {tag}";
        var itemId = ItemId(await ReceiveNewAsync(store, clinic, drug, 3, Token, priceSatang: Price, reorderLevel: 5, lotNumber: "M1", expiresOn: Day(100)));

        // A supply with the same tag is stocked but never offered to a prescriber.
        await OkAsync(await MoveAsync(store, new
        {
            type = "receive",
            branch_id = clinic.BranchId,
            new_item = new { name = $"Syringe 3ml {tag}", kind = "supply", unit = "อัน", unit_price_satang = 1_000 },
            quantity = 40,
            unit_cost_satang = 400,
        }, Token), Token);

        using var vet = fixture.Vet(clinic);
        var response = await vet.GetAsync(Options(tag), Token);
        var raw = await response.Content.ReadAsStringAsync(Token);
        Assert.True(response.IsSuccessStatusCode, raw);

        Assert.DoesNotContain("cost", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lot", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("supplier", raw, StringComparison.OrdinalIgnoreCase);

        var body = JsonDocument.Parse(raw).RootElement;
        Assert.Equal(clinic.BranchId, body.GetProperty("branch_id").GetString());

        var option = Assert.Single(body.GetProperty("data").EnumerateArray());
        Assert.Equal(
            ["available", "item_id", "name", "status", "unit", "unit_price_satang"],
            option.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(itemId, option.GetProperty("item_id").GetString());
        Assert.Equal(drug, option.GetProperty("name").GetString());
        Assert.Equal("เม็ด", option.GetProperty("unit").GetString());
        Assert.Equal(Price, option.GetProperty("unit_price_satang").GetInt64());
        Assert.Equal(3, option.GetProperty("available").GetInt32());
        Assert.Equal("low", option.GetProperty("status").GetString());
    }

    [Fact]
    public async Task The_picker_shows_only_the_callers_branch_and_refuses_another()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var them = fixture.Storekeeper(theirs);
        var drug = UniqueName("Doxycycline 100mg");
        await ReceiveNewAsync(them, theirs, drug, 30, Token);

        using var vet = fixture.Vet(mine);

        var refused = await vet.GetAsync(Options(drug, theirs.BranchId), Token);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);

        // The catalogue is the clinic's, so the drug is offered, with my branch's stock: none.
        var body = await OkAsync(await vet.GetAsync(Options(drug), Token), Token);
        Assert.Equal(mine.BranchId, body.GetProperty("branch_id").GetString());
        var option = Assert.Single(body.GetProperty("data").EnumerateArray());
        Assert.Equal(0, option.GetProperty("available").GetInt32());
        Assert.Equal("out", option.GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_drug_picked_from_stock_takes_the_items_name_and_carries_its_id()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var store = fixture.Storekeeper(open.Clinic);
        var drug = UniqueName("Cefalexin 250mg");
        var itemId = ItemId(await ReceiveNewAsync(store, open.Clinic, drug, 20, Token, priceSatang: Price));
        using var vet = fixture.Vet(open.Clinic);

        var consultation = await OkAsync(await PrescribeAsync(vet, open.ConsultationId, Rx(itemId: itemId)), Token);

        var written = LastPrescription(consultation);
        Assert.Equal(drug, written.GetProperty("drug_name").GetString());
        Assert.Equal(itemId, written.GetProperty("inventory_item_id").GetString());
    }

    [Fact]
    public async Task An_unknown_item_is_404_and_a_supply_is_422_and_neither_writes_anything()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var store = fixture.Storekeeper(open.Clinic);
        var supply = ItemId(await OkAsync(await MoveAsync(store, new
        {
            type = "receive",
            branch_id = open.Clinic.BranchId,
            new_item = new { name = UniqueName("Gauze"), kind = "supply", unit = "ห่อ", unit_price_satang = 500 },
            quantity = 10,
        }, Token), Token));
        using var vet = fixture.Vet(open.Clinic);

        var unknown = await PrescribeAsync(vet, open.ConsultationId, Rx("Something", "itm_doesnotexist"));
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var notADrug = await PrescribeAsync(vet, open.ConsultationId, Rx(itemId: supply));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, notADrug.StatusCode);
        Assert.Contains("inventory_item_id", await notADrug.Content.ReadAsStringAsync(Token));

        var consultation = await vet.GetFromJsonAsync<JsonElement>($"/staff/consultations/{open.ConsultationId}", Token);
        Assert.Empty(consultation.GetProperty("prescriptions").EnumerateArray());
    }

    [Fact]
    public async Task A_picked_drug_is_dispensed_from_its_item_and_charged_even_when_the_name_differs()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var store = fixture.Storekeeper(open.Clinic);
        var drug = UniqueName("Amoxicillin 250mg");
        var itemId = ItemId(await ReceiveNewAsync(store, open.Clinic, drug, 30, Token, priceSatang: Price, lotNumber: "A1", expiresOn: Day(150)));
        using var vet = fixture.Vet(open.Clinic, PharmacyScopes);

        // Written with a spacing the name match would never find.
        var typed = drug.Replace("250mg", "250 mg");
        var consultation = await OkAsync(await PrescribeAsync(vet, open.ConsultationId, Rx(typed, itemId, quantity: 12)), Token);
        var written = LastPrescription(consultation);
        Assert.Equal(typed, written.GetProperty("drug_name").GetString());
        var rx = written.GetProperty("id").GetString()!;

        var result = await OkAsync(await vet.PostAsync($"/staff/prescriptions/{rx}/dispense", null, Token), Token);

        Assert.True(result.GetProperty("stock_deducted").GetBoolean());
        Assert.Equal("deducted", result.GetProperty("stock").GetProperty("status").GetString());
        Assert.Equal(itemId, result.GetProperty("stock").GetProperty("item_id").GetString());
        Assert.Equal(12 * Price, result.GetProperty("charge").GetProperty("amount_satang").GetInt64());
        Assert.Equal(typed, result.GetProperty("label").GetProperty("drug_name").GetString());
        Assert.Equal(18, Available(await ItemAsync(store, open.Clinic.BranchId, drug, Token), open.Clinic.BranchId));
    }

    [Fact]
    public async Task A_typed_drug_is_still_matched_by_name_only_and_an_inexact_name_takes_nothing()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var store = fixture.Storekeeper(open.Clinic);
        var drug = UniqueName("Enrofloxacin 50mg");
        var itemId = ItemId(await ReceiveNewAsync(store, open.Clinic, drug, 30, Token, priceSatang: Price));
        using var vet = fixture.Vet(open.Clinic, PharmacyScopes);

        var exact = LastPrescription(await OkAsync(await PrescribeAsync(vet, open.ConsultationId, Rx(drug.ToUpperInvariant(), quantity: 5)), Token));
        var near = LastPrescription(await OkAsync(await PrescribeAsync(vet, open.ConsultationId, Rx(drug.Replace("50mg", "50 mg"), quantity: 5)), Token));

        // Typed drugs are not tied to stock until the dispense matches them.
        Assert.False(exact.TryGetProperty("inventory_item_id", out _));
        Assert.False(near.TryGetProperty("inventory_item_id", out _));

        var matched = await OkAsync(await vet.PostAsync($"/staff/prescriptions/{exact.GetProperty("id").GetString()}/dispense", null, Token), Token);
        Assert.Equal("deducted", matched.GetProperty("stock").GetProperty("status").GetString());
        Assert.Equal(itemId, matched.GetProperty("stock").GetProperty("item_id").GetString());

        var missed = await OkAsync(await vet.PostAsync($"/staff/prescriptions/{near.GetProperty("id").GetString()}/dispense", null, Token), Token);
        Assert.Equal("not_matched", missed.GetProperty("stock").GetProperty("status").GetString());
        Assert.False(missed.TryGetProperty("charge", out _));

        Assert.Equal(25, Available(await ItemAsync(store, open.Clinic.BranchId, drug, Token), open.Clinic.BranchId));
    }

    [Fact]
    public async Task A_picked_drug_is_checked_for_allergy_under_the_stocked_name_too()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var store = fixture.Storekeeper(open.Clinic);
        var tag = Guid.NewGuid().ToString("N")[..10];
        var itemId = ItemId(await ReceiveNewAsync(store, open.Clinic, $"Cephazolin{tag} 1g", 5, Token));
        using var vet = fixture.Vet(open.Clinic);

        var allergy = await vet.PostAsJsonAsync(
            $"/staff/pets/{open.Clinic.PetId}/allergies",
            new { substance = $"Cephazolin{tag}", severity = "moderate", reaction = "ผื่น" },
            Token);
        allergy.EnsureSuccessStatusCode();

        var refused = await PrescribeAsync(vet, open.ConsultationId, Rx("ยาฉีดฆ่าเชื้อ", itemId));
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("allergy_conflict", await refused.Content.ReadAsStringAsync(Token));

        var acknowledged = await OkAsync(await PrescribeAsync(vet, open.ConsultationId, Rx("ยาฉีดฆ่าเชื้อ", itemId, acknowledge: "ทดสอบแล้วไม่แพ้")), Token);
        Assert.Equal(itemId, LastPrescription(acknowledged).GetProperty("inventory_item_id").GetString());
    }
}
