using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;

namespace HappyPaws.IntegrationTests.Api.Billing;

/// <summary>
/// The refusals: what a bill will not do, and which status code says so.
/// <para>
/// These are the tests that matter most in this module. Every one of them is a way the clinic's
/// books could quietly stop adding up.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class InvoiceStateTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_bill_refuses_more_money_than_it_asked_for()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);

        using var response = await PayAsync(cashier, invoiceId, 200_000, Token);

        // 409, not 422, and that is the platform's convention rather than a billing choice: a
        // request whose *shape* is wrong is 422 from the validator, while a request the aggregate
        // refuses given the state it is in is 409 carrying the domain's own code. ฿2,000 is a
        // perfectly valid amount — just not against a bill that asked for ฿1,460.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("invalid_payment", error.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_settled_bill_takes_no_more_payments()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);
        (await PayAsync(cashier, invoiceId, 146_000, Token)).Dispose();

        using var again = await PayAsync(cashier, invoiceId, 1_000, Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task An_untouched_bill_can_be_voided_with_a_reason()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);

        var response = await cashier.PostAsJsonAsync(
            $"/staff/invoices/{invoiceId}/void", new { reason = "ออกบิลผิดคน" }, Token);

        response.EnsureSuccessStatusCode();

        var invoice = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        // The state the contract draws and no documented endpoint reaches — contract-extensions § 3.
        Assert.Equal("void", invoice.GetProperty("status").GetString());
        Assert.Equal("ออกบิลผิดคน", invoice.GetProperty("void_reason").GetString());
    }

    [Fact]
    public async Task A_paid_bill_is_corrected_by_a_refund_not_a_void()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);
        (await PayAsync(cashier, invoiceId, 146_000, Token)).Dispose();

        var voided = await cashier.PostAsJsonAsync(
            $"/staff/invoices/{invoiceId}/void", new { reason = "ยกเลิก" }, Token);

        // Voiding would erase a transaction that happened and leave the drawer out by ฿1,460.
        Assert.Equal(HttpStatusCode.Conflict, voided.StatusCode);

        using var refund = BookingScenario.Idempotent(
            $"/staff/invoices/{invoiceId}/refunds",
            new { amount_satang = 146_000, reason = "คิดค่าบริการผิด" });

        var refunded = await cashier.SendAsync(refund, Token);
        refunded.EnsureSuccessStatusCode();

        var invoice = await refunded.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("refunded", invoice.GetProperty("status").GetString());
        Assert.Equal(146_000, invoice.GetProperty("refunded_satang").GetInt64());
    }

    [Fact]
    public async Task A_refund_without_a_reason_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);
        (await PayAsync(cashier, invoiceId, 146_000, Token)).Dispose();

        using var request = BookingScenario.Idempotent(
            $"/staff/invoices/{invoiceId}/refunds", new { amount_satang = 10_000 });

        var response = await cashier.SendAsync(request, Token);

        // "ผูกเหตุผล" is the whole point of the row: years later, the difference between a goodwill
        // gesture and a disputed charge is nowhere else.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_bill_raised_at_another_branch_is_not_reachable()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var theirCashier = fixture.Cashier(theirs);
        var invoiceId = await RaiseIdAsync(theirCashier, theirs, Token);

        using var myCashier = fixture.Cashier(mine);

        var response = await myCashier.GetAsync($"/staff/invoices/{invoiceId}", Token);

        // The money goes into a different drawer and reconciles against a different day's takings.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_bill_for_an_unknown_customer_is_refused_by_name()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        using var request = BookingScenario.Idempotent("/staff/invoices", new
        {
            customer_id = "cus_nobody",
            branch_id = clinic.BranchId,
            lines = new object[]
            {
                new { description = "ค่าบริการ", quantity = 1, unit_price_satang = 10_000 },
            },
        });

        var response = await cashier.SendAsync(request, Token);

        // A foreign key would refuse this too, but as a constraint violation at commit — a 500 with
        // a Postgres message in it rather than a 404 naming the field.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_empty_bill_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        using var request = BookingScenario.Idempotent("/staff/invoices", new
        {
            customer_id = clinic.CustomerId,
            branch_id = clinic.BranchId,
            lines = Array.Empty<object>(),
        });

        var response = await cashier.SendAsync(request, Token);

        // It would total zero and be born already `paid` — a document saying a customer settled
        // something nobody charged them for.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "", Scopes.Invoice.Write)]
    [InlineData("GET", "", Scopes.Invoice.Write)]
    [InlineData("POST", "/discounts", Scopes.Invoice.Discount)]
    [InlineData("POST", "/payments", Scopes.Payment.Collect)]
    [InlineData("POST", "/refunds", Scopes.Payment.Refund)]
    [InlineData("POST", "/receipt", Scopes.Receipt.Issue)]
    [InlineData("POST", "/void", Scopes.Invoice.Write)]
    public async Task Every_billing_route_refuses_a_caller_without_its_scope(
        string method,
        string suffix,
        string required)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        // Everything the cashier holds except the one this route needs.
        using var underscoped = fixture.Cashier(
            clinic, [.. CashierScopes.Where(scope => scope != required)]);

        var path = suffix.Length == 0 && method == "POST"
            ? "/staff/invoices"
            : $"/staff/invoices/inv_whatever{suffix}";

        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        }

        var response = await underscoped.SendAsync(request, Token);

        // 403 before anything else is evaluated — the id does not exist, and a 404 here would mean
        // the scope was checked after the lookup.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
