using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Clinics;

/// <summary>
/// <c>PATCH /clinic/branches/{id}</c> (<c>docs/contract-extensions.md § 51</c>): what can change,
/// who may change it, and the one thing that stops an edit, a room the queue still uses.
/// <para>
/// On the shared instance: every test edits a branch of its own from <see cref="ClinicSeed"/>.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class BranchEditTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>An owner or admin with no branch list, which <c>BranchGuard</c> reads as any branch.</summary>
    private HttpClient Admin(params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: scopes.Length == 0 ? [Scopes.Settings.Read, Scopes.Settings.Write] : scopes));

    private HttpClient AdminOf(string branchId) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: [Scopes.Settings.Read, Scopes.Settings.Write], branches: [branchId]));

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response) =>
        (await BodyAsync(response)).GetProperty("error").GetProperty("code").GetString();

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string branchId, object body, uint? ifMatch = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/clinic/branches/{branchId}")
        {
            Content = JsonContent.Create(body),
        };

        if (ifMatch is { } version)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        }

        return client.SendAsync(request, Token);
    }

    private async Task<JsonElement> ListedAsync(string branchId)
    {
        using var anonymous = fixture.CreateClient();
        var branches = await anonymous.GetFromJsonAsync<JsonElement>("/clinic/branches", Token);

        return branches.GetProperty("data").EnumerateArray().Single(branch => branch.GetProperty("id").GetString() == branchId);
    }

    /// <summary>A walk-in ticket at the clinic's branch, put in <paramref name="roomId"/>.</summary>
    private async Task<string> TicketInRoomAsync(SeededClinic clinic, string roomId)
    {
        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            [Scopes.Queue.Read, Scopes.Queue.Write],
            [clinic.BranchId]));

        using var issue = Idempotent("/staff/queue/tickets", new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
        });

        var issued = await staff.SendAsync(issue, Token);
        issued.EnsureSuccessStatusCode();

        var ticketId = (await BodyAsync(issued)).GetProperty("id").GetString()!;

        (await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/assign", new { room_id = roomId }, Token))
            .EnsureSuccessStatusCode();

        return ticketId;
    }

    [Fact]
    public async Task Name_address_pin_and_rooms_change_and_the_code_stays()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var admin = Admin();
        var before = await ListedAsync(clinic.BranchId);

        var response = await PatchAsync(admin, clinic.BranchId, new
        {
            name = "สาขาทองหล่อ",
            address = "55 ซอยทองหล่อ กรุงเทพฯ",
            latitude = 13.7325,
            longitude = 100.5786,
            exam_rooms = new[] { "room_1", "room_2", "room_3" },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);

        var body = await BodyAsync(response);

        Assert.Equal(before.GetProperty("code").GetString(), body.GetProperty("code").GetString());
        Assert.NotEqual(before.GetProperty("version").GetUInt32(), body.GetProperty("version").GetUInt32());

        // What the public list, the booking page and the queue read from now on.
        var listed = await ListedAsync(clinic.BranchId);

        Assert.Equal("สาขาทองหล่อ", listed.GetProperty("name").GetString());
        Assert.Equal("55 ซอยทองหล่อ กรุงเทพฯ", listed.GetProperty("address").GetString());
        Assert.Equal(13.7325, listed.GetProperty("latitude").GetDouble());
        Assert.Equal(100.5786, listed.GetProperty("longitude").GetDouble());
        Assert.Equal(["room_1", "room_2", "room_3"], listed.GetProperty("exam_rooms").EnumerateArray().Select(room => room.GetString()));

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var audit = await db.Set<AuditEntry>().AsNoTracking()
            .Where(row => row.Action == AuditActions.BranchUpdated && row.ResourceId == clinic.BranchId)
            .ToListAsync(Token);

        var row = Assert.Single(audit);
        Assert.Equal(AuditResources.Branch, row.ResourceType);
        Assert.Contains("สาขาทองหล่อ", row.DetailsJson);
        Assert.Contains("room_3", row.DetailsJson);
    }

    [Fact]
    public async Task An_absent_field_is_left_alone_and_an_empty_address_is_cleared()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var admin = Admin();

        (await PatchAsync(admin, clinic.BranchId, new { address = "1 ถนนสุขุมวิท", latitude = 13.7, longitude = 100.5 }))
            .EnsureSuccessStatusCode();

        var cleared = await BodyAsync(await PatchAsync(admin, clinic.BranchId, new { address = "" }));

        Assert.False(cleared.TryGetProperty("address", out _));
        Assert.Equal("สาขาทดสอบ", cleared.GetProperty("name").GetString());
        Assert.Equal(13.7, cleared.GetProperty("latitude").GetDouble());
        Assert.Equal(["room_1", "room_2"], cleared.GetProperty("exam_rooms").EnumerateArray().Select(room => room.GetString()));
    }

    [Fact]
    public async Task Only_settings_write_may_edit_and_nobody_signed_out()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = Admin(Scopes.Settings.Read);
        using var anonymous = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await PatchAsync(reader, clinic.BranchId, new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PatchAsync(anonymous, clinic.BranchId, new { name = "x" })).StatusCode);
        Assert.Equal("สาขาทดสอบ", (await ListedAsync(clinic.BranchId)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task An_admin_tied_to_one_branch_cannot_edit_another()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var admin = AdminOf(mine.BranchId);

        var refused = await PatchAsync(admin, theirs.BranchId, new { name = "ไม่ใช่สาขาของฉัน" });

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("สาขาทดสอบ", (await ListedAsync(theirs.BranchId)).GetProperty("name").GetString());

        Assert.Equal(HttpStatusCode.OK, (await PatchAsync(admin, mine.BranchId, new { name = "สาขาของฉัน" })).StatusCode);
    }

    [Fact]
    public async Task A_branch_that_does_not_exist_is_not_found()
    {
        using var admin = Admin();

        var response = await PatchAsync(admin, "brn_nowhere_at_all", new { name = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("{\"latitude\": 13.7}", "latitude")]
    [InlineData("{\"latitude\": 91, \"longitude\": 100}", "latitude")]
    [InlineData("{\"name\": \"\"}", "name")]
    [InlineData("{\"exam_rooms\": [\"\"]}", "exam_rooms[0]")]
    public async Task A_malformed_edit_is_a_validation_error(string json, string field)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var admin = Admin();

        var response = await PatchAsync(admin, clinic.BranchId, JsonDocument.Parse(json).RootElement);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(field, (await BodyAsync(response)).GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task An_edit_made_on_an_old_version_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var admin = Admin();
        var read = (await ListedAsync(clinic.BranchId)).GetProperty("version").GetUInt32();

        (await PatchAsync(admin, clinic.BranchId, new { name = "คนแรก" }, read)).EnsureSuccessStatusCode();

        var late = await PatchAsync(admin, clinic.BranchId, new { name = "คนที่สอง" }, read);

        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
        Assert.Equal("version_conflict", await ErrorCodeAsync(late));
        Assert.Equal("คนแรก", (await ListedAsync(clinic.BranchId)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task A_room_a_waiting_ticket_is_in_cannot_be_removed_until_the_ticket_is_done()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var ticketId = await TicketInRoomAsync(clinic, "room_2");
        using var admin = Admin();

        var refused = await PatchAsync(admin, clinic.BranchId, new { name = "ชื่อใหม่", exam_rooms = new[] { "room_1" } });

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        var error = (await BodyAsync(refused)).GetProperty("error");
        Assert.Equal("exam_room_in_use", error.GetProperty("code").GetString());
        Assert.Equal("exam_rooms", error.GetProperty("field").GetString());
        Assert.Equal(["room_2"], error.GetProperty("details").GetProperty("rooms").EnumerateArray().Select(room => room.GetString()));

        var blocking = Assert.Single(error.GetProperty("details").GetProperty("tickets").EnumerateArray());
        Assert.Equal(ticketId, blocking.GetProperty("id").GetString());
        Assert.Equal("room_2", blocking.GetProperty("room_id").GetString());

        // Refused whole: the name sent with it did not change either.
        var unchanged = await ListedAsync(clinic.BranchId);
        Assert.Equal("สาขาทดสอบ", unchanged.GetProperty("name").GetString());
        Assert.Equal(["room_1", "room_2"], unchanged.GetProperty("exam_rooms").EnumerateArray().Select(room => room.GetString()));

        // A room nobody is in can go while room_2 is busy, and adding is never blocked.
        Assert.Equal(
            HttpStatusCode.OK,
            (await PatchAsync(admin, clinic.BranchId, new { exam_rooms = new[] { "room_2", "room_3" } })).StatusCode);

        // Once the visit is over the room is free to go.
        await QueueSeed.MarkServedAsync(fixture, ticketId, Token);

        var removed = await PatchAsync(admin, clinic.BranchId, new { exam_rooms = new[] { "room_3" } });

        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal(["room_3"], (await BodyAsync(removed)).GetProperty("exam_rooms").EnumerateArray().Select(room => room.GetString()));
    }
}
