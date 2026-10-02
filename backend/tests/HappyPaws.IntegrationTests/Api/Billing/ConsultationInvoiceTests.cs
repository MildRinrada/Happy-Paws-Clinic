using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Billing;
using HappyPaws.IntegrationTests.Fixtures;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Billing;

/// <summary>
/// "ออกบิล" — the half of <c>POST /staff/consultations/{id}/close</c> that was named and unbuilt
/// until Billing existed, and the <c>invoice.issued ฿1,460</c> line in the contract's own worked
/// example.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ConsultationInvoiceTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Closing_a_case_raises_the_bill_for_what_was_done()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await TreatAsync(vet, open.ConsultationId, "ทำแผล", 101_000);
        await TreatAsync(vet, open.ConsultationId, "ตรวจรักษาทั่วไป", 45_000);
        await DiagnoseAsync(vet, open.ConsultationId, Token);

        var result = await CloseAsync(vet, open.ConsultationId, Token);
        var invoice = result.GetProperty("invoice");

        Assert.Equal(146_000, result.GetProperty("charges_satang").GetInt64());
        Assert.Equal(146_000, invoice.GetProperty("total_satang").GetInt64());
        Assert.Equal("open", invoice.GetProperty("status").GetString());
        Assert.Equal(open.ConsultationId, invoice.GetProperty("consultation_id").GetString());
        Assert.Equal(2, invoice.GetProperty("lines").GetArrayLength());
    }

    [Fact]
    public async Task The_bill_the_case_raised_is_the_one_the_cashier_collects_against()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);
        using var cashier = fixture.Cashier(open.Clinic);

        await TreatAsync(vet, open.ConsultationId, "ทำแผล", 101_000);
        await DiagnoseAsync(vet, open.ConsultationId, Token);

        var result = await CloseAsync(vet, open.ConsultationId, Token);
        var invoiceId = result.GetProperty("invoice").GetProperty("id").GetString()!;

        // The whole point of the extension read in contract-extensions § 7: the vet closed the case
        // upstairs, and the counter has nothing but the id and a customer standing in front of it.
        using var paid = await PayAsync(cashier, invoiceId, 101_000, Token, "promptpay", "PP-99381");
        paid.EnsureSuccessStatusCode();

        var settled = await paid.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("paid", settled.GetProperty("status").GetString());
        Assert.Equal("promptpay", settled.GetProperty("payments")[0].GetProperty("method").GetString());
    }

    [Fact]
    public async Task A_case_with_nothing_chargeable_raises_no_bill()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await DiagnoseAsync(vet, open.ConsultationId, Token);

        var result = await CloseAsync(vet, open.ConsultationId, Token);

        // A ฿0 invoice is not a courtesy — it is a document the counter has to look at and a row in
        // every revenue report, all to say nothing was charged.
        Assert.False(result.TryGetProperty("invoice", out var invoice) && invoice.ValueKind != JsonValueKind.Null);

        var raised = await InvoicesForCaseAsync(open.ConsultationId);
        Assert.Empty(raised);
    }

    [Fact]
    public async Task A_prescription_does_not_reach_the_bill()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await TreatAsync(vet, open.ConsultationId, "ทำแผล", 50_000);
        await DiagnoseAsync(vet, open.ConsultationId, Token);

        using var prescribe = BookingScenario.Idempotent(
            $"/staff/consultations/{open.ConsultationId}/prescriptions",
            new
            {
                drug_name = "Amoxicillin",
                dose = "250mg",
                frequency = "วันละ 2 ครั้ง",
                duration_days = 7,
                quantity = 14,
            });

        var prescribed = await vet.SendAsync(prescribe, Token);
        prescribed.EnsureSuccessStatusCode();

        var result = await CloseAsync(vet, open.ConsultationId, Token);
        var invoice = result.GetProperty("invoice");

        // A Prescription carries a drug, a dose and a quantity and no money anywhere. Medicine is
        // charged when it is handed over — "ตัดสต๊อกเมื่อจ่ายจริง" — which needs Inventory for its
        // price. Inventing one here would put a number on a bill nothing can justify.
        Assert.Equal(50_000, invoice.GetProperty("total_satang").GetInt64());
        Assert.Equal(1, invoice.GetProperty("lines").GetArrayLength());
    }

    [Fact]
    public async Task A_bill_cannot_be_raised_twice_for_one_case()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);
        using var cashier = fixture.Cashier(open.Clinic);

        await TreatAsync(vet, open.ConsultationId, "ทำแผล", 101_000);
        await DiagnoseAsync(vet, open.ConsultationId, Token);
        await CloseAsync(vet, open.ConsultationId, Token);

        using var duplicate = BookingScenario.Idempotent(
            "/staff/invoices", Bill(open.Clinic, open.ConsultationId));

        var response = await cashier.SendAsync(duplicate, Token);

        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Single(await InvoicesForCaseAsync(open.ConsultationId));
    }

    /// <summary>
    /// Records a billable procedure. Idempotent, because "a repeated line is a repeated charge" —
    /// and the response is checked, so a request this test forms wrongly fails here rather than
    /// silently producing a case with nothing on it.
    /// </summary>
    private static async Task TreatAsync(
        HttpClient vet,
        string consultationId,
        string name,
        long unitPriceSatang)
    {
        using var request = BookingScenario.Idempotent(
            $"/staff/consultations/{consultationId}/treatments",
            new { name, quantity = 1, unit_price_satang = unitPriceSatang });

        var response = await vet.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();
    }

    private async Task<IReadOnlyList<Invoice>> InvoicesForCaseAsync(string consultationId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<Invoice>().AsNoTracking()
            .Where(invoice => invoice.ConsultationId == consultationId)
            .ToListAsync(Token);
    }
}
