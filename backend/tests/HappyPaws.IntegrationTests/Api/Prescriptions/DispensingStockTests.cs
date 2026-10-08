using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Prescriptions;

/// <summary>
/// "จ่ายยาจริง · ตัดสต๊อก": a dispense takes the drug off the branch's shelf and puts it on the
/// case's bill, in one transaction, and never refuses the hand-over for stock.
/// </summary>
[Collection(nameof(ApiCollection))]
public class DispensingStockTests(ApiFixture fixture)
{
    private const long Price = 500;

    private static readonly string[] PharmacyScopes =
        [Scopes.Prescription.Read, Scopes.Prescription.Dispense, Scopes.Invoice.Write, .. ClinicalScopes];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed record Case(OpenCase Open, HttpClient Vet, HttpClient Store, string Drug, string ItemId) : IDisposable
    {
        public SeededClinic Clinic => Open.Clinic;

        public void Dispose()
        {
            Vet.Dispose();
            Store.Dispose();
        }
    }

    /// <summary>A case in the room, and a drug of its own in stock at the case's branch.</summary>
    private async Task<Case> CaseWithStockAsync(int stock)
    {
        var open = await OpenCaseAsync(fixture, Token);
        var store = fixture.Storekeeper(open.Clinic);
        var drug = UniqueName("Amoxicillin 250mg");
        var itemId = ItemId(await ReceiveNewAsync(store, open.Clinic, drug, stock, Token, priceSatang: Price, lotNumber: "L1", expiresOn: Day(200)));

        return new Case(open, fixture.Vet(open.Clinic, PharmacyScopes), store, drug, itemId);
    }

    private static async Task<string> PrescribeAsync(HttpClient vet, string consultationId, string drug, int quantity)
    {
        using var request = BookingScenario.Idempotent(
            $"/staff/consultations/{consultationId}/prescriptions",
            new { drug_name = drug, dose = "1 เม็ด", frequency = "วันละ 2 ครั้ง", duration_days = 7, quantity });

        var consultation = await OkAsync(await vet.SendAsync(request, Token), Token);
        return consultation.GetProperty("prescriptions").EnumerateArray().Last().GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> DispenseAsync(HttpClient vet, string prescriptionId) =>
        await OkAsync(await vet.PostAsync($"/staff/prescriptions/{prescriptionId}/dispense", null, Token), Token);

    private static async Task TreatAsync(HttpClient vet, string consultationId, long priceSatang, string? serviceId = null)
    {
        using var request = BookingScenario.Idempotent(
            $"/staff/consultations/{consultationId}/treatments",
            new { name = "ตรวจสุขภาพ", quantity = 1, unit_price_satang = priceSatang, service_id = serviceId });

        await OkAsync(await vet.SendAsync(request, Token), Token);
    }

    private static async Task<JsonElement> CloseCaseAsync(HttpClient vet, string consultationId)
    {
        await DiagnoseAsync(vet, consultationId, Token);
        return await CloseAsync(vet, consultationId, Token);
    }

    private static List<JsonElement> Lines(JsonElement invoice) => [.. invoice.GetProperty("lines").EnumerateArray()];

    [Fact]
    public async Task A_dispense_before_the_close_takes_stock_and_the_close_bills_it_beside_the_treatment()
    {
        using var @case = await CaseWithStockAsync(50);
        await TreatAsync(@case.Vet, @case.Open.ConsultationId, 70_000);
        var rx = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, @case.Drug, 14);

        var result = await DispenseAsync(@case.Vet, rx);

        Assert.True(result.GetProperty("stock_deducted").GetBoolean());
        var stock = result.GetProperty("stock");
        Assert.Equal("deducted", stock.GetProperty("status").GetString());
        Assert.Equal(@case.ItemId, stock.GetProperty("item_id").GetString());
        Assert.Equal(50, stock.GetProperty("available").GetInt32());
        Assert.Equal(14, stock.GetProperty("lots")[0].GetProperty("quantity").GetInt32());

        var charge = result.GetProperty("charge");
        Assert.Equal("on_close", charge.GetProperty("placement").GetString());
        Assert.Equal(14 * Price, charge.GetProperty("amount_satang").GetInt64());
        Assert.False(charge.TryGetProperty("invoice_id", out _));

        Assert.Equal(36, Available(await ItemAsync(@case.Store, @case.Clinic.BranchId, @case.Drug, Token), @case.Clinic.BranchId));

        var (_, movements) = await LedgerAsync(fixture, @case.ItemId, @case.Clinic.BranchId, Token);
        var dispensed = Assert.Single(movements, movement => movement.PrescriptionId == rx);
        Assert.Equal(-14, dispensed.Quantity);

        var closed = await CloseCaseAsync(@case.Vet, @case.Open.ConsultationId);
        var invoice = closed.GetProperty("invoice");
        var lines = Lines(invoice);

        Assert.Equal(2, lines.Count);
        var medicine = Assert.Single(lines, line => line.GetProperty("kind").GetString() == "medicine");
        Assert.Equal(rx, medicine.GetProperty("source_id").GetString());
        Assert.Equal(14, medicine.GetProperty("quantity").GetInt32());
        Assert.Equal(Price, medicine.GetProperty("unit_price_satang").GetInt64());
        Assert.Equal(70_000 + (14 * Price), invoice.GetProperty("total_satang").GetInt64());

        // The history says what the dispense did.
        var history = await @case.Vet.GetFromJsonAsync<JsonElement>($"/staff/prescriptions?pet_id={@case.Clinic.PetId}", Token);
        var row = history.GetProperty("data").EnumerateArray().Single(item => item.GetProperty("id").GetString() == rx);
        Assert.True(row.GetProperty("stock_deducted").GetBoolean());
        Assert.Equal(Price, row.GetProperty("charged_unit_price_satang").GetInt64());
        Assert.Equal(@case.ItemId, row.GetProperty("inventory_item_id").GetString());
    }

