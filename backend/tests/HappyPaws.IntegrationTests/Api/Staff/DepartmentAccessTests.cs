using System.Net;
using System.Net.Http.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Staff;

/// <summary>
/// The staff-side extensions of § 10 answer only to the scopes the rest of the staff module uses.
/// </summary>
[Collection(nameof(ApiCollection))]
public class DepartmentAccessTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Departments_need_staff_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var queueOnly = fixture.StaffOf(clinic, Scopes.Queue.Read);

        var response = await queueOnly.GetAsync("/staff/departments", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_public_profile_needs_staff_manage_to_change()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.StaffOf(clinic, Scopes.Staff.Read);

        var response = await reader.PutAsJsonAsync(
            $"/staff/members/{clinic.StaffId}/public-profile", new { show_on_website = true }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_public_team_page_needs_no_sign_in()
    {
        using var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync("/team", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
