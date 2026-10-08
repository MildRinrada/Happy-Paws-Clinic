using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// What the statistics and analytics tests share: a reader holding the report scopes, the clinic's
/// today, and the money movements the revenue figures are built from.
/// <para>
/// Every report test narrows to the branch its own <see cref="ClinicSeed"/> created. The suite
/// shares one database, so a figure taken across all branches would include every other test's
/// bills; one branch per test is what makes an exact assertion possible.
/// </para>
/// </summary>
public static class AnalyticsScenario
{
    /// <summary>Every scope the reports use.</summary>
    public static readonly string[] ReportScopes =
    [
        Scopes.Dashboard.Read,
        Scopes.Stats.Read,
        Scopes.Analytics.Read,
        Scopes.Analytics.Hr,
    ];

    /// <summary>A reader bound to the clinic's branch, as the seeded vet.</summary>
    public static HttpClient Reader(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? ReportScopes : scopes,
            [clinic.BranchId]));

    /// <summary>The clinic's today, the way the API works it out.</summary>
    public static DateOnly Today => DateOnly.Parse(ClinicSeed.ClinicDate(DateTimeOffset.UtcNow));

    /// <summary>
    /// <c>from</c>, <c>to</c> and <c>branch</c> for a span around today. A day either side, so a
    /// test that starts at 23:59 in Bangkok still counts what it created a minute later.
    /// </summary>
    public static string Around(SeededClinic clinic, int daysBack = 1, int daysAhead = 1) =>
        $"from={Today.AddDays(-daysBack):yyyy-MM-dd}&to={Today.AddDays(daysAhead):yyyy-MM-dd}&branch={clinic.BranchId}";

    /// <summary>GETs a report and fails the test at once if it was refused.</summary>
    public static async Task<JsonElement> ReadAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(path, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"{path} answered {(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>A bill with the given lines at the clinic's branch, raised through the API.</summary>
    public static async Task<string> RaiseAsync(
        HttpClient cashier,
        SeededClinic clinic,
        CancellationToken cancellationToken,
        params object[] lines)
    {
        var invoice = await BillingScenario.RaiseAsync(cashier, clinic, cancellationToken, new
        {
            customer_id = clinic.CustomerId,
            pet_id = clinic.PetId,
            branch_id = clinic.BranchId,
            lines,
        });

        return invoice.GetProperty("id").GetString()!;
    }

    /// <summary>Takes one payment and fails the test if it was refused.</summary>
    public static async Task PayAsync(
        HttpClient cashier,
        string invoiceId,
        long amountSatang,
        string method,
        CancellationToken cancellationToken)
    {
        using var response = await BillingScenario.PayAsync(
            cashier, invoiceId, amountSatang, cancellationToken, method, method == "cash" ? null : "REF-1");

        response.EnsureSuccessStatusCode();
    }

    /// <summary>Pays out one refund and fails the test if it was refused.</summary>
    public static async Task RefundAsync(
        HttpClient cashier,
        string invoiceId,
        long amountSatang,
        string method,
        CancellationToken cancellationToken)
    {
        using var request = Idempotent(
            $"/staff/invoices/{invoiceId}/refunds",
            new { amount_satang = amountSatang, reason = "คืนเงินบางส่วน", method });

        using var response = await cashier.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Voids a bill that has taken no money.</summary>
    public static async Task VoidAsync(HttpClient cashier, string invoiceId, CancellationToken cancellationToken)
    {
        using var response = await cashier.PostAsJsonAsync(
            $"/staff/invoices/{invoiceId}/void", new { reason = "ออกผิดใบ" }, cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