    [Fact]
    public async Task A_dispense_after_the_close_lands_on_the_cases_open_bill()
    {
        using var @case = await CaseWithStockAsync(20);
        await TreatAsync(@case.Vet, @case.Open.ConsultationId, 40_000);
        var rx = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, @case.Drug, 10);

        var closed = await CloseCaseAsync(@case.Vet, @case.Open.ConsultationId);
        var invoiceId = closed.GetProperty("invoice").GetProperty("id").GetString()!;
        Assert.Single(Lines(closed.GetProperty("invoice")));

        var charge = (await DispenseAsync(@case.Vet, rx)).GetProperty("charge");

        Assert.Equal("invoiced", charge.GetProperty("placement").GetString());
        Assert.Equal(invoiceId, charge.GetProperty("invoice_id").GetString());

        var invoice = await BillingScenario.ReadAsync(@case.Vet, invoiceId, Token);
        Assert.Equal(40_000 + (10 * Price), invoice.GetProperty("total_satang").GetInt64());
        Assert.Equal("open", invoice.GetProperty("status").GetString());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.True(await db.Set<AuditEntry>().AnyAsync(entry => entry.Action == AuditActions.InvoiceLineAdded && entry.ResourceId == invoiceId, Token));
    }

    [Fact]
    public async Task A_case_that_closed_with_nothing_to_charge_gets_its_bill_when_the_medicine_is_dispensed()
    {
        using var @case = await CaseWithStockAsync(20);
        var rx = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, @case.Drug, 4);

        var closed = await CloseCaseAsync(@case.Vet, @case.Open.ConsultationId);
        Assert.False(closed.TryGetProperty("invoice", out _));

        var charge = (await DispenseAsync(@case.Vet, rx)).GetProperty("charge");
        Assert.Equal("invoiced", charge.GetProperty("placement").GetString());

        var invoice = await BillingScenario.ReadAsync(@case.Vet, charge.GetProperty("invoice_id").GetString()!, Token);
        Assert.Equal(@case.Open.ConsultationId, invoice.GetProperty("consultation_id").GetString());
        Assert.Equal(4 * Price, invoice.GetProperty("total_satang").GetInt64());
    }

    [Fact]
    public async Task A_bill_that_has_taken_money_is_never_changed_the_medicine_gets_a_bill_of_its_own()
    {
        using var @case = await CaseWithStockAsync(20);
        await TreatAsync(@case.Vet, @case.Open.ConsultationId, 30_000);
        var rx = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, @case.Drug, 6);

        var closed = await CloseCaseAsync(@case.Vet, @case.Open.ConsultationId);
        var caseInvoice = closed.GetProperty("invoice").GetProperty("id").GetString()!;
        using var cashier = fixture.Cashier(@case.Clinic);
        (await BillingScenario.PayAsync(cashier, caseInvoice, 30_000, Token)).EnsureSuccessStatusCode();

        var charge = (await DispenseAsync(@case.Vet, rx)).GetProperty("charge");

        Assert.Equal("separate_invoice", charge.GetProperty("placement").GetString());
        var separate = charge.GetProperty("invoice_id").GetString()!;
        Assert.NotEqual(caseInvoice, separate);

        var paid = await BillingScenario.ReadAsync(cashier, caseInvoice, Token);
        Assert.Equal(30_000, paid.GetProperty("total_satang").GetInt64());
        Assert.Equal("paid", paid.GetProperty("status").GetString());

        var own = await BillingScenario.ReadAsync(cashier, separate, Token);
        Assert.False(own.TryGetProperty("consultation_id", out _));
        Assert.Equal(@case.Clinic.PetId, own.GetProperty("pet_id").GetString());
        Assert.Equal(6 * Price, own.GetProperty("total_satang").GetInt64());
        Assert.Contains(@case.Open.ConsultationId, own.GetProperty("note").GetString());
    }

    [Fact]
    public async Task A_drug_that_matches_no_item_is_dispensed_without_touching_stock_or_the_bill()
    {
        using var @case = await CaseWithStockAsync(20);
        await TreatAsync(@case.Vet, @case.Open.ConsultationId, 30_000);
        var rx = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, UniqueName("Unstocked drug"), 6);

        var result = await DispenseAsync(@case.Vet, rx);

        Assert.False(result.GetProperty("stock_deducted").GetBoolean());
        Assert.Equal("not_matched", result.GetProperty("stock").GetProperty("status").GetString());
        Assert.False(result.GetProperty("stock").TryGetProperty("item_id", out _));
        Assert.False(result.TryGetProperty("charge", out _));
        Assert.NotNull(result.GetProperty("prescription").GetProperty("dispensed_at").GetString());

        var closed = await CloseCaseAsync(@case.Vet, @case.Open.ConsultationId);
        Assert.DoesNotContain(Lines(closed.GetProperty("invoice")), line => line.GetProperty("kind").GetString() == "medicine");
    }

    [Fact]
    public async Task Too_little_usable_stock_still_dispenses_and_charges_but_leaves_the_shelf_alone()
    {
        using var @case = await CaseWithStockAsync(3);

        // An expired lot does not count: the shelf has 3 usable and 50 expired.
        await SeedLotAsync(fixture, @case.ItemId, @case.Clinic.BranchId, 50, AnalyticsScenario.Today.AddDays(-1), "EXP", Token);
        var rx = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, @case.Drug, 10);

        var result = await DispenseAsync(@case.Vet, rx);

        Assert.False(result.GetProperty("stock_deducted").GetBoolean());
        Assert.Equal("insufficient", result.GetProperty("stock").GetProperty("status").GetString());
        Assert.Equal(3, result.GetProperty("stock").GetProperty("available").GetInt32());
        Assert.Equal(10 * Price, result.GetProperty("charge").GetProperty("amount_satang").GetInt64());

        var branch = BranchOf(await ItemAsync(@case.Store, @case.Clinic.BranchId, @case.Drug, Token), @case.Clinic.BranchId);
        Assert.Equal(3, branch.GetProperty("available").GetInt32());
        Assert.Equal(53, branch.GetProperty("on_hand").GetInt32());
    }

    [Fact]
    public async Task Two_counters_dispensing_the_last_units_at_once_take_them_once()
    {
        using var @case = await CaseWithStockAsync(10);
        var first = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, @case.Drug, 10);
        var second = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, @case.Drug, 10);

        var responses = await Task.WhenAll(
            @case.Vet.PostAsync($"/staff/prescriptions/{first}/dispense", null, Token),
            @case.Vet.PostAsync($"/staff/prescriptions/{second}/dispense", null, Token));

        var results = new List<JsonElement>();
        foreach (var response in responses)
        {
            results.Add(await OkAsync(response, Token));
        }

        var statuses = results.Select(result => result.GetProperty("stock").GetProperty("status").GetString()).Order().ToList();
        Assert.Equal(["deducted", "insufficient"], statuses);

        var (lots, movements) = await LedgerAsync(fixture, @case.ItemId, @case.Clinic.BranchId, Token);
        Assert.Equal(0, lots.Sum(lot => lot.QuantityOnHand));
        Assert.Equal(0, movements.Sum(movement => movement.Quantity));
        Assert.Single(movements, movement => movement.PrescriptionId is not null);
    }

    [Fact]
    public async Task The_service_report_sets_dispensed_medicine_beside_the_service_it_was_sold_with()
    {
        using var @case = await CaseWithStockAsync(30);
        await TreatAsync(@case.Vet, @case.Open.ConsultationId, 70_000, @case.Clinic.ServiceId);
        var rx = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, @case.Drug, 14);
        await DispenseAsync(@case.Vet, rx);

        var closed = await CloseCaseAsync(@case.Vet, @case.Open.ConsultationId);
        var invoiceId = closed.GetProperty("invoice").GetProperty("id").GetString()!;
        using var cashier = fixture.Cashier(@case.Clinic);
        (await BillingScenario.PayAsync(cashier, invoiceId, 70_000 + (14 * Price), Token)).EnsureSuccessStatusCode();

        using var reader = fixture.Reader(@case.Clinic, Scopes.Analytics.Read);
        var report = await AnalyticsScenario.ReadAsync(reader, $"/staff/analytics/services?{AnalyticsScenario.Around(@case.Clinic)}", Token);

        var service = Assert.Single(report.GetProperty("services").EnumerateArray());
        Assert.Equal(70_000, service.GetProperty("revenue_satang").GetInt64());
        Assert.Equal(14 * Price, service.GetProperty("medicine_satang").GetInt64());
        Assert.Equal(70_000 - (14 * Price), service.GetProperty("revenue_excluding_medicine_satang").GetInt64());

        var kinds = report.GetProperty("by_kind").EnumerateArray()
            .ToDictionary(row => row.GetProperty("kind").GetString()!, row => row.GetProperty("revenue_satang").GetInt64());
        Assert.Equal(14 * Price, kinds["medicine"]);
        Assert.Equal(70_000 + (14 * Price), kinds.Values.Sum());
    }

    [Fact]
    public async Task Dispensing_needs_rx_dispense_whatever_inventory_scopes_are_held()
    {
        using var @case = await CaseWithStockAsync(5);
        var rx = await PrescribeAsync(@case.Vet, @case.Open.ConsultationId, @case.Drug, 1);

        var response = await @case.Store.PostAsync($"/staff/prescriptions/{rx}/dispense", null, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(5, Available(await ItemAsync(@case.Store, @case.Clinic.BranchId, @case.Drug, Token), @case.Clinic.BranchId));
    }
}
