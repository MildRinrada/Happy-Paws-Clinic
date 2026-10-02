using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Queueing;

/// <summary>
/// Assign, call and skip — the three actions on a ticket, their scope split, and what an owner
/// sees while waiting.
/// </summary>
[Collection(nameof(ApiCollection))]
public class QueueLifecycleTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient QueueStaff(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Queue.Read, Scopes.Queue.Write, Scopes.Queue.Call] : scopes,
            [clinic.BranchId]));

    private async Task<(SeededClinic Clinic, string TicketId)> TicketAsync()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var staff = QueueStaff(clinic);
        using var request = Idempotent("/staff/queue/tickets", new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
        });

        var response = await staff.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();

        var ticket = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        return (clinic, ticket.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task Assigning_sets_the_vet_and_the_room()
    {
        var (clinic, ticketId) = await TicketAsync();
        using var staff = QueueStaff(clinic);

        var response = await staff.PostAsJsonAsync(
            $"/staff/queue/{ticketId}/assign",
            new { staff_id = clinic.StaffId, room_id = "room_2" },
            Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ticket = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(clinic.StaffId, ticket.GetProperty("staff_id").GetString());
        Assert.Equal("room_2", ticket.GetProperty("room_id").GetString());
        // Assignment is not a state change — that is what makes a mid-visit room move possible.
        Assert.Equal("waiting", ticket.GetProperty("status").GetString());
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task A_room_that_is_not_at_the_branch_is_refused()
    {
        // Accepting any string would send an owner to a door that does not exist, and the board is
        // read by people in a waiting room rather than by someone who can spot the typo.
        var (clinic, ticketId) = await TicketAsync();
        using var staff = QueueStaff(clinic);

        var response = await staff.PostAsJsonAsync(
            $"/staff/queue/{ticketId}/assign", new { room_id = "room_99" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("room_id", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task An_assignment_with_neither_vet_nor_room_is_refused()
    {
        var (clinic, ticketId) = await TicketAsync();
        using var staff = QueueStaff(clinic);

        var response = await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/assign", new { }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Calling_records_the_caller_and_moves_the_ticket_to_called()
    {
        var (clinic, ticketId) = await TicketAsync();
        using var staff = QueueStaff(clinic);

        var response = await staff.PostAsync($"/staff/queue/{ticketId}/call", null, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ticket = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("called", ticket.GetProperty("status").GetString());
        Assert.True(ticket.TryGetProperty("called_at", out _));

        var stored = await QueueSeed.ReloadAsync(fixture, ticketId, Token);
        Assert.Equal(clinic.StaffId, stored!.CalledByStaffId);
    }

    [Fact]
    public async Task Calling_needs_queue_call_and_assigning_needs_queue_write()
    {
        // The contract splits these deliberately: a receptionist organises the board, and calling a
        // patient forward is a separate permission.
        var (clinic, ticketId) = await TicketAsync();

        using var organiser = QueueStaff(clinic, Scopes.Queue.Read, Scopes.Queue.Write);
        var refusedCall = await organiser.PostAsync($"/staff/queue/{ticketId}/call", null, Token);

        Assert.Equal(HttpStatusCode.Forbidden, refusedCall.StatusCode);

        var error = (await refusedCall.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal(
            "queue:call",
            error.GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Single().GetString());

        using var caller = QueueStaff(clinic, Scopes.Queue.Read, Scopes.Queue.Call);
        var refusedAssign = await caller.PostAsJsonAsync(
            $"/staff/queue/{ticketId}/assign", new { room_id = "room_1" }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, refusedAssign.StatusCode);
    }

    [Fact]
    public async Task A_ticket_that_has_left_cannot_be_called()
    {
        var (clinic, ticketId) = await TicketAsync();
        await QueueSeed.MarkLeftAsync(fixture, ticketId, Token);

        using var staff = QueueStaff(clinic);
        var response = await staff.PostAsync($"/staff/queue/{ticketId}/call", null, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("state_conflict", error.GetProperty("code").GetString());
        Assert.Equal("Left", error.GetProperty("details").GetProperty("current_state").GetString());
    }

    [Fact]
    public async Task A_stale_if_match_refuses_the_call_and_leaves_the_ticket_alone()
    {
        var (clinic, ticketId) = await TicketAsync();
        using var staff = QueueStaff(clinic);

        var before = await QueueSeed.ReloadAsync(fixture, ticketId, Token);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/staff/queue/{ticketId}/call");
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{before!.Version + 1}\"");

        var response = await staff.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("version_conflict", error.GetProperty("code").GetString());

        var after = await QueueSeed.ReloadAsync(fixture, ticketId, Token);
        Assert.Equal(Domain.Queueing.QueueTicketStatus.Waiting, after!.Status);
    }

    [Fact]
    public async Task An_owner_sees_their_number_and_how_many_are_ahead()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var ids = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            using var request = Idempotent("/staff/queue/tickets", new
            {
                pet_id = clinic.PetId,
                service_id = clinic.ServiceId,
                branch_id = clinic.BranchId,
            });

            var created = await staff.SendAsync(request, Token);
            ids.Add((await created.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!);
        }

        using var owner = fixture.CustomerOf(clinic);

        var third = await owner.GetAsync($"/portal/queue/{ids[2]}", Token);
        Assert.Equal(HttpStatusCode.OK, third.StatusCode);

        var mine = await third.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("A003", mine.GetProperty("number").GetString());
        Assert.Equal(2, mine.GetProperty("ahead").GetInt32());
        Assert.Equal("waiting", mine.GetProperty("status").GetString());

        // Someone already in a room is being seen, not queueing — the count must not stall on them.
        await QueueSeed.EnterRoomAsync(fixture, ids[0], Token);

        var updated = await owner.GetAsync($"/portal/queue/{ids[2]}", Token);
        Assert.Equal(1, (await updated.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("ahead").GetInt32());
    }

    [Fact]
    public async Task A_skipped_ticket_ahead_of_you_stops_being_ahead()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = QueueStaff(clinic);

        var ids = new List<string>();
        for (var i = 0; i < 2; i++)
        {
            using var request = Idempotent("/staff/queue/tickets", new
            {
                pet_id = clinic.PetId,
                service_id = clinic.ServiceId,
                branch_id = clinic.BranchId,
            });

            var created = await staff.SendAsync(request, Token);
            ids.Add((await created.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!);
        }

        using var owner = fixture.CustomerOf(clinic);

        var before = await owner.GetAsync($"/portal/queue/{ids[1]}", Token);
        Assert.Equal(1, (await before.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("ahead").GetInt32());

        await staff.PostAsync($"/staff/queue/{ids[0]}/skip", null, Token);

        var after = await owner.GetAsync($"/portal/queue/{ids[1]}", Token);
        Assert.Equal(0, (await after.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("ahead").GetInt32());
    }

    [Fact]
    public async Task Another_customers_ticket_is_hidden_behind_a_404()
    {
        // Queue ids are short-lived and guessable in bulk; a 403 would expose how busy the clinic
        // is and who is in it.
        var (_, ticketId) = await TicketAsync();
        var (otherCustomerId, _) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        using var stranger = fixture.Authenticated(
            TestTokens.Customer(fixture, "usr_stranger", otherCustomerId));

        var response = await stranger.GetAsync($"/portal/queue/{ticketId}", Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
