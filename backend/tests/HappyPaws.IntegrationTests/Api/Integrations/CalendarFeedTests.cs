using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Staff;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// <c>GET /staff/integrations/calendar/ics</c>: a calendar app authenticates with the token in its
/// link; a signed-in person gets the link or the file; the feed shows when and where, never why.
/// </summary>
[Collection(nameof(IntegrationsCollection))]
public class CalendarFeedTests(IntegrationsFixture fixture)
{
    private const string Feed = "/staff/integrations/calendar/ics";
    private const string Symptom = "อาเจียนเป็นเลือดสองวัน";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Without_a_login_or_a_token_the_feed_is_refused()
    {
        using var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync(Feed, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthenticated", await IntegrationsScenario.ErrorCodeAsync(response, Token));
    }

    [Theory]
    [InlineData("hpcal_not-a-real-token")]
    [InlineData("hp_abcdefghij_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("anything")]
    public async Task A_token_that_is_not_a_link_is_refused(string token)
    {
        using var anonymous = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Feed}?token={token}", Token)).StatusCode);
    }

    [Fact]
    public async Task A_signed_in_staff_member_gets_one_stable_link_stored_as_a_hash()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = Signed(clinic);

        var first = await LinkAsync(vet);
        var second = await LinkAsync(vet);

        var token = first.GetProperty("token").GetString()!;
        Assert.StartsWith("hpcal_", token, StringComparison.Ordinal);
        Assert.Equal(token, second.GetProperty("token").GetString());
        Assert.Equal($"{Feed}?token={token}", first.GetProperty("path").GetString());
        Assert.True(first.GetProperty("can_view_branch").GetBoolean());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var stored = await db.Set<CalendarFeed>().AsNoTracking().SingleAsync(feed => feed.StaffId == clinic.StaffId, Token);
        Assert.Equal(ApiKey.Hash(token), stored.TokenHash);
        Assert.DoesNotContain(token[6..], stored.TokenCiphertext, StringComparison.Ordinal);
        Assert.Equal(1, await db.Set<AuditEntry>().CountAsync(entry => entry.Action == AuditActions.CalendarFeedCreated && entry.ResourceId == stored.Id, Token));
    }

    [Fact]
    public async Task A_calendar_app_gets_the_appointments_with_no_reason_and_no_owner()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var appointmentId = await BookWithReasonAsync(clinic);

        using var vet = Signed(clinic);
        var token = (await LinkAsync(vet)).GetProperty("token").GetString()!;

        using var calendarApp = fixture.CreateClient();
        var response = await calendarApp.GetAsync($"{Feed}?token={token}", Token);
        var ics = await response.Content.ReadAsStringAsync(Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/calendar", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("BEGIN:VCALENDAR", ics, StringComparison.Ordinal);
        Assert.Contains($"UID:{appointmentId}@", ics, StringComparison.Ordinal);
        Assert.Contains("ลูน่า", Unfold(ics), StringComparison.Ordinal);
        Assert.Contains("ตรวจสุขภาพ", Unfold(ics), StringComparison.Ordinal);

        Assert.DoesNotContain(Symptom, Unfold(ics), StringComparison.Ordinal);
        Assert.DoesNotContain("คุณลูกค้า", Unfold(ics), StringComparison.Ordinal);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.NotNull((await db.Set<CalendarFeed>().AsNoTracking().SingleAsync(feed => feed.StaffId == clinic.StaffId, Token)).LastUsedAt);
    }

    [Fact]
    public async Task My_view_holds_only_my_appointments_and_the_branch_view_needs_appointment_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var vetsOwn = await BookWithReasonAsync(clinic);

        // A colleague at the same branch who holds no appointment scope at all.
        var colleague = await IdentitySeed.StaffAsync(fixture, [Scopes.Queue.Read], withTwoFactor: false, branches: [clinic.BranchId], cancellationToken: Token);
        using var colleagueClient = fixture.Authenticated(TestTokens.Staff(fixture, colleague.UserId, colleague.StaffId, [Scopes.Queue.Read], [clinic.BranchId]));

        var link = await LinkAsync(colleagueClient);
        Assert.False(link.GetProperty("can_view_branch").GetBoolean());
        var token = link.GetProperty("token").GetString()!;

        using var calendarApp = fixture.CreateClient();
        var mine = await (await calendarApp.GetAsync($"{Feed}?token={token}", Token)).Content.ReadAsStringAsync(Token);
        Assert.DoesNotContain(vetsOwn, mine, StringComparison.Ordinal);

        var branch = await calendarApp.GetAsync($"{Feed}?token={token}&view=branch", Token);
        Assert.Equal(HttpStatusCode.Forbidden, branch.StatusCode);

        // The vet holds appointment:read, so the branch view is theirs to see.
        using var vet = Signed(clinic);
        var vetToken = (await LinkAsync(vet)).GetProperty("token").GetString()!;
        var vetBranch = await calendarApp.GetAsync($"{Feed}?token={vetToken}&view=branch", Token);
        Assert.Equal(HttpStatusCode.OK, vetBranch.StatusCode);
        Assert.Contains(vetsOwn, await vetBranch.Content.ReadAsStringAsync(Token), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_deactivated_staff_members_link_stops_working()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = Signed(clinic);
        var token = (await LinkAsync(vet)).GetProperty("token").GetString()!;

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var membership = await db.Set<StaffMembership>().SingleAsync(row => row.Id == clinic.StaffId, Token);
            membership.Deactivate();
            await db.SaveChangesAsync(Token);
        }

        using var calendarApp = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await calendarApp.GetAsync($"{Feed}?token={token}", Token)).StatusCode);
    }

    [Fact]
    public async Task Signed_in_the_feed_downloads_as_a_file_and_an_unknown_view_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await BookWithReasonAsync(clinic);
        using var vet = Signed(clinic);

        var file = await vet.GetAsync(Feed, Token);
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("text/calendar", file.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", file.Content.Headers.ContentDisposition?.DispositionType);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await vet.GetAsync($"{Feed}?view=everyone", Token)).StatusCode);
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

    private async Task<string> BookWithReasonAsync(SeededClinic clinic)
    {
        using var staff = fixture.StaffOf(clinic);
        using var request = Idempotent("/staff/appointments", new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            starts_at = ClinicSeed.SlotAt(5),
            branch_id = clinic.BranchId,
            staff_id = clinic.StaffId,
            reason = Symptom,
        });

        var response = await staff.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!;
    }

    /// <summary>Undoes RFC 5545 line folding, so a search does not miss text split across lines.</summary>
    private static string Unfold(string ics) => ics.Replace("\r\n ", string.Empty, StringComparison.Ordinal);
}
