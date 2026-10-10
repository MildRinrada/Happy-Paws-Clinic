using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Queueing;

/// <summary>
/// <c>docs/contract-extensions.md § 31</c>: the reasons and times a ticket stores reach staff and
/// nobody else, <c>GET /staff/queue</c> filters by status, and the vet put on a ticket is an active
/// vet at its branch.
/// </summary>
[Collection(nameof(ApiCollection))]
public class QueueTicketDetailTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] StaffOnlyFields = ["priority_reason", "entered_room_at", "served_at", "left_at", "left_reason"];

    private HttpClient QueueStaff(SeededClinic clinic) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            [Scopes.Queue.Read, Scopes.Queue.Write, Scopes.Queue.Call, Scopes.Appointment.CheckIn, Scopes.Appointment.Read],
            [clinic.BranchId]));

    private static async Task<JsonElement> IssueAsync(HttpClient staff, object body)
    {
        using var request = Idempotent("/staff/queue/tickets", body);
        var response = await staff.SendAsync(request, Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static object WalkIn(SeededClinic clinic, string? staffId = null, string? priority = null, string? reason = null) => new
    {
        pet_id = clinic.PetId,
        service_id = clinic.ServiceId,
        branch_id = clinic.BranchId,
        staff_id = staffId,
        priority,
        priority_reason = reason,
    };

    private static async Task<JsonElement> DayAsync(HttpClient staff, string branchId, string query = "include_finished=true") =>
        await (await staff.GetAsync($"/staff/queue?branch={branchId}&{query}", Token)).Content.ReadFromJsonAsync<JsonElement>(Token);

    private static JsonElement Ticket(JsonElement board, string id) =>
        board.GetProperty("tickets").EnumerateArray().Single(ticket => ticket.GetProperty("id").GetString() == id);

    /// <summary>A member of staff straight into the database, so the test controls type, branches and status.</summary>
    private async Task<string> MemberAsync(string staffType, string[] branches, bool active = true)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var account = UserAccount.CreateStaff("คนทดสอบ", $"member-{Guid.NewGuid():N}@happypaws.test", "not-a-real-hash");
        var membership = StaffMembership.Create(account.Id, staffType, [], branches);
        if (!active)
        {
            membership.Deactivate();
        }

        db.Set<UserAccount>().Add(account);
        db.Set<StaffMembership>().Add(membership);
        await db.SaveChangesAsync(Token);

        return membership.Id;
    }

    [Fact]
    public async Task Staff_see_the_reasons_and_times_the_ticket_stores()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var urgent = await IssueAsync(staff, WalkIn(clinic, priority: "urgent", reason: "อาเจียนตั้งแต่เช้า"));
        Assert.Equal("อาเจียนตั้งแต่เช้า", urgent.GetProperty("priority_reason").GetString());

        var leaving = (await IssueAsync(staff, WalkIn(clinic))).GetProperty("id").GetString()!;
        var left = await staff.PostAsJsonAsync($"/staff/queue/{leaving}/leave", new { reason = "รอนานเกินไป" }, Token);
        var leftBody = await left.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("รอนานเกินไป", leftBody.GetProperty("left_reason").GetString());
        Assert.True(leftBody.TryGetProperty("left_at", out _));

        var served = (await IssueAsync(staff, WalkIn(clinic))).GetProperty("id").GetString()!;
        await QueueSeed.MarkServedAsync(fixture, served, Token);

        var day = await DayAsync(staff, clinic.BranchId);

        Assert.Equal("อาเจียนตั้งแต่เช้า", Ticket(day, urgent.GetProperty("id").GetString()!).GetProperty("priority_reason").GetString());
        Assert.Equal("รอนานเกินไป", Ticket(day, leaving).GetProperty("left_reason").GetString());

        var visit = Ticket(day, served);
        Assert.True(visit.TryGetProperty("entered_room_at", out _));
        Assert.True(visit.TryGetProperty("served_at", out _));
        Assert.False(visit.TryGetProperty("left_at", out _));
    }

    [Fact]
    public async Task The_owner_sees_none_of_them()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var appointmentId = await BookAsStaffAsync(fixture, clinic, 3, Token);
        using var staff = QueueStaff(clinic);
        using var owner = fixture.CustomerOf(clinic);

        using (var checkIn = Idempotent($"/appointments/{appointmentId}/check-in"))
        {
            (await owner.SendAsync(checkIn, Token)).EnsureSuccessStatusCode();
        }

        var ticketId = (await DayAsync(staff, clinic.BranchId, "include_finished=false"))
            .GetProperty("tickets").EnumerateArray().Single().GetProperty("id").GetString()!;

        (await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/priority", new { priority = "urgent", reason = "ไข้สูง" }, Token))
            .EnsureSuccessStatusCode();

        // Checking in again returns the ticket that exists: to the owner without the reason...
        using (var again = Idempotent($"/appointments/{appointmentId}/check-in"))
        {
            var raw = await (await owner.SendAsync(again, Token)).Content.ReadAsStringAsync(Token);
            Assert.DoesNotContain("ไข้สูง", raw);
            Assert.All(StaffOnlyFields, field => Assert.DoesNotContain(field, raw));
        }

        // ...and to the counter with it.
        using (var counter = Idempotent($"/appointments/{appointmentId}/check-in"))
        {
            var body = await (await staff.SendAsync(counter, Token)).Content.ReadFromJsonAsync<JsonElement>(Token);
            Assert.Equal("ไข้สูง", body.GetProperty("ticket").GetProperty("priority_reason").GetString());
        }

        var portal = await (await owner.GetAsync($"/portal/queue/{ticketId}", Token)).Content.ReadAsStringAsync(Token);
        Assert.All(StaffOnlyFields, field => Assert.DoesNotContain(field, portal));
        Assert.DoesNotContain("ไข้สูง", portal);
    }

    [Fact]
    public async Task The_board_filters_by_status()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var waiting = (await IssueAsync(staff, WalkIn(clinic))).GetProperty("id").GetString()!;
        var left = (await IssueAsync(staff, WalkIn(clinic))).GetProperty("id").GetString()!;
        var served = (await IssueAsync(staff, WalkIn(clinic))).GetProperty("id").GetString()!;
        await staff.PostAsJsonAsync($"/staff/queue/{left}/leave", new { }, Token);
        await QueueSeed.MarkServedAsync(fixture, served, Token);

        var finished = await DayAsync(staff, clinic.BranchId, "status=served,left");
        var ids = finished.GetProperty("tickets").EnumerateArray().Select(ticket => ticket.GetProperty("id").GetString()).ToList();

        Assert.Equal(2, ids.Count);
        Assert.Contains(left, ids);
        Assert.Contains(served, ids);
        Assert.DoesNotContain(waiting, ids);
        Assert.Equal(0, finished.GetProperty("waiting_count").GetInt32());

        var onlyWaiting = await DayAsync(staff, clinic.BranchId, "status=waiting");
        Assert.Equal(waiting, onlyWaiting.GetProperty("tickets").EnumerateArray().Single().GetProperty("id").GetString());

        var typo = await staff.GetAsync($"/staff/queue?branch={clinic.BranchId}&status=finished", Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, typo.StatusCode);
        var error = (await typo.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("status", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Assigning_someone_who_is_not_an_active_vet_at_the_branch_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var ticketId = (await IssueAsync(staff, WalkIn(clinic))).GetProperty("id").GetString()!;

        var receptionist = await MemberAsync("receptionist", [clinic.BranchId]);
        var elsewhere = await MemberAsync("veterinarian", [other.BranchId]);
        var departed = await MemberAsync("veterinarian", [clinic.BranchId], active: false);

        foreach (var staffId in new[] { "stf_nobody", receptionist, elsewhere, departed })
        {
            var response = await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/assign", new { staff_id = staffId }, Token);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
            Assert.Equal("staff_id", error.GetProperty("field").GetString());
        }

        // A vet bound to no branch works everywhere, and the seeded vet works here.
        var roaming = await MemberAsync("veterinarian", []);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/assign", new { staff_id = roaming }, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/assign", new { staff_id = clinic.StaffId }, Token)).StatusCode);
    }

    [Fact]
    public async Task A_walk_in_ticket_names_only_a_vet_at_the_branch()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);
        var receptionist = await MemberAsync("receptionist", [clinic.BranchId]);

        using var request = Idempotent("/staff/queue/tickets", WalkIn(clinic, staffId: receptionist));
        var refused = await staff.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);

        var issued = await IssueAsync(staff, WalkIn(clinic, staffId: clinic.StaffId));
        Assert.Equal(clinic.StaffId, issued.GetProperty("staff_id").GetString());
    }
}
