using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.CrmScenario;

namespace HappyPaws.IntegrationTests.Api.Crm;

/// <summary><c>docs/contract-extensions.md § 25</c>: <c>GET /staff/packages?customer_id=</c>.</summary>
[Collection(nameof(ApiCollection))]
public class PackageListTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Listing_needs_crm_read_a_customer_and_a_known_status()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);
        using var reader = fixture.CrmDesk(clinic, Scopes.Crm.Read);

        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync($"/staff/packages?customer_id={clinic.CustomerId}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await reader.GetAsync("/staff/packages", Token)).StatusCode);
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await reader.GetAsync($"/staff/packages?customer_id={clinic.CustomerId}&status=usable", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reader.GetAsync("/staff/packages?customer_id=cus_does_not_exist", Token)).StatusCode);
    }

    [Fact]
    public async Task A_customers_packages_are_listed_usable_first_and_filtered_by_status_and_branch()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var pending = (await SellAsync(desk, clinic, Token)).GetProperty("package").GetProperty("id").GetString();
        var active = await SellPaidAsync(desk, clinic, Token);

        var all = await desk.GetFromJsonAsync<JsonElement>($"/staff/packages?customer_id={clinic.CustomerId}", Token);
        var rows = all.GetProperty("data").EnumerateArray().ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(active, rows[0].GetProperty("id").GetString());
        Assert.Equal("active", rows[0].GetProperty("status").GetString());
        Assert.Equal(pending, rows[1].GetProperty("id").GetString());

        var onlyPending = await desk.GetFromJsonAsync<JsonElement>(
            $"/staff/packages?customer_id={clinic.CustomerId}&status=pending_payment", Token);
        Assert.Equal(pending, Assert.Single(onlyPending.GetProperty("data").EnumerateArray()).GetProperty("id").GetString());

        var otherBranch = await desk.GetFromJsonAsync<JsonElement>(
            $"/staff/packages?customer_id={clinic.CustomerId}&branch={elsewhere.BranchId}", Token);
        Assert.Empty(otherBranch.GetProperty("data").EnumerateArray());

        // Clinic-wide like the loyalty account: staff of another branch read the same packages.
        using var otherDesk = fixture.CrmDesk(elsewhere, Scopes.Crm.Read);
        var seen = await otherDesk.GetFromJsonAsync<JsonElement>($"/staff/packages?customer_id={clinic.CustomerId}", Token);
        Assert.Equal(2, seen.GetProperty("data").GetArrayLength());
    }
}
