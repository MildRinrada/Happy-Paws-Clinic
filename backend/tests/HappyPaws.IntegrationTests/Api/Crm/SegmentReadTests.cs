using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.CrmScenario;

namespace HappyPaws.IntegrationTests.Api.Crm;

/// <summary>
/// <c>docs/contract-extensions.md § 25</c>: <c>GET /staff/segments</c> and <c>GET /staff/segments/{id}</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class SegmentReadTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> CreateAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/staff/segments", body, Token);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static object ForBranch(SeededClinic clinic) =>
        new { name = $"ลูกค้าสาขา {Guid.NewGuid():N}"[..20], criteria = new { branch_id = clinic.BranchId } };

    [Fact]
    public async Task Reading_segments_needs_crm_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var segment = await CreateAsync(desk, ForBranch(clinic));
        using var cashier = fixture.Cashier(clinic);

        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/staff/segments", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync($"/staff/segments/{segment.GetProperty("id").GetString()}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await desk.GetAsync("/staff/segments/seg_does_not_exist", Token)).StatusCode);
    }

    [Fact]
    public async Task One_segment_is_counted_again_while_the_list_keeps_the_count_it_was_saved_with()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var segment = await CreateAsync(desk, ForBranch(clinic));
        var id = segment.GetProperty("id").GetString()!;
        Assert.Equal(0, segment.GetProperty("customer_count").GetInt32());

        // A bill at the branch is a visit: the customer now belongs to "visited this branch".
        await RaiseIdAsync(desk, clinic, Token);

        var fresh = await desk.GetFromJsonAsync<JsonElement>($"/staff/segments/{id}", Token);
        var list = await desk.GetFromJsonAsync<JsonElement>($"/staff/segments?branch={clinic.BranchId}", Token);
        var row = Assert.Single(list.GetProperty("data").EnumerateArray());

        Assert.Equal(1, fresh.GetProperty("customer_count").GetInt32());
        Assert.True(fresh.GetProperty("counted_at").GetDateTimeOffset() > segment.GetProperty("created_at").GetDateTimeOffset());
        Assert.Equal(clinic.BranchId, fresh.GetProperty("criteria").GetProperty("branch_id").GetString());
        Assert.Equal(id, row.GetProperty("id").GetString());
        Assert.Equal(0, row.GetProperty("customer_count").GetInt32());
        Assert.Equal(row.GetProperty("created_at").GetDateTimeOffset(), row.GetProperty("counted_at").GetDateTimeOffset());
    }

    [Fact]
    public async Task Branch_staff_see_only_their_branchs_segments()
    {
        var home = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.UnboundCrmDesk(home);
        var mine = (await CreateAsync(owner, ForBranch(home))).GetProperty("id").GetString();
        var theirs = (await CreateAsync(owner, ForBranch(other))).GetProperty("id").GetString();
        var wide = (await CreateAsync(owner, new { name = "ทุกสาขา", criteria = new { inactive_days = 3000 } })).GetProperty("id").GetString();
        using var desk = fixture.CrmDesk(home);

        var listed = (await desk.GetFromJsonAsync<JsonElement>("/staff/segments?limit=100", Token))
            .GetProperty("data").EnumerateArray().Select(row => row.GetProperty("id").GetString()).ToList();

        Assert.Contains(mine, listed);
        Assert.DoesNotContain(theirs, listed);
        Assert.DoesNotContain(wide, listed);
        Assert.Equal(HttpStatusCode.Forbidden, (await desk.GetAsync($"/staff/segments/{theirs}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await desk.GetAsync($"/staff/segments/{wide}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await desk.GetAsync($"/staff/segments?branch={other.BranchId}", Token)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/staff/segments/{wide}", Token)).StatusCode);
    }
}
