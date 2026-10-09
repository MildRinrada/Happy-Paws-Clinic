using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;
using static HappyPaws.IntegrationTests.Fixtures.CrmScenario;

namespace HappyPaws.IntegrationTests.Api.Crm;

/// <summary>
/// <c>POST /staff/packages</c>, "แพ็กเกจตรวจ 4 ครั้ง · ตัดสิทธิ์อัตโนมัติเมื่อใช้": sold on a bill, usable
/// once that bill is paid, spent automatically when a covered service is billed, and undone by a
/// void or a refund.
/// </summary>
[Collection(nameof(ApiCollection))]
public class PackageTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Selling_needs_crm_manage()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.CrmDesk(clinic, Scopes.Crm.Read);

        var response = await PostPackageAsync(reader, Package(clinic), Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("insufficient_scope", await ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task Selling_raises_money_so_it_needs_an_idempotency_key()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);

        var response = await desk.PostAsJsonAsync("/staff/packages", Package(clinic), Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("idempotency_key_required", await ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task A_package_without_services_or_with_an_unknown_service_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);

        var empty = await PostPackageAsync(desk, new
        {
            customer_id = clinic.CustomerId,
            name = "ว่าง",
            service_ids = Array.Empty<string>(),
            total_uses = 4,
            price_satang = 100_000,
            branch_id = clinic.BranchId,
        }, Token);
        var unknown = await PostPackageAsync(desk, new
        {
            customer_id = clinic.CustomerId,
            name = "บริการไม่มีจริง",
            service_ids = new[] { "svc_does_not_exist" },
            total_uses = 4,
            price_satang = 100_000,
            branch_id = clinic.BranchId,
        }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
    }

    [Fact]
    public async Task A_pet_that_is_not_the_customers_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (_, otherPetId) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var desk = fixture.CrmDesk(clinic);

        var response = await PostPackageAsync(desk, Package(clinic, petId: otherPetId), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Staff_cannot_sell_at_a_branch_they_do_not_work_at()
    {
        var home = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(home);

        var response = await PostPackageAsync(desk, Package(other), Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_sale_raises_its_bill_and_the_package_waits_for_payment_then_activates()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);

        var sale = await SellAsync(desk, clinic, Token);
        var package = sale.GetProperty("package");
        var invoice = sale.GetProperty("invoice");

        Assert.Equal("pending_payment", package.GetProperty("status").GetString());
        Assert.Equal(invoice.GetProperty("id").GetString(), package.GetProperty("invoice_id").GetString());
        Assert.Equal(180_000, invoice.GetProperty("total_satang").GetInt64());
        var line = Assert.Single(invoice.GetProperty("lines").EnumerateArray());
        Assert.Equal("other", line.GetProperty("kind").GetString());
        Assert.Equal(package.GetProperty("id").GetString(), line.GetProperty("source_id").GetString());

        // Half now: still waiting. The rest: usable.
        var invoiceId = invoice.GetProperty("id").GetString()!;
        (await PayAsync(desk, invoiceId, 90_000, Token)).EnsureSuccessStatusCode();
        Assert.Equal("pending_payment", (await FirstPackageAsync(desk, clinic)).GetProperty("status").GetString());

        (await PayAsync(desk, invoiceId, 90_000, Token)).EnsureSuccessStatusCode();
        var active = await FirstPackageAsync(desk, clinic);
        Assert.Equal("active", active.GetProperty("status").GetString());
        Assert.Equal(4, active.GetProperty("remaining").GetInt32());
    }

    [Fact]
    public async Task Retrying_a_sale_with_the_same_key_does_not_sell_twice()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var key = Guid.NewGuid().ToString();

        var first = await PostPackageAsync(desk, Package(clinic), Token, key);
        var second = await PostPackageAsync(desk, Package(clinic), Token, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Single((await LoyaltyAsync(desk, clinic.CustomerId, Token)).GetProperty("packages").EnumerateArray());
    }

    [Fact]
    public async Task A_counter_bill_for_a_covered_service_spends_a_use_and_charges_nothing_for_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        await SellPaidAsync(desk, clinic, Token);

        var bill = await RaiseAsync(desk, clinic, Token, ServiceBill(clinic));

        // ฿500 service covered, ฿200 supplies still charged.
        Assert.Equal(20_000, bill.GetProperty("total_satang").GetInt64());
        var covered = bill.GetProperty("lines").EnumerateArray().First(line => line.GetProperty("kind").GetString() == "service");
        Assert.Equal(0, covered.GetProperty("unit_price_satang").GetInt64());
        Assert.Contains("ใช้สิทธิ์แพ็กเกจ", covered.GetProperty("description").GetString(), StringComparison.Ordinal);

        Assert.Equal(3, (await FirstPackageAsync(desk, clinic)).GetProperty("remaining").GetInt32());
    }

    [Fact]
    public async Task An_unpaid_package_does_not_cover_anything()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        await SellAsync(desk, clinic, Token);

        var bill = await RaiseAsync(desk, clinic, Token, ServiceBill(clinic));

        Assert.Equal(70_000, bill.GetProperty("total_satang").GetInt64());
        Assert.Equal(4, (await FirstPackageAsync(desk, clinic)).GetProperty("remaining").GetInt32());
    }

    [Fact]
    public async Task A_package_for_one_pet_does_not_cover_a_bill_without_that_pet()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        await SellPaidAsync(desk, clinic, Token, Package(clinic, petId: clinic.PetId));

        var bill = await RaiseAsync(desk, clinic, Token, ServiceBill(clinic, withPet: false));

        Assert.Equal(70_000, bill.GetProperty("total_satang").GetInt64());
    }

    [Fact]
    public async Task More_than_is_left_is_split_into_a_covered_line_and_a_charged_line()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        await SellPaidAsync(desk, clinic, Token, Package(clinic, totalUses: 1, priceSatang: 40_000));

        var bill = await RaiseAsync(desk, clinic, Token, ServiceBill(clinic, quantity: 3));

        var services = bill.GetProperty("lines").EnumerateArray()
            .Where(line => line.GetProperty("kind").GetString() == "service")
            .Select(line => (line.GetProperty("quantity").GetInt32(), line.GetProperty("unit_price_satang").GetInt64()))
            .ToList();
        Assert.Equal([(1, 0L), (2, 50_000L)], services);
        Assert.Equal("used_up", (await FirstPackageAsync(desk, clinic)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Closing_a_case_spends_the_package_for_the_service_performed()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var desk = fixture.CrmDesk(open.Clinic);
        using var vet = fixture.Vet(open.Clinic);
        await SellPaidAsync(desk, open.Clinic, Token, Package(open.Clinic, totalUses: 2));

        using (var treat = BookingScenario.Idempotent(
                   $"/staff/consultations/{open.ConsultationId}/treatments",
                   new { name = "ตรวจสุขภาพ", quantity = 1, unit_price_satang = 50_000, service_id = open.Clinic.ServiceId }))
        {
            (await vet.SendAsync(treat, Token)).EnsureSuccessStatusCode();
        }

        await DiagnoseAsync(vet, open.ConsultationId, Token);
        var result = await CloseAsync(vet, open.ConsultationId, Token);

        var invoice = result.GetProperty("invoice");
        Assert.Equal(0, invoice.GetProperty("total_satang").GetInt64());
        Assert.Equal("paid", invoice.GetProperty("status").GetString());
        Assert.Equal(1, (await FirstPackageAsync(desk, open.Clinic)).GetProperty("remaining").GetInt32());
    }

    [Fact]
    public async Task Voiding_a_bill_gives_its_package_uses_back()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        await SellPaidAsync(desk, clinic, Token);
        var bill = await RaiseAsync(desk, clinic, Token, ServiceBill(clinic));

        var voided = await desk.PostAsJsonAsync(
            $"/staff/invoices/{bill.GetProperty("id").GetString()}/void", new { reason = "ออกบิลผิด" }, Token);
        voided.EnsureSuccessStatusCode();

        Assert.Equal(4, (await FirstPackageAsync(desk, clinic)).GetProperty("remaining").GetInt32());
    }

    [Fact]
    public async Task Voiding_an_unpaid_sale_cancels_the_package()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var sale = await SellAsync(desk, clinic, Token);

        var voided = await desk.PostAsJsonAsync(
            $"/staff/invoices/{sale.GetProperty("invoice").GetProperty("id").GetString()}/void", new { reason = "ลูกค้าเปลี่ยนใจ" }, Token);
        voided.EnsureSuccessStatusCode();

        Assert.Equal("cancelled", (await FirstPackageAsync(desk, clinic)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Refunding_the_sale_cancels_what_is_left_and_keeps_what_was_used()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var sale = await SellAsync(desk, clinic, Token);
        var saleInvoiceId = sale.GetProperty("invoice").GetProperty("id").GetString()!;
        (await PayAsync(desk, saleInvoiceId, 180_000, Token)).EnsureSuccessStatusCode();
        await RaiseAsync(desk, clinic, Token, ServiceBill(clinic));

        using (var refund = BookingScenario.Idempotent(
                   $"/staff/invoices/{saleInvoiceId}/refunds",
                   new { amount_satang = 135_000, reason = "คืนสิทธิ์ที่เหลือ 3 ครั้ง" }))
        {
            (await desk.SendAsync(refund, Token)).EnsureSuccessStatusCode();
        }

        var package = await FirstPackageAsync(desk, clinic);
        Assert.Equal("cancelled", package.GetProperty("status").GetString());
        Assert.Equal(1, package.GetProperty("used_count").GetInt32());

        // A cancelled package covers nothing.
        var bill = await RaiseAsync(desk, clinic, Token, ServiceBill(clinic));
        Assert.Equal(70_000, bill.GetProperty("total_satang").GetInt64());
    }

    private static async Task<JsonElement> FirstPackageAsync(HttpClient desk, SeededClinic clinic) =>
        (await LoyaltyAsync(desk, clinic.CustomerId, Token)).GetProperty("packages")[0];
}
