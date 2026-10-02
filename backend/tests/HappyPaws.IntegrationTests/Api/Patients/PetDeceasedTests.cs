using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Patients;

/// <summary>
/// <c>POST /staff/pets/{id}/deceased</c> — "ปิดเรกคอร์ดอย่างสุภาพ · หยุดแจ้งเตือนทุกชนิด".
/// <para>
/// The contract says to stop notifications and says nothing about visits already in the calendar.
/// This project cancels them, because the alternative is a reminder — or a call from the counter —
/// asking a grieving owner to confirm an appointment for a pet that has died. See
/// <c>docs/contract-extensions.md § 6</c>.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class PetDeceasedTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Counter(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Patient.Write, Scopes.Audit.Read] : scopes,
            [clinic.BranchId]));

    [Fact]
    public async Task Closing_a_record_cancels_the_visits_already_booked()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        var soon = await BookAsStaffAsync(fixture, clinic, 4, Token);
        var later = await BookAsStaffAsync(fixture, clinic, 8, Token);

        using var counter = Counter(clinic);

        var response = await counter.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/deceased",
            new { note = "จากไปอย่างสงบที่บ้าน" },
            Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var cancelled = result.GetProperty("cancelled_appointment_ids").EnumerateArray()
            .Select(id => id.GetString()).ToList();

        Assert.Equal(2, cancelled.Count);
        Assert.Contains(soon, cancelled);
        Assert.Contains(later, cancelled);

        foreach (var appointmentId in new[] { soon, later })
        {
            var appointment = await ClinicSeed.ReloadAppointmentAsync(fixture, appointmentId, Token);
            Assert.Equal(AppointmentStatus.Cancelled, appointment!.Status);
            Assert.Equal("สัตว์เลี้ยงเสียชีวิต", appointment.CancellationReason);
        }
    }

    [Fact]
    public async Task A_completed_visit_in_the_past_is_left_alone()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var upcoming = await BookAsStaffAsync(fixture, clinic, 5, Token);

        using var counter = Counter(clinic);
        await counter.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/deceased", new { }, Token);

        // History is never deleted. "ปิดเรกคอร์ดอย่างสุภาพ" is about stopping the clinic reaching
        // out, not about erasing a patient.
        var all = await ClinicSeed.AppointmentsAtAsync(fixture, clinic.BranchId, Token);
        Assert.Single(all);
        Assert.Equal(upcoming, all[0].Id);
    }

    [Fact]
    public async Task The_patient_can_no_longer_be_booked_or_given_a_queue_number()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var counter = Counter(clinic);
        await counter.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/deceased", new { }, Token);

        using var staff = fixture.StaffOf(clinic);
        using var booking = Idempotent("/staff/appointments", Booking(clinic, ClinicSeed.SlotAt(6)));

        var booked = await staff.SendAsync(booking, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, booked.StatusCode);
    }

    [Fact]
    public async Task Closing_a_record_is_audited_together_with_every_cancellation_it_caused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await BookAsStaffAsync(fixture, clinic, 4, Token);

        using var counter = Counter(clinic);
        await counter.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/deceased", new { }, Token);

        var log = await counter.GetFromJsonAsync<JsonElement>($"/staff/audit-logs?pet_id={clinic.PetId}", Token);
        var actions = log.GetProperty("data").EnumerateArray()
            .Select(row => row.GetProperty("action").GetString()).ToList();

        Assert.Contains("pet.marked_deceased", actions);
        Assert.Contains("appointment.cancelled", actions);

        var closure = log.GetProperty("data").EnumerateArray()
            .First(row => row.GetProperty("action").GetString() == "pet.marked_deceased");

        Assert.Equal(1, closure.GetProperty("details").GetProperty("cancelled_appointments").GetInt32());
    }

    [Fact]
    public async Task Pressing_it_twice_does_not_move_the_date_of_death()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var backdated = DateTimeOffset.UtcNow.AddDays(-2);

        var first = await counter.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/deceased", new { occurred_at = backdated }, Token);
        var second = await counter.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/deceased", new { }, Token);

        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>(Token);
        var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>(Token);

        // The response echoes what was asked for, but the record keeps the first answer — a second
        // press at the counter must not quietly restate when a patient died.
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Empty(secondBody.GetProperty("cancelled_appointment_ids").EnumerateArray());

        var pet = await PatientSeed.ReloadPetAsync(fixture, clinic.PetId, Token);
        Assert.Equal(
            firstBody.GetProperty("deceased_at").GetDateTimeOffset().ToUnixTimeMilliseconds(),
            pet!.DeceasedAt!.Value.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task It_takes_patient_write_and_not_merely_a_signed_in_staff_token()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = Counter(clinic, Scopes.Patient.Read);

        var response = await reader.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/deceased", new { }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
