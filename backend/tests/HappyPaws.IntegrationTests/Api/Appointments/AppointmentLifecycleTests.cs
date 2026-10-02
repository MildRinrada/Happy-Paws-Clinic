using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Appointments;

/// <summary>
/// The lifecycle after booking: confirm, check in (which issues a queue ticket), reschedule and
/// cancel — including the state transitions that must be refused.
/// </summary>
[Collection(nameof(ApiCollection))]
public class AppointmentLifecycleTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<(SeededClinic Clinic, string AppointmentId)> BookedAsync(int hoursFromNow)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        return (clinic, await BookAsStaffAsync(fixture, clinic, hoursFromNow, Token));
    }

    [Fact]
    public async Task A_customer_confirms_their_own_appointment_without_holding_any_staff_scope()
    {
        // The contract marks this route "ทั้งสองฝั่ง" and a customer holds only `portal`. If the
        // route carried a scope attribute, half of its intended audience would get a 403.
        var (clinic, appointmentId) = await BookedAsync(4);

        using var customer = fixture.CustomerOf(clinic);
        var response = await customer.PostAsync($"/appointments/{appointmentId}/confirm", null, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var appointment = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("confirmed", appointment.GetProperty("status").GetString());
        Assert.True(appointment.TryGetProperty("confirmed_at", out _));
    }

    [Fact]
    public async Task Confirming_twice_still_answers_ok()
    {
        var (clinic, appointmentId) = await BookedAsync(5);
        using var customer = fixture.CustomerOf(clinic);

        await customer.PostAsync($"/appointments/{appointmentId}/confirm", null, Token);
        var second = await customer.PostAsync($"/appointments/{appointmentId}/confirm", null, Token);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Check_in_issues_a_queue_ticket_in_the_same_transaction()
    {
        var (clinic, appointmentId) = await BookedAsync(6);

        using var staff = fixture.StaffOf(clinic);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/appointments/{appointmentId}/check-in");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await staff.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("checked_in", body.GetProperty("appointment").GetProperty("status").GetString());

        var ticket = body.GetProperty("ticket");
        Assert.StartsWith("tkt_", ticket.GetProperty("id").GetString());
        Assert.Equal("waiting", ticket.GetProperty("status").GetString());
        // A014-style, reset daily per branch.
        Assert.Matches(@"^A\d{3}$", ticket.GetProperty("number").GetString());
        Assert.Equal(clinic.BranchId, ticket.GetProperty("branch_id").GetString());
    }

    [Fact]
    public async Task Queue_numbers_run_in_sequence_within_a_branch_and_day()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);

        var numbers = new List<string>();

        for (var hour = 3; hour <= 5; hour++)
        {
            using var book = new HttpRequestMessage(HttpMethod.Post, "/staff/appointments")
            {
                Content = JsonContent.Create(new
                {
                    pet_id = clinic.PetId,
                    service_id = clinic.ServiceId,
                    starts_at = ClinicSeed.SlotAt(hour),
                    branch_id = clinic.BranchId,
                }),
            };
            book.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

            var booked = await staff.SendAsync(book, Token);
            var id = (await booked.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString();

            using var checkIn = new HttpRequestMessage(HttpMethod.Post, $"/appointments/{id}/check-in");
            checkIn.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

            var result = await staff.SendAsync(checkIn, Token);
            var ticket = (await result.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("ticket");

            numbers.Add(ticket.GetProperty("number").GetString()!);
        }

        Assert.Equal(["A001", "A002", "A003"], numbers);
    }

    [Fact]
    public async Task Checking_in_twice_returns_the_same_ticket_rather_than_issuing_a_second()
    {
        var (clinic, appointmentId) = await BookedAsync(7);
        using var staff = fixture.StaffOf(clinic);

        var numbers = new List<string>();

        // Two distinct idempotency keys, so this is not the middleware replaying — the handler
        // itself has to notice the visit was already admitted.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/appointments/{appointmentId}/check-in");
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

            var response = await staff.SendAsync(request, Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var ticket = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("ticket");
            numbers.Add(ticket.GetProperty("number").GetString()!);
        }

        Assert.Equal(numbers[0], numbers[1]);
    }

    [Fact]
    public async Task A_cancelled_appointment_cannot_be_checked_in()
    {
        var (clinic, appointmentId) = await BookedAsync(8);
        using var staff = fixture.StaffOf(clinic);

        await staff.PostAsJsonAsync($"/appointments/{appointmentId}/cancel", new { reason = "ยกเลิก" }, Token);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/appointments/{appointmentId}/check-in");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await staff.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

        Assert.Equal("state_conflict", error.GetProperty("code").GetString());
        Assert.Equal("Cancelled", error.GetProperty("details").GetProperty("current_state").GetString());
    }

    [Fact]
    public async Task Cancelling_records_who_and_why_and_frees_the_slot()
    {
        var (clinic, appointmentId) = await BookedAsync(9);
        var startsAt = ClinicSeed.SlotAt(9);

        using var staff = fixture.StaffOf(clinic);

        var response = await staff.PostAsJsonAsync(
            $"/appointments/{appointmentId}/cancel", new { reason = "เจ้าของติดธุระ" }, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stored = await ClinicSeed.ReloadAppointmentAsync(fixture, appointmentId, Token);
        Assert.NotNull(stored);
        Assert.Equal("เจ้าของติดธุระ", stored.CancellationReason);
        Assert.Equal(clinic.StaffUserId, stored.CancelledByUserId);
        // The row survives; the contract needs to know who called it off.
        Assert.False(stored.OccupiesSlot);

        // "ปล่อย slot คืน" — the time is bookable again with no separate release step.
        using var anonymous = fixture.CreateClient();
        var feed = await anonymous.GetAsync(
            $"/booking/availability?service={clinic.ServiceId}&date={ClinicSeed.ClinicDate(startsAt)}&branch={clinic.BranchId}",
            Token);

        var slots = (await feed.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("slots").EnumerateArray()
            .Select(slot => slot.GetProperty("starts_at").GetDateTimeOffset());

        Assert.Contains(startsAt, slots);
    }

    [Fact]
    public async Task Rescheduling_moves_the_appointment_and_clears_its_confirmation()
    {
        var (clinic, appointmentId) = await BookedAsync(10);
        using var staff = fixture.StaffOf(clinic);

        await staff.PostAsync($"/appointments/{appointmentId}/confirm", null, Token);

        var newStart = ClinicSeed.SlotAt(12);
        var response = await staff.PostAsJsonAsync(
            $"/appointments/{appointmentId}/reschedule", new { starts_at = newStart }, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var appointment = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(newStart, appointment.GetProperty("starts_at").GetDateTimeOffset());
        Assert.Equal("booked", appointment.GetProperty("status").GetString());
        // Confirmed for the old time is not confirmed for the new one.
        Assert.False(appointment.TryGetProperty("confirmed_at", out _));
    }

    [Fact]
    public async Task Rescheduling_by_a_few_minutes_does_not_collide_with_the_slot_it_is_vacating()
    {
        // The appointment must be excluded from its own collision check, or every small nudge is
        // refused as a double booking.
        var (clinic, appointmentId) = await BookedAsync(11);
        using var staff = fixture.StaffOf(clinic);

        var nudged = ClinicSeed.SlotAt(11).AddMinutes(10);
        var response = await staff.PostAsJsonAsync(
            $"/appointments/{appointmentId}/reschedule", new { starts_at = nudged }, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            nudged,
            (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("starts_at").GetDateTimeOffset());
    }

    [Fact]
    public async Task A_customer_cannot_reschedule_past_the_cutoff_but_staff_can()
    {
        // Cut-off is twelve hours by default, so an appointment four hours out is closed to the
        // customer and still open to the counter.
        var (clinic, appointmentId) = await BookedAsync(4);

        using var customer = fixture.CustomerOf(clinic);
        var refused = await customer.PostAsJsonAsync(
            $"/appointments/{appointmentId}/reschedule",
            new { starts_at = ClinicSeed.SlotAt(20) },
            Token);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        var error = (await refused.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("cutoff_passed", error.GetProperty("code").GetString());
        Assert.Equal(12, error.GetProperty("details").GetProperty("cutoff_hours").GetInt32());

        using var staff = fixture.StaffOf(clinic);
        var accepted = await staff.PostAsJsonAsync(
            $"/appointments/{appointmentId}/reschedule",
            new { starts_at = ClinicSeed.SlotAt(20) },
            Token);

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }
}
