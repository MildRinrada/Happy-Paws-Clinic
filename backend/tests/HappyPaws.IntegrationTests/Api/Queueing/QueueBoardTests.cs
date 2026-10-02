using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Queueing;

/// <summary>
/// The counter's board: who is on it, in what order, and who is allowed to see it.
/// </summary>
[Collection(nameof(ApiCollection))]
public class QueueBoardTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient QueueStaff(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Queue.Read, Scopes.Queue.Write, Scopes.Queue.Call] : scopes,
            [clinic.BranchId]));

    private async Task<JsonElement> BoardAsync(HttpClient client, SeededClinic clinic, string? query = null)
    {
        var response = await client.GetAsync($"/staff/queue?branch={clinic.BranchId}{query}", Token);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static IEnumerable<string> Numbers(JsonElement board) =>
        board.GetProperty("tickets").EnumerateArray().Select(t => t.GetProperty("number").GetString()!);

    private async Task<JsonElement> IssueWalkInAsync(HttpClient client, SeededClinic clinic)
    {
        using var request = Idempotent("/staff/queue/tickets", new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
        });

        var response = await client.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    [Fact]
    public async Task A_walk_in_gets_a_number_without_an_appointment()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var ticket = await IssueWalkInAsync(staff, clinic);

        Assert.StartsWith("tkt_", ticket.GetProperty("id").GetString());
        Assert.Equal("A001", ticket.GetProperty("number").GetString());
        Assert.Equal("waiting", ticket.GetProperty("status").GetString());
        Assert.Equal(clinic.PetId, ticket.GetProperty("pet_id").GetString());

        // No appointment was invented to hang the ticket off — the two are separate in the contract.
        Assert.False(ticket.TryGetProperty("appointment_id", out _));
    }

    [Fact]
    public async Task Issuing_a_ticket_requires_an_idempotency_key()
    {
        // The contract requires the header on every POST that creates a queue.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var response = await staff.PostAsJsonAsync(
            "/staff/queue/tickets",
            new { pet_id = clinic.PetId, service_id = clinic.ServiceId, branch_id = clinic.BranchId },
            Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("idempotency_key_required", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Walk_ins_and_checked_in_appointments_share_one_daily_sequence()
    {
        // Both routes into the queue draw from the same per-branch, per-day allocator, so two
        // patients cannot both be A001.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var staff = QueueStaff(clinic, Scopes.Queue.Read, Scopes.Queue.Write,
            Scopes.Appointment.Read, Scopes.Appointment.Write, Scopes.Appointment.CheckIn);

        var walkIn = await IssueWalkInAsync(staff, clinic);

        var appointmentId = await BookAsStaffAsync(fixture, clinic, 4, Token);
        using var checkIn = Idempotent($"/appointments/{appointmentId}/check-in");
        var checkedIn = await staff.SendAsync(checkIn, Token);

        var fromAppointment = (await checkedIn.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("ticket").GetProperty("number").GetString();

        Assert.Equal("A001", walkIn.GetProperty("number").GetString());
        Assert.Equal("A002", fromAppointment);
    }

    [Fact]
    public async Task The_board_lists_todays_tickets_with_a_waiting_count()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        await IssueWalkInAsync(staff, clinic);
        await IssueWalkInAsync(staff, clinic);

        var board = await BoardAsync(staff, clinic);

        Assert.Equal(clinic.BranchId, board.GetProperty("branch_id").GetString());
        Assert.Equal(2, board.GetProperty("waiting_count").GetInt32());
        Assert.Equal(["A001", "A002"], Numbers(board));
    }

    [Fact]
    public async Task A_skipped_ticket_moves_to_the_back_of_the_board()
    {
        // "เลื่อนไปท้ายแถว" — the whole point of skip.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var first = await IssueWalkInAsync(staff, clinic);
        await IssueWalkInAsync(staff, clinic);
        await IssueWalkInAsync(staff, clinic);

        Assert.Equal(["A001", "A002", "A003"], Numbers(await BoardAsync(staff, clinic)));

        var skipped = await staff.PostAsync(
            $"/staff/queue/{first.GetProperty("id").GetString()}/skip", null, Token);
        Assert.Equal(HttpStatusCode.OK, skipped.StatusCode);

        Assert.Equal(["A002", "A003", "A001"], Numbers(await BoardAsync(staff, clinic)));

        // Still waiting to be called, just later.
        Assert.Equal(3, (await BoardAsync(staff, clinic)).GetProperty("waiting_count").GetInt32());
    }

    [Fact]
    public async Task Being_passed_over_a_second_time_puts_you_behind_someone_passed_over_once()
    {
        // The real sequence: called, no answer, skipped — twice for A001 and once for A002. Every
        // step is asserted, because a skip that was silently refused would leave the board in the
        // order the test expected for the wrong reason.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var first = (await IssueWalkInAsync(staff, clinic)).GetProperty("id").GetString()!;
        var second = (await IssueWalkInAsync(staff, clinic)).GetProperty("id").GetString()!;

        await CallThenSkipAsync(staff, first);
        await CallThenSkipAsync(staff, second);
        await CallThenSkipAsync(staff, first);

        // A001 was passed over most recently, so it now sits behind A002.
        Assert.Equal(["A002", "A001"], Numbers(await BoardAsync(staff, clinic)));

        var stored = await QueueSeed.ReloadAsync(fixture, first, Token);
        Assert.Equal(2, stored!.SkipCount);
    }

    [Fact]
    public async Task Skipping_a_ticket_that_is_already_at_the_back_is_refused()
    {
        // There is nothing to skip: the ticket is already last in line. Answering 409 rather than
        // quietly demoting it again keeps SkipCount an honest count of how many times the owner was
        // actually called and did not come forward.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var ticketId = (await IssueWalkInAsync(staff, clinic)).GetProperty("id").GetString()!;

        var first = await staff.PostAsync($"/staff/queue/{ticketId}/skip", null, Token);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await staff.PostAsync($"/staff/queue/{ticketId}/skip", null, Token);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var error = (await second.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("state_conflict", error.GetProperty("code").GetString());
        Assert.Equal("Skipped", error.GetProperty("details").GetProperty("current_state").GetString());
    }

    private async Task CallThenSkipAsync(HttpClient staff, string ticketId)
    {
        var called = await staff.PostAsync($"/staff/queue/{ticketId}/call", null, Token);
        Assert.Equal(HttpStatusCode.OK, called.StatusCode);

        var skipped = await staff.PostAsync($"/staff/queue/{ticketId}/skip", null, Token);
        Assert.Equal(HttpStatusCode.OK, skipped.StatusCode);
    }

    [Fact]
    public async Task Finished_tickets_leave_the_live_board_but_stay_in_the_days_record()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var ticket = await IssueWalkInAsync(staff, clinic);
        var ticketId = ticket.GetProperty("id").GetString();

        await QueueSeed.MarkLeftAsync(fixture, ticketId!, Token);

        Assert.Empty(Numbers(await BoardAsync(staff, clinic)));
        Assert.Equal(["A001"], Numbers(await BoardAsync(staff, clinic, "&include_finished=true")));
    }

    [Fact]
    public async Task The_board_can_be_narrowed_to_one_room()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var first = await IssueWalkInAsync(staff, clinic);
        await IssueWalkInAsync(staff, clinic);

        await staff.PostAsJsonAsync(
            $"/staff/queue/{first.GetProperty("id").GetString()}/assign", new { room_id = "room_2" }, Token);

        Assert.Equal(["A001"], Numbers(await BoardAsync(staff, clinic, "&room=room_2")));
    }

    [Fact]
    public async Task The_board_needs_queue_read_and_says_what_is_missing()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var unscoped = QueueStaff(clinic, Scopes.Patient.Read);

        var response = await unscoped.GetAsync($"/staff/queue?branch={clinic.BranchId}", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal(
            "queue:read",
            error.GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Single().GetString());
    }

    [Fact]
    public async Task Staff_cannot_read_the_board_of_a_branch_they_do_not_work_at()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var elsewhere = fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: [Scopes.Queue.Read], branches: ["brn_somewhere_else"]));

        var response = await elsewhere.GetAsync($"/staff/queue?branch={clinic.BranchId}", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
