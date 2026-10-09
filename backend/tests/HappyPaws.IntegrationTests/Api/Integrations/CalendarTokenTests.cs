using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// <c>POST /staff/integrations/calendar/token</c> (<c>docs/contract-extensions.md § 22</c>): a new
/// link for oneself, and the old one refused from then on.
/// </summary>
[Collection(nameof(IntegrationsCollection))]
public class CalendarTokenTests(IntegrationsFixture fixture)
{
    private const string Feed = "/staff/integrations/calendar/ics";
    private const string Rotate = "/staff/integrations/calendar/token";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_new_link_replaces_the_old_one_which_is_refused_from_then_on()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = Signed(clinic);
        using var calendarApp = fixture.CreateClient();

        var old = (await LinkAsync(vet)).GetProperty("token").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await calendarApp.GetAsync($"{Feed}?token={old}", Token)).StatusCode);

        var response = await vet.PostAsync(Rotate, null, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        var link = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var fresh = link.GetProperty("token").GetString()!;
        Assert.StartsWith("hpcal_", fresh, StringComparison.Ordinal);
        Assert.NotEqual(old, fresh);
        Assert.Equal($"{Feed}?token={fresh}", link.GetProperty("path").GetString());
        Assert.True(link.GetProperty("can_view_branch").GetBoolean());

        var refused = await calendarApp.GetAsync($"{Feed}?token={old}", Token);
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        Assert.Equal("unauthenticated", await IntegrationsScenario.ErrorCodeAsync(refused, Token));
        Assert.Equal(HttpStatusCode.OK, (await calendarApp.GetAsync($"{Feed}?token={fresh}", Token)).StatusCode);

        // The settings page shows the new link from now on.
        Assert.Equal(fresh, (await LinkAsync(vet)).GetProperty("token").GetString());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var stored = await db.Set<CalendarFeed>().AsNoTracking().SingleAsync(feed => feed.StaffId == clinic.StaffId, Token);
        Assert.Equal(ApiKey.Hash(fresh), stored.TokenHash);
        var audit = await db.Set<AuditEntry>().AsNoTracking()
            .SingleAsync(entry => entry.Action == AuditActions.CalendarFeedRotated && entry.ResourceId == stored.Id, Token);
        Assert.DoesNotContain(fresh, audit.DetailsJson ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task With_no_link_yet_it_makes_the_first_one()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var colleague = await IdentitySeed.StaffAsync(fixture, [Scopes.Queue.Read], withTwoFactor: false, branches: [clinic.BranchId], cancellationToken: Token);
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, colleague.UserId, colleague.StaffId, [Scopes.Queue.Read], [clinic.BranchId]));

        var response = await client.PostAsync(Rotate, null, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.False(link.GetProperty("can_view_branch").GetBoolean());

        using var calendarApp = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await calendarApp.GetAsync($"{Feed}?token={link.GetProperty("token").GetString()}", Token)).StatusCode);
    }

    [Fact]
    public async Task Only_a_signed_in_person_may_replace_their_link()
    {
        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync(Rotate, null, Token)).StatusCode);

        // An API key has no calendar of its own.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        string[] scopes = [Scopes.Integration.Manage, Scopes.Customer.Read];
        var staff = await IdentitySeed.StaffAsync(fixture, scopes, withTwoFactor: true, branches: [clinic.BranchId], cancellationToken: Token);
        using var creator = fixture.Authenticated(TestTokens.Staff(fixture, staff.UserId, staff.StaffId, scopes, [clinic.BranchId]));
        var created = await creator.PostAsJsonAsync("/staff/api-keys", new { name = "x", scopes = new[] { Scopes.Customer.Read } }, Token);
        created.EnsureSuccessStatusCode();
        using var key = fixture.Authenticated((await created.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("key").GetString()!);

        Assert.Equal(HttpStatusCode.Forbidden, (await key.PostAsync(Rotate, null, Token)).StatusCode);
    }

    private HttpClient Signed(SeededClinic clinic) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Appointment.Read, Scopes.Appointment.Write], [clinic.BranchId]));

    private async Task<JsonElement> LinkAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Feed);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }
}
