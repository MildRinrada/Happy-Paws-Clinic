using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.CrmScenario;

namespace HappyPaws.IntegrationTests.Api.Crm;

/// <summary>
/// <c>POST /staff/coupons</c>, "โค้ดส่วนลด · จำกัดบริการ จำนวน และช่วงเวลา", and redeeming one at the
/// cash desk through <c>POST /staff/invoices/{id}/discounts</c> with <c>coupon_code</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class CouponTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Creating_a_coupon_needs_crm_manage()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var response = await cashier.PostAsJsonAsync("/staff/coupons", Coupon(clinic, UniqueCode()), Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_coupon_is_a_rate_or_an_amount_and_its_dates_must_be_in_order()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);

        var both = await desk.PostAsJsonAsync("/staff/coupons", new
        {
            name = "ทั้งสองแบบ", percent = 10, amount_satang = 5_000, branch_id = clinic.BranchId,
        }, Token);
        var backwards = await desk.PostAsJsonAsync("/staff/coupons", new
        {
            name = "วันกลับด้าน", percent = 10, branch_id = clinic.BranchId,
            valid_from = Today(5), valid_until = Today(1),
        }, Token);
        var badCode = await desk.PostAsJsonAsync("/staff/coupons", new
        {
            name = "โค้ดเว้นวรรค", code = "HAS SPACE", percent = 10, branch_id = clinic.BranchId,
        }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, both.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, backwards.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badCode.StatusCode);
    }

    [Fact]
    public async Task A_code_is_stored_in_upper_case_and_cannot_be_reused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var code = UniqueCode("S");

        var coupon = await CreateCouponAsync(desk, Coupon(clinic, code.ToLowerInvariant()), Token);
        var again = await desk.PostAsJsonAsync("/staff/coupons", Coupon(clinic, code), Token);

        Assert.Equal(code, coupon.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("coupon_code_taken", await ErrorCodeAsync(again, Token));
    }

    [Fact]
    public async Task Without_a_code_one_is_generated()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);

        var coupon = await CreateCouponAsync(desk, new { name = "สุ่มโค้ด", amount_satang = 10_000, branch_id = clinic.BranchId }, Token);

        Assert.Equal(8, coupon.GetProperty("code").GetString()!.Length);
    }

    [Fact]
    public async Task Staff_bound_to_a_branch_cannot_issue_a_coupon_for_every_branch_or_for_another_one()
    {
        var home = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(home);

        var everywhere = await desk.PostAsJsonAsync("/staff/coupons", new { name = "ทุกสาขา", percent = 5 }, Token);
        var elsewhere = await desk.PostAsJsonAsync("/staff/coupons", Coupon(other, UniqueCode()), Token);

        Assert.Equal(HttpStatusCode.Forbidden, everywhere.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, elsewhere.StatusCode);

        using var owner = fixture.UnboundCrmDesk(home);
        var clinicWide = await owner.PostAsJsonAsync("/staff/coupons", new { name = "ทุกสาขา", percent = 5 }, Token);
        Assert.Equal(HttpStatusCode.Created, clinicWide.StatusCode);
    }

    [Fact]
    public async Task A_coupon_for_one_service_discounts_only_that_services_lines_and_names_its_issuer_as_approver()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var code = UniqueCode("V");
        await CreateCouponAsync(desk, new
        {
            code, name = "ลดตรวจ 10%", percent = 10, service_ids = new[] { clinic.ServiceId }, branch_id = clinic.BranchId,
        }, Token);
        var bill = await RaiseAsync(desk, clinic, Token, ServiceBill(clinic));

        var response = await DiscountAsync(desk, bill.GetProperty("id").GetString()!, new { coupon_code = code.ToLowerInvariant() }, Token);

        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(5_000, invoice.GetProperty("discount_satang").GetInt64());
        var discount = Assert.Single(invoice.GetProperty("discounts").EnumerateArray());
        Assert.Equal(clinic.StaffId, discount.GetProperty("approved_by_staff_id").GetString());
        Assert.Contains(code, discount.GetProperty("reason").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_code_is_not_found_and_a_coupon_that_has_not_started_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var code = UniqueCode("F");
        await CreateCouponAsync(desk, new
        {
            code, name = "เดือนหน้า", percent = 10, branch_id = clinic.BranchId, valid_from = Today(30),
        }, Token);
        var invoiceId = await RaiseIdAsync(desk, clinic, Token);

        var unknown = await DiscountAsync(desk, invoiceId, new { coupon_code = "NOSUCHCODE" }, Token);
        var early = await DiscountAsync(desk, invoiceId, new { coupon_code = code }, Token);

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal("coupon_not_started", await ErrorCodeAsync(early, Token));
    }

    [Fact]
    public async Task A_coupon_cannot_be_used_twice_on_one_bill_or_past_its_per_customer_limit()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var code = UniqueCode("P");
        await CreateCouponAsync(desk, new
        {
            code, name = "ครั้งเดียวต่อคน", amount_satang = 10_000, per_customer_limit = 1, branch_id = clinic.BranchId,
        }, Token);
        var first = await RaiseIdAsync(desk, clinic, Token);
        var second = await RaiseIdAsync(desk, clinic, Token);

        (await DiscountAsync(desk, first, new { coupon_code = code }, Token)).EnsureSuccessStatusCode();
        var sameBill = await DiscountAsync(desk, first, new { coupon_code = code }, Token);
        var sameCustomer = await DiscountAsync(desk, second, new { coupon_code = code }, Token);

        Assert.Equal("coupon_not_applicable", await ErrorCodeAsync(sameBill, Token));
        Assert.Equal("coupon_customer_limit", await ErrorCodeAsync(sameCustomer, Token));
    }

    [Fact]
    public async Task Two_cashiers_redeeming_a_single_use_coupon_at_once_get_one_success()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var code = UniqueCode("ONE");
        await CreateCouponAsync(desk, new
        {
            code, name = "ใช้ได้ครั้งเดียว", amount_satang = 10_000, max_redemptions = 1, branch_id = clinic.BranchId,
        }, Token);
        var bills = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => RaiseIdAsync(desk, clinic, Token)));

        using var cashierA = fixture.CrmDesk(clinic);
        using var cashierB = fixture.CrmDesk(clinic);
        var responses = await Task.WhenAll(
            DiscountAsync(cashierA, bills[0], new { coupon_code = code }, Token),
            DiscountAsync(cashierB, bills[1], new { coupon_code = code }, Token));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        var refused = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("coupon_used_up", await ErrorCodeAsync(refused, Token));
    }

    [Fact]
    public async Task Voiding_the_bill_gives_the_coupon_use_back()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var code = UniqueCode("R");
        await CreateCouponAsync(desk, new
        {
            code, name = "คืนได้", amount_satang = 10_000, max_redemptions = 1, branch_id = clinic.BranchId,
        }, Token);
        var first = await RaiseIdAsync(desk, clinic, Token);
        (await DiscountAsync(desk, first, new { coupon_code = code }, Token)).EnsureSuccessStatusCode();

        (await desk.PostAsJsonAsync($"/staff/invoices/{first}/void", new { reason = "ออกบิลผิด" }, Token)).EnsureSuccessStatusCode();

        var second = await RaiseIdAsync(desk, clinic, Token);
        (await DiscountAsync(desk, second, new { coupon_code = code }, Token)).EnsureSuccessStatusCode();

        var history = (await LoyaltyAsync(desk, clinic.CustomerId, Token)).GetProperty("coupon_redemptions");
        Assert.Equal(2, history.GetArrayLength());
        Assert.Single(history.EnumerateArray(), row =>
            !row.TryGetProperty("released_at", out var released) || released.ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task A_discount_body_must_be_exactly_one_kind()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var invoiceId = await RaiseIdAsync(desk, clinic, Token);

        var two = await DiscountAsync(desk, invoiceId, new { coupon_code = "ANYCODE", points = 10 }, Token);
        var manualWithoutApprover = await DiscountAsync(desk, invoiceId, new { amount_satang = 1_000, reason = "x" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, two.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, manualWithoutApprover.StatusCode);
    }

    [Fact]
    public async Task Redeeming_a_coupon_needs_invoice_discount_and_the_bills_branch()
    {
        var home = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var otherDesk = fixture.CrmDesk(other);
        var otherBill = await RaiseIdAsync(otherDesk, other, Token);
        using var desk = fixture.CrmDesk(home);
        using var noDiscount = fixture.CrmDesk(home, Scopes.Crm.Manage, Scopes.Invoice.Write);

        var wrongBranch = await DiscountAsync(desk, otherBill, new { coupon_code = "ANYCODE" }, Token);
        var noScope = await DiscountAsync(noDiscount, otherBill, new { coupon_code = "ANYCODE" }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, wrongBranch.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, noScope.StatusCode);
    }

    private static object Coupon(SeededClinic clinic, string code) =>
        new { code, name = "ลด 10%", percent = 10, branch_id = clinic.BranchId };

    private static string Today(int plusDays) => ClinicSeed.ClinicDate(DateTimeOffset.UtcNow.AddDays(plusDays));
}
