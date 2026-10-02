using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Billing;

/// <summary>
/// The cash desk end to end: raise a bill, discount it, take the money in two tenders, print the
/// receipt — and find it again afterwards from both sides of the counter.
/// </summary>
[Collection(nameof(ApiCollection))]
public class InvoiceFlowTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_new_bill_is_numbered_open_and_totals_its_lines()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoice = await RaiseAsync(cashier, clinic, Token);

        Assert.Equal("open", invoice.GetProperty("status").GetString());
        Assert.Equal(146_000, invoice.GetProperty("total_satang").GetInt64());
        Assert.Equal(146_000, invoice.GetProperty("balance_satang").GetInt64());
        Assert.Equal(0, invoice.GetProperty("paid_satang").GetInt64());
        Assert.StartsWith("INV-", invoice.GetProperty("number").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_percentage_discount_needs_an_approver_and_reduces_the_total()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);

        var response = await cashier.PostAsJsonAsync(
            $"/staff/invoices/{invoiceId}/discounts",
            new { percent = 10m, reason = "ลูกค้าประจำ", approved_by_staff_id = clinic.StaffId },
            Token);

        response.EnsureSuccessStatusCode();

        var invoice = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(14_600, invoice.GetProperty("discount_satang").GetInt64());
        Assert.Equal(131_400, invoice.GetProperty("total_satang").GetInt64());
        Assert.Equal(
            clinic.StaffId,
            invoice.GetProperty("discounts")[0].GetProperty("approved_by_staff_id").GetString());
    }

    [Fact]
    public async Task A_discount_without_an_approver_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);

        var response = await cashier.PostAsJsonAsync(
            $"/staff/invoices/{invoiceId}/discounts",
            new { amount_satang = 10_000, reason = "ลดให้" },
            Token);

        // "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ" — the contract's own words, and half of it is missing.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Paying_in_two_tenders_walks_the_bill_to_paid()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);

        using var first = await PayAsync(cashier, invoiceId, 40_000, Token);
        first.EnsureSuccessStatusCode();

        var partial = await first.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("partially_paid", partial.GetProperty("status").GetString());
        Assert.Equal(106_000, partial.GetProperty("balance_satang").GetInt64());

        using var second = await PayAsync(cashier, invoiceId, 106_000, Token, "card", "APPR-77213");
        second.EnsureSuccessStatusCode();

        var settled = await second.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("paid", settled.GetProperty("status").GetString());
        Assert.Equal(0, settled.GetProperty("balance_satang").GetInt64());
        Assert.Equal(2, settled.GetProperty("payments").GetArrayLength());
    }

    [Fact]
    public async Task A_receipt_is_issued_once_and_sent_to_the_owner()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);
        (await PayAsync(cashier, invoiceId, 146_000, Token)).Dispose();

        using var first = await cashier.PostAsJsonAsync(
            $"/staff/invoices/{invoiceId}/receipt",
            new { is_tax_invoice = true, tax_id = "0105536000021", legal_name = "บริษัท ทดสอบ จำกัด" },
            Token);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var issued = await first.Content.ReadFromJsonAsync<JsonElement>(Token);
        var receipt = issued.GetProperty("receipt");

        Assert.StartsWith("RCP-", receipt.GetProperty("number").GetString(), StringComparison.Ordinal);
        Assert.True(receipt.GetProperty("is_tax_invoice").GetBoolean());
        Assert.Equal(146_000, receipt.GetProperty("amount_satang").GetInt64());

        // "ส่งเข้า LINE" — through the consent gate, like every other outbound message.
        Assert.Contains(
            fixture.Notifications.SentTo(clinic.CustomerId),
            sent => sent.TemplateKey == "receipt.issued");

        using var again = await cashier.PostAsJsonAsync(
            $"/staff/invoices/{invoiceId}/receipt", new { }, Token);

        // A second original ใบกำกับภาษี is a tax problem. Asking again returns the first document.
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        var repeated = await again.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(
            receipt.GetProperty("number").GetString(),
            repeated.GetProperty("receipt").GetProperty("number").GetString());
    }

    [Fact]
    public async Task The_counter_can_find_todays_unpaid_bills()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var unpaid = await RaiseIdAsync(cashier, clinic, Token);
        var settled = await RaiseIdAsync(cashier, clinic, Token);
        (await PayAsync(cashier, settled, 146_000, Token)).Dispose();

        var response = await cashier.GetAsync(
            $"/staff/invoices?branch={clinic.BranchId}&status=open", Token);

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var ids = page.GetProperty("data").EnumerateArray()
            .Select(invoice => invoice.GetProperty("id").GetString())
            .ToList();

        Assert.Contains(unpaid, ids);
        Assert.DoesNotContain(settled, ids);
    }

    [Fact]
    public async Task A_listed_bill_carries_the_same_totals_as_the_one_that_is_read_directly()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await RaiseIdAsync(cashier, clinic, Token);

        await cashier.PostAsJsonAsync(
            $"/staff/invoices/{invoiceId}/discounts",
            new { amount_satang = 20_000, reason = "ลดให้", approved_by_staff_id = clinic.StaffId },
            Token);

        var response = await cashier.GetAsync($"/staff/invoices?customer_id={clinic.CustomerId}", Token);
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var listed = page.GetProperty("data").EnumerateArray()
            .Single(invoice => invoice.GetProperty("id").GetString() == invoiceId);

        // A list that loaded fewer children than the detail read would quote every discounted bill
        // at its undiscounted price — and would do it consistently enough to look correct.
        Assert.Equal(126_000, listed.GetProperty("total_satang").GetInt64());
        Assert.Equal(20_000, listed.GetProperty("discount_satang").GetInt64());
    }

    [Fact]
    public async Task A_customer_sees_their_own_bills_and_what_is_still_owed()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);
        using var customer = fixture.CustomerOf(clinic);

        var unpaid = await RaiseIdAsync(cashier, clinic, Token);
        var settled = await RaiseIdAsync(cashier, clinic, Token);
        (await PayAsync(cashier, settled, 146_000, Token)).Dispose();

        var history = await customer.GetFromJsonAsync<JsonElement>("/portal/invoices", Token);
        var owing = await customer.GetFromJsonAsync<JsonElement>("/portal/invoices?unpaid=true", Token);

        var all = history.GetProperty("data").EnumerateArray()
            .Select(invoice => invoice.GetProperty("id").GetString()).ToList();
        var outstanding = owing.GetProperty("data").EnumerateArray()
            .Select(invoice => invoice.GetProperty("id").GetString()).ToList();

        Assert.Contains(unpaid, all);
        Assert.Contains(settled, all);

        // "จ่ายค้างชำระ" is the list of what is owed, not the list of everything.
        Assert.Contains(unpaid, outstanding);
        Assert.DoesNotContain(settled, outstanding);
    }

    [Fact]
    public async Task A_customer_cannot_see_another_customers_bills()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);
        using var stranger = fixture.Authenticated(TestTokens.Customer(fixture, "usr_stranger", "cus_stranger"));

        await RaiseIdAsync(cashier, clinic, Token);

        var page = await stranger.GetFromJsonAsync<JsonElement>("/portal/invoices", Token);

        // Scoped by customer_id at the database, so the query cannot express someone else's bills.
        Assert.Empty(page.GetProperty("data").EnumerateArray());
    }
}
