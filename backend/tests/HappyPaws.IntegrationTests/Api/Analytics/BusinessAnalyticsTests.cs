using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Billing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.AnalyticsScenario;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Analytics;

/// <summary>
/// <c>/staff/analytics/*</c>: the money adds up the way Billing says it should, and the customer
/// and staff figures count what actually happened.
/// </summary>
[Collection(nameof(ApiCollection))]
public class BusinessAnalyticsTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static long Satang(JsonElement element, string name) => element.GetProperty(name).GetInt64();

    private static int Int(JsonElement element, string name) => element.GetProperty(name).GetInt32();

    private static JsonElement Method(JsonElement report, string method) =>
        report.GetProperty("by_method").EnumerateArray().Single(row => row.GetProperty("method").GetString() == method);

    [Fact]
    public async Task Revenue_is_money_taken_less_refunds_by_tender_and_never_counts_a_voided_or_unpaid_bill()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        // ฿1,460 paid in two tenders, then ฿200 handed back in cash.
        var paid = await BillingScenario.RaiseIdAsync(cashier, clinic, Token);
        await PayAsync(cashier, paid, 46_000, "cash", Token);
        await PayAsync(cashier, paid, 100_000, "card", Token);
        await RefundAsync(cashier, paid, 20_000, "cash", Token);

        // A bill raised by mistake and voided, and one nobody has paid yet.
        var voided = await BillingScenario.RaiseIdAsync(cashier, clinic, Token);
        await VoidAsync(cashier, voided, Token);
        await BillingScenario.RaiseIdAsync(cashier, clinic, Token);

        using var reader = fixture.Reader(clinic, Scopes.Analytics.Read);
        var report = await ReadAsync(reader, $"/staff/analytics/revenue?{Around(clinic)}", Token);
        var totals = report.GetProperty("totals");

        Assert.Equal(146_000, Satang(totals, "collected_satang"));
        Assert.Equal(20_000, Satang(totals, "refunded_satang"));
        Assert.Equal(126_000, Satang(totals, "net_satang"));
        Assert.Equal(2, Int(totals, "payments"));
        Assert.Equal(1, Int(totals, "refunds"));

        // Billed: the paid bill and the unpaid one. The voided bill is not a sale.
        Assert.Equal(292_000, Satang(totals, "invoiced_satang"));
        Assert.Equal(2, Int(totals, "invoices"));

        Assert.Equal(26_000, Satang(Method(report, "cash"), "net_satang"));
        Assert.Equal(20_000, Satang(Method(report, "cash"), "refunded_satang"));
        Assert.Equal(100_000, Satang(Method(report, "card"), "net_satang"));
        Assert.Equal(0, Satang(Method(report, "promptpay"), "collected_satang"));

        var branch = Assert.Single(report.GetProperty("by_branch").EnumerateArray());
        Assert.Equal(clinic.BranchId, branch.GetProperty("branch_id").GetString());
        Assert.Equal("สาขาทดสอบ", branch.GetProperty("branch_name").GetString());
        Assert.Equal(126_000, Satang(branch, "net_satang"));

        var series = report.GetProperty("series").EnumerateArray().ToList();
        Assert.Equal(3, series.Count);
        Assert.Equal(126_000, series.Sum(period => Satang(period, "net_satang")));

        var monthly = await ReadAsync(reader, $"/staff/analytics/revenue?{Around(clinic)}&granularity=month", Token);
        Assert.Equal(126_000, monthly.GetProperty("series").EnumerateArray().Sum(period => Satang(period, "net_satang")));
    }

    [Fact]
    public async Task A_branch_bound_reader_adds_up_only_their_own_branch()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using (var cashier = fixture.Cashier(mine))
        {
            await PayAsync(cashier, await BillingScenario.RaiseIdAsync(cashier, mine, Token), 146_000, "promptpay", Token);
        }

        using (var cashier = fixture.Cashier(theirs))
        {
            await PayAsync(cashier, await BillingScenario.RaiseIdAsync(cashier, theirs, Token), 50_000, "cash", Token);
        }

        using var reader = fixture.Reader(mine, Scopes.Analytics.Read);
        var range = $"from={Today.AddDays(-1):yyyy-MM-dd}&to={Today.AddDays(1):yyyy-MM-dd}";
        var report = await ReadAsync(reader, $"/staff/analytics/revenue?{range}", Token);

        Assert.Equal(146_000, Satang(report.GetProperty("totals"), "collected_satang"));
        var branch = Assert.Single(report.GetProperty("by_branch").EnumerateArray());
        Assert.Equal(mine.BranchId, branch.GetProperty("branch_id").GetString());
    }

    [Fact]
    public async Task Service_revenue_shares_out_what_each_bill_kept_and_sets_medicine_beside_the_service()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        // ฿1,000 listed, ฿100 off, paid ฿900, ฿90 refunded: the bill kept ฿810, 81% of list.
        var bill = await RaiseAsync(
            cashier,
            clinic,
            Token,
            new { kind = "service", description = "ตรวจสุขภาพ", quantity = 1, unit_price_satang = 60_000, source_id = clinic.ServiceId },
            new { kind = "medicine", description = "ยาปฏิชีวนะ", quantity = 2, unit_price_satang = 10_000 },
            new { kind = "other", description = "ปลอกคอ", quantity = 1, unit_price_satang = 20_000 });

        var discount = await cashier.PostAsJsonAsync(
            $"/staff/invoices/{bill}/discounts",
            new { amount_satang = 10_000, reason = "ลูกค้าประจำ", approved_by_staff_id = clinic.StaffId },
            Token);
        discount.EnsureSuccessStatusCode();

        await PayAsync(cashier, bill, 90_000, "card", Token);
        await RefundAsync(cashier, bill, 9_000, "cash", Token);

        // The same service on a bill that was voided earns nothing.
        var voided = await RaiseAsync(
            cashier,
            clinic,
            Token,
            new { kind = "service", description = "ตรวจสุขภาพ", quantity = 1, unit_price_satang = 60_000, source_id = clinic.ServiceId });
        await VoidAsync(cashier, voided, Token);

        using var reader = fixture.Reader(clinic, Scopes.Analytics.Read);
        var report = await ReadAsync(reader, $"/staff/analytics/services?{Around(clinic)}", Token);

        var service = Assert.Single(report.GetProperty("services").EnumerateArray());
        Assert.Equal(clinic.ServiceId, service.GetProperty("service_id").GetString());
        Assert.Equal("ตรวจสุขภาพ", service.GetProperty("name").GetString());
        Assert.Equal(1, Int(service, "quantity"));
        Assert.Equal(1, Int(service, "invoices"));
        Assert.Equal(60_000, Satang(service, "billed_satang"));
        Assert.Equal(48_600, Satang(service, "revenue_satang"));
        Assert.Equal(16_200, Satang(service, "medicine_satang"));
        Assert.Equal(32_400, Satang(service, "revenue_excluding_medicine_satang"));

        var kinds = report.GetProperty("by_kind").EnumerateArray()
            .ToDictionary(row => row.GetProperty("kind").GetString()!, row => Satang(row, "revenue_satang"));

        Assert.Equal(48_600, kinds["service"]);
        Assert.Equal(16_200, kinds["medicine"]);
        Assert.Equal(16_200, kinds["other"]);
        Assert.Equal(0, kinds["treatment"]);

        // Every satang the bill kept is somewhere in the split.
        Assert.Equal(81_000, kinds.Values.Sum());
    }

    [Fact]
    public async Task Retention_tells_new_customers_from_returning_ones_and_follows_each_cohort()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (newcomer, newcomerPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var thisMonth = new DateOnly(Today.Year, Today.Month, 1);
        var twoMonthsAgo = thisMonth.AddMonths(-2);

        // The regular first came two months ago. No endpoint backdates a bill, so that visit is seeded.
        await SeedPastBillAsync(clinic, twoMonthsAgo.AddDays(9));

        using var cashier = fixture.Cashier(clinic);
        await BillingScenario.RaiseIdAsync(cashier, clinic, Token);
        await BillingScenario.RaiseAsync(cashier, clinic, Token, new
        {
            customer_id = newcomer,
            pet_id = newcomerPet,
            branch_id = clinic.BranchId,
            lines = new object[] { new { kind = "service", description = "ตรวจ", quantity = 1, unit_price_satang = 30_000 } },
        });

        using var reader = fixture.Reader(clinic, Scopes.Analytics.Read);
        var report = await ReadAsync(
            reader,
            $"/staff/analytics/retention?from={twoMonthsAgo:yyyy-MM-dd}&to={Today:yyyy-MM-dd}&branch={clinic.BranchId}",
            Token);

        var months = report.GetProperty("months").EnumerateArray().ToList();
        Assert.Equal(3, months.Count);
        Assert.Equal((1, 1, 0), Split(months[0]));
        Assert.Equal((0, 0, 0), Split(months[1]));
        Assert.Equal((2, 1, 1), Split(months[2]));

        var cohorts = report.GetProperty("cohorts").EnumerateArray().ToList();
        Assert.Equal(twoMonthsAgo.ToString("yyyy-MM-dd"), cohorts[0].GetProperty("cohort").GetString());
        Assert.Equal(1, Int(cohorts[0], "customers"));
        Assert.Equal([1, 0, 1], cohorts[0].GetProperty("retained").EnumerateArray().Select(count => count.GetInt32()));
        Assert.Equal(1m, cohorts[0].GetProperty("retention_rates")[2].GetDecimal());
        Assert.Equal([1], cohorts[2].GetProperty("retained").EnumerateArray().Select(count => count.GetInt32()));
    }

    [Fact]
    public async Task Staff_performance_credits_cases_and_their_money_to_the_vet_and_the_takings_to_the_cashier()
    {
        var open = await ConsultationScenario.OpenCaseAsync(fixture, Token);
        var clinic = open.Clinic;
        using var vet = fixture.Vet(clinic);

        await TreatAsync(vet, open.ConsultationId, new { name = "ตรวจสุขภาพ", quantity = 1, unit_price_satang = 70_000, service_id = clinic.ServiceId });
        await TreatAsync(vet, open.ConsultationId, new { name = "ทำแผล", quantity = 1, unit_price_satang = 30_000 });
        await ConsultationScenario.DiagnoseAsync(vet, open.ConsultationId, Token);

        var closed = await ConsultationScenario.CloseAsync(vet, open.ConsultationId, Token);
        var invoiceId = closed.GetProperty("invoice").GetProperty("id").GetString()!;

        var cashierId = $"stf_cash_{Guid.NewGuid():N}"[..24];
        using var cashier = fixture.Authenticated(TestTokens.Staff(
            fixture, "usr_cash", cashierId, BillingScenario.CashierScopes, [clinic.BranchId]));

        await PayAsync(cashier, invoiceId, 100_000, "cash", Token);
        await RefundAsync(cashier, invoiceId, 10_000, "cash", Token);

        using var hr = fixture.Reader(clinic, Scopes.Analytics.Hr);
        var report = await ReadAsync(hr, $"/staff/analytics/staff-performance?{Around(clinic)}", Token);
        var staff = report.GetProperty("staff").EnumerateArray().ToList();

        var theVet = staff.Single(row => row.GetProperty("staff_id").GetString() == clinic.StaffId);
        Assert.Equal(1, Int(theVet, "cases"));
        Assert.Equal(2, Int(theVet, "treatments"));
        Assert.Equal(90_000, Satang(theVet, "attributed_revenue_satang"));
        Assert.Equal(0, Satang(theVet, "collected_satang"));
        Assert.True(theVet.GetProperty("average_case_minutes").GetDecimal() >= 0);

        var theCashier = staff.Single(row => row.GetProperty("staff_id").GetString() == cashierId);
        Assert.Equal(0, Int(theCashier, "cases"));
        Assert.Equal(100_000, Satang(theCashier, "collected_satang"));

        // The same bill seen from the service side: the catalogue treatment is a service line.
        using var analyst = fixture.Reader(clinic, Scopes.Analytics.Read);
        var services = await ReadAsync(analyst, $"/staff/analytics/services?{Around(clinic)}", Token);
        var service = Assert.Single(services.GetProperty("services").EnumerateArray());
        Assert.Equal(clinic.ServiceId, service.GetProperty("service_id").GetString());
        Assert.Equal(63_000, Satang(service, "revenue_satang"));
    }

    private static (int Active, int New, int Returning) Split(JsonElement month) =>
        (Int(month, "active"), Int(month, "new"), Int(month, "returning"));

    private static async Task TreatAsync(HttpClient vet, string consultationId, object body)
    {
        using var request = Idempotent($"/staff/consultations/{consultationId}/treatments", body);
        using var response = await vet.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();
    }

    private async Task SeedPastBillAsync(SeededClinic clinic, DateOnly day)
    {
        var issuedAt = new DateTimeOffset(day.Year, day.Month, day.Day, 12, 0, 0, TimeSpan.FromHours(7));
        var invoice = Invoice.Raise($"SEED-{Guid.NewGuid():N}"[..20], clinic.BranchId, clinic.CustomerId, clinic.StaffId, issuedAt, clinic.PetId);
        invoice.AddLine(InvoiceLineKind.Service, "ตรวจ", 1, 30_000);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(Token);
    }
}
