using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// The cash desk: a client holding the cashier's scopes, and the requests the billing tests all
/// need.
/// </summary>
public static class BillingScenario
{
    /// <summary>What the contract gives <c>cashier</c>: <c>invoice:*, payment:*, receipt:issue</c>.</summary>
    public static readonly string[] CashierScopes =
    [
        Scopes.Invoice.Write,
        Scopes.Invoice.Discount,
        Scopes.Payment.Collect,
        Scopes.Payment.Refund,
        Scopes.Receipt.Issue,
    ];

    public static HttpClient Cashier(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? CashierScopes : scopes,
            [clinic.BranchId]));

    /// <summary>A ฿1,460 bill — the figure from the contract's own worked example.</summary>
    public static object Bill(SeededClinic clinic, string? consultationId = null) => new
    {
        customer_id = clinic.CustomerId,
        pet_id = clinic.PetId,
        branch_id = clinic.BranchId,
        consultation_id = consultationId,
        lines = new object[]
        {
            new { kind = "service", description = "ตรวจรักษาทั่วไป", quantity = 1, unit_price_satang = 45_000 },
            new { kind = "treatment", description = "ทำแผล", quantity = 1, unit_price_satang = 101_000 },
        },
    };

    /// <summary>Raises a bill through the API and returns the whole invoice document.</summary>
    public static async Task<JsonElement> RaiseAsync(
        HttpClient cashier,
        SeededClinic clinic,
        CancellationToken cancellationToken,
        object? body = null)
    {
        using var request = Idempotent("/staff/invoices", body ?? Bill(clinic));

        var response = await cashier.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    /// <summary>Raises a bill and returns just its id.</summary>
    public static async Task<string> RaiseIdAsync(
        HttpClient cashier,
        SeededClinic clinic,
        CancellationToken cancellationToken) =>
        (await RaiseAsync(cashier, clinic, cancellationToken)).GetProperty("id").GetString()!;

    /// <summary>One tender against a bill. Returns the response so a test can assert on the refusal.</summary>
    public static async Task<HttpResponseMessage> PayAsync(
        HttpClient cashier,
        string invoiceId,
        long amountSatang,
        CancellationToken cancellationToken,
        string method = "cash",
        string? reference = null)
    {
        // Awaited before the request is disposed. Returning the task and letting `using` run first
        // tears the content stream down underneath the in-process test server.
        using var request = Idempotent(
            $"/staff/invoices/{invoiceId}/payments",
            new { method, amount_satang = amountSatang, reference });

        return await cashier.SendAsync(request, cancellationToken);
    }

    /// <summary>Reads a bill back through <c>GET /staff/invoices/{id}</c>.</summary>
    public static async Task<JsonElement> ReadAsync(
        HttpClient cashier,
        string invoiceId,
        CancellationToken cancellationToken)
    {
        var response = await cashier.GetAsync($"/staff/invoices/{invoiceId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }
}
