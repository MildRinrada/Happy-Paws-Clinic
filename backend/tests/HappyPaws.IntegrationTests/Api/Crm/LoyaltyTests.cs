using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.CrmScenario;

namespace HappyPaws.IntegrationTests.Api.Crm;

/// <summary>
/// <c>GET /staff/loyalty/{customer_id}</c>, "แต้มสะสม ระดับสมาชิก และประวัติแลก", and the points that
/// move with a bill's money: earned when it is paid, taken back on refund, spent as a discount and
/// given back by a void.
/// </summary>
[Collection(nameof(ApiCollection))]
public class LoyaltyTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Reading_an_account_needs_crm_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var response = await cashier.GetAsync($"/staff/loyalty/{clinic.CustomerId}", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_customer_is_not_found()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic, Scopes.Crm.Read);

        var response = await desk.GetAsync("/staff/loyalty/cus_does_not_exist", Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_new_customer_is_a_member_with_no_points()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic, Scopes.Crm.Read);

        var account = await LoyaltyAsync(desk, clinic.CustomerId, Token);

        Assert.Equal(0, account.GetProperty("points_balance").GetInt32());
        Assert.Equal("member", account.GetProperty("tier").GetString());
        Assert.Equal("silver", account.GetProperty("next_tier").GetProperty("tier").GetString());
        Assert.Equal(10_000, account.GetProperty("rules").GetProperty("satang_per_point").GetInt64());
    }

    [Fact]
    public async Task Points_are_earned_only_when_the_bill_is_paid_in_full()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var invoiceId = await RaiseIdAsync(desk, clinic, Token);

        (await PayAsync(desk, invoiceId, 46_000, Token)).EnsureSuccessStatusCode();
        Assert.Equal(0, (await LoyaltyAsync(desk, clinic.CustomerId, Token)).GetProperty("points_balance").GetInt32());

        (await PayAsync(desk, invoiceId, 100_000, Token)).EnsureSuccessStatusCode();
        var account = await LoyaltyAsync(desk, clinic.CustomerId, Token);

        // ฿1,460 paid: 14 points, one row, pointing at the bill.
        Assert.Equal(14, account.GetProperty("points_balance").GetInt32());
        Assert.Equal(14, account.GetProperty("lifetime_points").GetInt32());
        var earn = Assert.Single(account.GetProperty("history").EnumerateArray());
        Assert.Equal("earn", earn.GetProperty("kind").GetString());
        Assert.Equal(invoiceId, earn.GetProperty("invoice_id").GetString());
        Assert.StartsWith("INV-", earn.GetProperty("invoice_number").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_refund_takes_back_the_points_for_the_money_returned()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var invoiceId = await RaiseIdAsync(desk, clinic, Token);
        (await PayAsync(desk, invoiceId, 146_000, Token)).EnsureSuccessStatusCode();

        await RefundAsync(desk, invoiceId, 50_000);
        Assert.Equal(9, (await LoyaltyAsync(desk, clinic.CustomerId, Token)).GetProperty("points_balance").GetInt32());

        await RefundAsync(desk, invoiceId, 96_000);
        var account = await LoyaltyAsync(desk, clinic.CustomerId, Token);
        Assert.Equal(0, account.GetProperty("points_balance").GetInt32());
        Assert.Equal(2, account.GetProperty("history").EnumerateArray().Count(row => row.GetProperty("kind").GetString() == "reverse"));
    }

    [Fact]
    public async Task Points_are_spent_as_a_discount_and_a_void_gives_them_back()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var paid = await RaiseIdAsync(desk, clinic, Token);
        (await PayAsync(desk, paid, 146_000, Token)).EnsureSuccessStatusCode();

        var next = await RaiseIdAsync(desk, clinic, Token);
        var response = await DiscountAsync(desk, next, new { points = 10 }, Token);

        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(1_000, invoice.GetProperty("discount_satang").GetInt64());
        Assert.Equal(4, (await LoyaltyAsync(desk, clinic.CustomerId, Token)).GetProperty("points_balance").GetInt32());

        (await desk.PostAsJsonAsync($"/staff/invoices/{next}/void", new { reason = "ออกบิลซ้ำ" }, Token)).EnsureSuccessStatusCode();

        var account = await LoyaltyAsync(desk, clinic.CustomerId, Token);
        Assert.Equal(14, account.GetProperty("points_balance").GetInt32());
        Assert.Contains(account.GetProperty("history").EnumerateArray(), row => row.GetProperty("kind").GetString() == "restore");
    }

    [Fact]
    public async Task Spending_more_points_than_the_customer_has_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var invoiceId = await RaiseIdAsync(desk, clinic, Token);

        var response = await DiscountAsync(desk, invoiceId, new { points = 1 }, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("insufficient_points", await ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task Points_earned_at_one_branch_are_seen_by_staff_of_another()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var invoiceId = await RaiseIdAsync(desk, clinic, Token);
        (await PayAsync(desk, invoiceId, 146_000, Token)).EnsureSuccessStatusCode();

        // Customers belong to the clinic, not a branch, the same as GET /staff/customers/{id}.
        using var otherBranch = fixture.CrmDesk(elsewhere, Scopes.Crm.Read);
        var account = await LoyaltyAsync(otherBranch, clinic.CustomerId, Token);

        Assert.Equal(14, account.GetProperty("points_balance").GetInt32());
    }

    private static async Task RefundAsync(HttpClient desk, string invoiceId, long amountSatang)
    {
        using var request = BookingScenario.Idempotent(
            $"/staff/invoices/{invoiceId}/refunds", new { amount_satang = amountSatang, reason = "คืนเงินบางส่วน" });

        (await desk.SendAsync(request, Token)).EnsureSuccessStatusCode();
    }
}
