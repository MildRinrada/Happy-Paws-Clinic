using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Appointments;

/// <summary>
/// No-shows, late cancellations, appointment types, conflict checks and the waiting list.
/// <para>
/// The tests that change the clinic-wide rule sets put them back as they found them. The shared
/// collection runs one test at a time, so nothing else sees the change in between.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class AdvancedAppointmentTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly object DefaultTypes = new
    {
        types = new object[]
        {
            new { code = "regular", name = "นัดปกติ" },
            new { code = "emergency", name = "ฉุกเฉิน", online_booking = false, queue_priority = "emergency" },
            new { code = "follow_up", name = "ติดตามผล" },
            new { code = "surgery", name = "ผ่าตัด", online_booking = false },
        },
    };

    private static readonly object DefaultPolicy = new { cutoff_hours = 24, fee_satang = 0, free_allowance = 1, window_days = 365 };

    private HttpClient Admin() =>
        fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Settings.Write, Scopes.Appointment.Read]));

    private static async Task<JsonElement> ErrorOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    [Fact]
    public async Task A_no_show_is_recorded_after_the_start_and_counted_against_the_customer()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var missed = await AdvancedBookingSeed.PastAppointmentAsync(fixture, clinic, Token);
        using var staff = fixture.StaffOf(clinic);

        var response = await staff.PostAsync($"/staff/appointments/{missed}/no-show", null, Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var attendance = result.GetProperty("attendance");

        Assert.Equal("no_show", result.GetProperty("appointment").GetProperty("status").GetString());
        Assert.Equal(1, attendance.GetProperty("no_shows").GetInt32());
        Assert.Equal(1, attendance.GetProperty("strikes_in_window").GetInt32());
        Assert.Equal(0, attendance.GetProperty("fee_due_satang").GetInt64());
    }

    [Fact]
    public async Task A_visit_that_has_not_started_cannot_be_marked_a_no_show()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var upcoming = await BookAsStaffAsync(fixture, clinic, 4, Token);
        using var staff = fixture.StaffOf(clinic);

        var response = await staff.PostAsync($"/staff/appointments/{upcoming}/no-show", null, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("no_show_too_early", (await ErrorOf(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_customer_cancelling_inside_the_cutoff_is_marked_late_and_staff_cancelling_is_not()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var byCustomer = await BookAsStaffAsync(fixture, clinic, 3, Token);
        var byClinic = await BookAsStaffAsync(fixture, clinic, 5, Token);

        using var customer = fixture.CustomerOf(clinic);
        using var staff = fixture.StaffOf(clinic);

        var late = await (await customer.PostAsJsonAsync($"/appointments/{byCustomer}/cancel", new { reason = "ติดธุระ" }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);
        var notLate = await (await staff.PostAsJsonAsync($"/appointments/{byClinic}/cancel", new { reason = "หมอป่วย" }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.True(late.GetProperty("late_cancellation").GetBoolean());
        Assert.False(notLate.GetProperty("late_cancellation").GetBoolean());
    }

    [Fact]
    public async Task The_cancellation_policy_is_replaced_whole_and_its_fee_applies_past_the_allowance()
    {
        using var admin = Admin();

        try
        {
            var response = await admin.PutAsJsonAsync(
                "/staff/policies/cancellation", new { cutoff_hours = 12, fee_satang = 20_000, free_allowance = 0, window_days = 90 }, Token);
            response.EnsureSuccessStatusCode();

            var policy = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
            Assert.Equal(20_000, policy.GetProperty("fee_satang").GetInt64());

            var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
            var missed = await AdvancedBookingSeed.PastAppointmentAsync(fixture, clinic, Token);
            using var staff = fixture.StaffOf(clinic);

            var result = await (await staff.PostAsync($"/staff/appointments/{missed}/no-show", null, Token))
                .Content.ReadFromJsonAsync<JsonElement>(Token);

            Assert.Equal(20_000, result.GetProperty("attendance").GetProperty("fee_due_satang").GetInt64());
        }
        finally
        {
            (await admin.PutAsJsonAsync("/staff/policies/cancellation", DefaultPolicy, Token)).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task An_emergency_cannot_be_booked_online_and_enters_the_queue_as_one()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var customer = fixture.CustomerOf(clinic);
        using var staff = fixture.StaffOf(clinic);

        var online = await customer.SendAsync(
            Idempotent("/portal/appointments", new { pet_id = clinic.PetId, service_id = clinic.ServiceId, starts_at = ClinicSeed.SlotAt(4), branch_id = clinic.BranchId, type = "emergency" }),
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, online.StatusCode);
        Assert.Equal("type", (await ErrorOf(online)).GetProperty("field").GetString());

        var booked = await (await staff.SendAsync(
            Idempotent("/staff/appointments", new { pet_id = clinic.PetId, service_id = clinic.ServiceId, starts_at = ClinicSeed.SlotAt(2), branch_id = clinic.BranchId, type = "emergency" }),
            Token)).Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("emergency", booked.GetProperty("type").GetString());

        var checkIn = await (await staff.SendAsync(Idempotent($"/appointments/{booked.GetProperty("id").GetString()}/check-in"), Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("emergency", checkIn.GetProperty("ticket").GetProperty("priority").GetString());
    }

    [Fact]
    public async Task Appointment_types_are_replaced_as_a_set_that_must_keep_regular()
    {
        using var admin = Admin();

        try
        {
            var withoutRegular = await admin.PutAsJsonAsync(
                "/staff/appointment-types", new { types = new[] { new { code = "dental", name = "ทันตกรรม" } } }, Token);

            Assert.Equal(HttpStatusCode.Conflict, withoutRegular.StatusCode);
            Assert.Equal("invalid_appointment_types", (await ErrorOf(withoutRegular)).GetProperty("code").GetString());

            var response = await admin.PutAsJsonAsync(
                "/staff/appointment-types",
                new { types = new object[] { new { code = "regular", name = "นัดปกติ" }, new { code = "dental", name = "ทันตกรรม", duration_minutes = 60 } } },
                Token);

            response.EnsureSuccessStatusCode();
            var types = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

            Assert.Equal(2, types.GetProperty("types").GetArrayLength());
            Assert.Equal(60, types.GetProperty("types")[1].GetProperty("duration_minutes").GetInt32());
        }
        finally
        {
            (await admin.PutAsJsonAsync("/staff/appointment-types", DefaultTypes, Token)).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task A_proposed_time_reports_the_vet_and_the_pet_already_booked()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await BookAsStaffAsync(fixture, clinic, 5, Token);
        using var staff = fixture.StaffOf(clinic);

        var report = await staff.GetFromJsonAsync<JsonElement>(
            $"/staff/appointments/conflicts?starts_at={Uri.EscapeDataString(ClinicSeed.SlotAt(5).ToString("O"))}&service_id={clinic.ServiceId}&vet_id={clinic.StaffId}&pet_id={clinic.PetId}&branch={clinic.BranchId}",
            Token);

        var kinds = report.GetProperty("conflicts").EnumerateArray().Select(item => item.GetProperty("kind").GetString()).ToList();

        Assert.False(report.GetProperty("clear").GetBoolean());
        Assert.Contains("staff_double_booked", kinds);
        Assert.Contains("pet_double_booked", kinds);
    }

    [Fact]
    public async Task A_time_outside_the_vets_shift_is_reported_and_a_free_one_is_clear()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);

        string Query(int hours) =>
            $"/staff/appointments/conflicts?starts_at={Uri.EscapeDataString(ClinicSeed.SlotAt(hours).ToString("O"))}&service_id={clinic.ServiceId}&vet_id={clinic.StaffId}&branch={clinic.BranchId}";

        var offShift = await staff.GetFromJsonAsync<JsonElement>(Query(30), Token);
        var free = await staff.GetFromJsonAsync<JsonElement>(Query(6), Token);

        Assert.Equal("staff_off_shift", offShift.GetProperty("conflicts")[0].GetProperty("kind").GetString());
        Assert.True(free.GetProperty("clear").GetBoolean());
    }

    [Fact]
    public async Task Leave_recorded_after_booking_shows_up_when_checking_the_day()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var booked = await BookAsStaffAsync(fixture, clinic, 6, Token);
        using var manager = fixture.StaffOf(clinic, Scopes.Appointment.Read, Scopes.Schedule.Write);

        var slot = ClinicSeed.SlotAt(6);
        (await manager.PostAsJsonAsync(
            "/staff/schedules/time-off",
            new { staff_id = clinic.StaffId, starts_at = slot.AddMinutes(-30), ends_at = slot.AddHours(2), reason = "ป่วย" },
            Token)).EnsureSuccessStatusCode();

        var report = await manager.GetFromJsonAsync<JsonElement>(
            $"/staff/appointments/conflicts?date={ClinicSeed.ClinicDate(slot)}&branch={clinic.BranchId}", Token);

        Assert.Contains(
            report.GetProperty("conflicts").EnumerateArray(),
            item => item.GetProperty("kind").GetString() == "staff_on_time_off" && item.GetProperty("appointment_id").GetString() == booked);
    }

    [Fact]
    public async Task A_course_is_booked_whole_with_one_vet_and_one_series_id()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await AdvancedBookingSeed.NextDayShiftAsync(fixture, clinic, Token);
        using var staff = fixture.StaffOf(clinic);

        var response = await staff.SendAsync(
            Idempotent("/staff/appointments/recurring", new
            {
                pet_id = clinic.PetId,
                service_id = clinic.ServiceId,
                branch_id = clinic.BranchId,
                first_starts_at = ClinicSeed.SlotAt(4),
                rrule = "FREQ=DAILY;COUNT=2",
            }),
            Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var series = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var visits = series.GetProperty("appointments").EnumerateArray().ToList();

        Assert.Equal(2, visits.Count);
        Assert.All(visits, visit => Assert.Equal(series.GetProperty("series_id").GetString(), visit.GetProperty("series_id").GetString()));
        Assert.Single(visits.Select(visit => visit.GetProperty("staff_id").GetString()).Distinct());
    }

    [Fact]
    public async Task A_course_with_a_date_nobody_can_take_books_nothing_and_says_which_date()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);

        var response = await staff.SendAsync(
            Idempotent("/staff/appointments/recurring", new
            {
                pet_id = clinic.PetId,
                service_id = clinic.ServiceId,
                branch_id = clinic.BranchId,
                first_starts_at = ClinicSeed.SlotAt(4),
                rrule = "FREQ=DAILY;COUNT=2",
            }),
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await ErrorOf(response);

        Assert.Equal("slot_unavailable", error.GetProperty("code").GetString());
        Assert.Equal(1, error.GetProperty("details").GetProperty("conflicts").GetArrayLength());
        Assert.Empty(await ClinicSeed.AppointmentsAtAsync(fixture, clinic.BranchId, Token));
    }

    [Fact]
    public async Task Cancelling_a_visit_tells_the_people_waiting_for_that_service()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (otherCustomer, otherPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var staff = fixture.StaffOf(clinic);

        var waiting = await staff.PostAsJsonAsync(
            "/staff/waiting-list", new { pet_id = otherPet, service_id = clinic.ServiceId, branch_id = clinic.BranchId }, Token);
        Assert.Equal(HttpStatusCode.Created, waiting.StatusCode);

        var again = await staff.PostAsJsonAsync("/staff/waiting-list", new { pet_id = otherPet, service_id = clinic.ServiceId }, Token);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        var booked = await BookAsStaffAsync(fixture, clinic, 6, Token);
        (await staff.PostAsJsonAsync($"/appointments/{booked}/cancel", new { reason = "ติดธุระ" }, Token)).EnsureSuccessStatusCode();

        Assert.Contains(fixture.Notifications.SentTo(otherCustomer), sent => sent.TemplateKey == "waiting_list.slot_available");
    }
}
