using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Queueing;

/// <summary>
/// The two queue endpoints that are <b>not</b> in the API reference: priority and leave.
/// <para>
/// Both close gaps found by building against the contract — its queue is strictly first-come with a
/// skip, and its state machine has a <c>left</c> state nothing can reach. See
/// <c>docs/contract-extensions.md</c>.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class QueuePriorityTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Counter(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Queue.Read, Scopes.Queue.Write, Scopes.Queue.Call] : scopes,
            [clinic.BranchId]));

    /// <summary>Issues a walk-in ticket for a fresh patient at the same branch.</summary>
    private async Task<string> WalkInAsync(SeededClinic clinic, HttpClient staff, object? extra = null)
    {
        var (_, petId) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        var body = new Dictionary<string, object?>
        {
            ["pet_id"] = petId,
            ["service_id"] = clinic.ServiceId,
            ["branch_id"] = clinic.BranchId,
        };

        if (extra is not null)
        {
            foreach (var property in extra.GetType().GetProperties())
            {
                body[property.Name] = property.GetValue(extra);
            }
        }

        using var request = Idempotent("/staff/queue/tickets", body);

        var response = await staff.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();

        var ticket = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        return ticket.GetProperty("id").GetString()!;
    }

    private async Task<IReadOnlyList<string>> BoardOrderAsync(SeededClinic clinic, HttpClient staff)
    {
        var board = await staff.GetFromJsonAsync<JsonElement>($"/staff/queue?branch={clinic.BranchId}", Token);

        return [.. board.GetProperty("tickets").EnumerateArray().Select(t => t.GetProperty("id").GetString()!)];
    }

    [Fact]
    public async Task An_emergency_is_called_next_whatever_number_it_holds()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var first = await WalkInAsync(clinic, counter);
        var second = await WalkInAsync(clinic, counter);
        var third = await WalkInAsync(clinic, counter);

        var raised = await counter.PostAsJsonAsync(
            $"/staff/queue/{third}/priority",
            new { priority = "emergency", reason = "ถูกรถชน · เลือดออกไม่หยุด" },
            Token);

        Assert.Equal(HttpStatusCode.OK, raised.StatusCode);

        var order = await BoardOrderAsync(clinic, counter);

        // A clinic that cannot put a hit-by-car ahead of a nail trim will do it on paper instead,
        // and then the board is a lie.
        Assert.Equal([third, first, second], order);
    }

    [Fact]
    public async Task Urgent_sits_between_emergency_and_the_rest()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var routine = await WalkInAsync(clinic, counter);
        var urgent = await WalkInAsync(clinic, counter);
        var emergency = await WalkInAsync(clinic, counter);

        await counter.PostAsJsonAsync(
            $"/staff/queue/{urgent}/priority", new { priority = "urgent", reason = "เจ็บปวดมาก" }, Token);
        await counter.PostAsJsonAsync(
            $"/staff/queue/{emergency}/priority", new { priority = "emergency", reason = "หายใจไม่ออก" }, Token);

        Assert.Equal([emergency, urgent, routine], await BoardOrderAsync(clinic, counter));
    }

    [Fact]
    public async Task Raising_a_priority_without_a_reason_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var ticketId = await WalkInAsync(clinic, counter);

        var response = await counter.PostAsJsonAsync(
            $"/staff/queue/{ticketId}/priority", new { priority = "emergency" }, Token);

        // Jumping the queue is a decision another owner is entitled to have questioned afterwards.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_priority_change_is_written_to_the_audit_log()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        using var auditor = Counter(clinic, Scopes.Audit.Read);

        var ticketId = await WalkInAsync(clinic, counter);

        await counter.PostAsJsonAsync(
            $"/staff/queue/{ticketId}/priority",
            new { priority = "urgent", reason = "ลูกสุนัขอายุ 6 สัปดาห์ ท้องเสียรุนแรง" },
            Token);

        var log = await auditor.GetFromJsonAsync<JsonElement>(
            $"/staff/audit-logs?resource_id={ticketId}&action=queue.priority_changed", Token);

        var entry = log.GetProperty("data")[0];
        var details = entry.GetProperty("details");

        Assert.Equal("Normal", details.GetProperty("from").GetString());
        Assert.Equal("Urgent", details.GetProperty("to").GetString());
        Assert.StartsWith("ลูกสุนัข", details.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task A_walk_in_can_be_issued_as_urgent_at_the_counter()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var routine = await WalkInAsync(clinic, counter);
        var urgent = await WalkInAsync(clinic, counter, new { priority = "urgent", priority_reason = "อาเจียนต่อเนื่อง" });

        Assert.Equal([urgent, routine], await BoardOrderAsync(clinic, counter));
    }

    [Fact]
    public async Task An_urgent_walk_in_without_a_reason_is_refused_at_the_counter_too()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        var (_, petId) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        using var request = Idempotent("/staff/queue/tickets", new
        {
            pet_id = petId,
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
            priority = "urgent",
        });

        var response = await counter.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_unrecognised_priority_is_refused_rather_than_treated_as_normal()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var ticketId = await WalkInAsync(clinic, counter);

        var response = await counter.PostAsJsonAsync(
            $"/staff/queue/{ticketId}/priority", new { priority = "emergancy", reason = "พิมพ์ผิด" }, Token);

        // Silently reading a typo as normal would put a collapsing patient at the back of the queue.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Leaving_takes_a_ticket_off_the_board_without_claiming_it_was_seen()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var staying = await WalkInAsync(clinic, counter);
        var leaving = await WalkInAsync(clinic, counter);

        var response = await counter.PostAsJsonAsync(
            $"/staff/queue/{leaving}/leave", new { reason = "รอไม่ไหว ขอนัดใหม่" }, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ticket = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("left", ticket.GetProperty("status").GetString());

        // Not "served": a visit that never happened must not land in the day's figures.
        Assert.Equal([staying], await BoardOrderAsync(clinic, counter));
    }

    [Fact]
    public async Task The_position_an_owner_is_told_agrees_with_the_board_after_a_priority_change()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var appointmentId = await BookAsStaffAsync(fixture, clinic, 3, Token);
        using var owner = fixture.CustomerOf(clinic);
        using var checkInStaff = fixture.StaffOf(clinic);

        var mine = await ConsultationScenario.CheckInAsync(checkInStaff, appointmentId, Token);

        var ahead = await WalkInAsync(clinic, counter);
        await counter.PostAsJsonAsync(
            $"/staff/queue/{ahead}/priority", new { priority = "emergency", reason = "ฉุกเฉิน" }, Token);

        var mineView = await owner.GetFromJsonAsync<JsonElement>($"/portal/queue/{mine}", Token);
        var order = await BoardOrderAsync(clinic, counter);

        // One ordering, expressed once. Two would be one edit away from telling a waiting owner a
        // different story from the screen above their head.
        Assert.Equal(1, mineView.GetProperty("ahead").GetInt32());
        Assert.Equal([ahead, mine], order);
    }
}
