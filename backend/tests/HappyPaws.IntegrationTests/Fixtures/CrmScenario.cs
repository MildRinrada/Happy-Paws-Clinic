using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Loyalty, packages and promotions: a client holding the CRM scopes and the cash desk's, and the
/// requests the CRM tests share.
/// </summary>
public static class CrmScenario
{
    /// <summary>Both CRM scopes plus the cashier's, so one client can sell, bill, pay and look up.</summary>
    public static readonly string[] CrmDeskScopes =
    [
        Scopes.Crm.Read,
        Scopes.Crm.Manage,
        .. BillingScenario.CashierScopes,
    ];

    /// <summary>A staff client bound to the seeded clinic's branch.</summary>
    public static HttpClient CrmDesk(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? CrmDeskScopes : scopes,
            [clinic.BranchId]));

    /// <summary>A staff client bound to no branch: what an owner or admin holds.</summary>
    public static HttpClient UnboundCrmDesk(this ApiFixture fixture, SeededClinic clinic) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            CrmDeskScopes,
            []));

    public static object Package(
        SeededClinic clinic,
        int totalUses = 4,
        long priceSatang = 180_000,
        string? expiresOn = null,
        string? petId = null) => new
        {
            customer_id = clinic.CustomerId,
            pet_id = petId,
            name = "ตรวจสุขภาพ 4 ครั้ง",
            service_ids = new[] { clinic.ServiceId },
            total_uses = totalUses,
            price_satang = priceSatang,
            expires_on = expiresOn,
            branch_id = clinic.BranchId,
        };

    public static async Task<HttpResponseMessage> PostPackageAsync(
        HttpClient staff,
        object body,
        CancellationToken cancellationToken,
        string? key = null)
    {
        using var request = Idempotent("/staff/packages", body, key);

        return await staff.SendAsync(request, cancellationToken);
    }

    /// <summary>Sells a package and returns <c>{ package, invoice }</c>.</summary>
    public static async Task<JsonElement> SellAsync(
        HttpClient staff,
        SeededClinic clinic,
        CancellationToken cancellationToken,
        object? body = null)
    {
        var response = await PostPackageAsync(staff, body ?? Package(clinic), cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    /// <summary>Sells a package and pays its bill in full, so it is active. Returns the package id.</summary>
    public static async Task<string> SellPaidAsync(
        HttpClient staff,
        SeededClinic clinic,
        CancellationToken cancellationToken,
        object? body = null)
    {
        var sale = await SellAsync(staff, clinic, cancellationToken, body);
        var invoice = sale.GetProperty("invoice");

        var paid = await BillingScenario.PayAsync(
            staff,
            invoice.GetProperty("id").GetString()!,
            invoice.GetProperty("balance_satang").GetInt64(),
            cancellationToken);
        paid.EnsureSuccessStatusCode();

        return sale.GetProperty("package").GetProperty("id").GetString()!;
    }

    /// <summary>A counter bill with one catalogue-service line, which a package can cover.</summary>
    public static object ServiceBill(SeededClinic clinic, int quantity = 1, long unitPriceSatang = 50_000, bool withPet = true) => new
    {
        customer_id = clinic.CustomerId,
        pet_id = withPet ? clinic.PetId : null,
        branch_id = clinic.BranchId,
        lines = new object[]
        {
            new { kind = "service", description = "ตรวจสุขภาพ", quantity, unit_price_satang = unitPriceSatang, source_id = clinic.ServiceId },
            new { kind = "other", description = "ค่าอุปกรณ์", quantity = 1, unit_price_satang = 20_000 },
        },
    };

    public static async Task<JsonElement> LoyaltyAsync(HttpClient staff, string customerId, CancellationToken cancellationToken) =>
        await staff.GetFromJsonAsync<JsonElement>($"/staff/loyalty/{customerId}", cancellationToken);

    public static async Task<HttpResponseMessage> DiscountAsync(
        HttpClient staff,
        string invoiceId,
        object body,
        CancellationToken cancellationToken) =>
        await staff.PostAsJsonAsync($"/staff/invoices/{invoiceId}/discounts", body, cancellationToken);

    public static async Task<JsonElement> CreateCouponAsync(
        HttpClient staff,
        object body,
        CancellationToken cancellationToken)
    {
        var response = await staff.PostAsJsonAsync("/staff/coupons", body, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    /// <summary>A code unique to one test, since the suite shares one database.</summary>
    public static string UniqueCode(string prefix = "T") => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    public static async Task<string> ErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return body.GetProperty("error").GetProperty("code").GetString()!;
    }
}
