using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Scheduling;

/// <summary>
/// The roster: shifts, time off, the calendar, free time and assignment rules, and what each of
/// them does to booking.
/// </summary>
[Collection(nameof(ApiCollection))]
public class SchedulesTests(ApiFixture fixture)
{
    private static readonly TimeSpan Bangkok = TimeSpan.FromHours(7);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Scheduler(SeededClinic clinic) =>
        fixture.StaffOf(
            clinic,
            Scopes.Schedule.Read,
            Scopes.Schedule.Write,
            Scopes.Appointment.Read,
            Scopes.Appointment.Write);

    /// <summary>A whole-hour, clinic-local time some days ahead, safely beyond the lead time.</summary>
    private static DateTimeOffset DaysAhead(int days, int hour)
    {
        var today = DateTimeOffset.UtcNow.ToOffset(Bangkok).Date;
        var day = today.AddDays(days);

        return new DateTimeOffset(day.Year, day.Month, day.Day, hour, 0, 0, Bangkok);
    }

    private static string Date(DateTimeOffset instant) => ClinicSeed.ClinicDate(instant);

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private async Task<List<string>> OpenSlotsAsync(SeededClinic clinic, DateTimeOffset day, string staffId)
    {
        using var anonymous = fixture.CreateClient();

        var feed = await anonymous.GetFromJsonAsync<JsonElement>(
            $"/booking/availability?service={clinic.ServiceId}&date={Date(day)}&branch={clinic.BranchId}", Token);

        return
        [
            .. feed.GetProperty("slots").EnumerateArray()
                .Where(slot => slot.GetProperty("staff_id").GetString() == staffId)
                .Select(slot => slot.GetProperty("starts_at").GetDateTimeOffset().ToOffset(Bangkok).ToString("HH:mm")),
        ];
    }

    private async Task<HttpResponseMessage> CreateShiftsAsync(HttpClient client, object body) =>
        await client.PostAsJsonAsync("/staff/schedules/shifts", body, Token);

    [Fact]
    public async Task A_repeating_rule_puts_shifts_on_the_roster_and_makes_them_bookable()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);
        var first = DaysAhead(3, 9);

        var response = await CreateShiftsAsync(scheduler, new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = first,
            ends_at = first.AddHours(8),
            rrule = "FREQ=DAILY;COUNT=3",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var shifts = (await BodyAsync(response)).GetProperty("data").EnumerateArray().ToList();
        Assert.Equal(3, shifts.Count);

        var calendar = await scheduler.GetFromJsonAsync<JsonElement>(
            $"/staff/schedules?from={Date(first)}&to={Date(first.AddDays(2))}", Token);
        var mine = calendar.GetProperty("staff").EnumerateArray()
            .Single(person => person.GetProperty("staff_id").GetString() == clinic.StaffId);
        Assert.Equal(3, mine.GetProperty("shifts").GetArrayLength());

        Assert.Contains("09:00", await OpenSlotsAsync(clinic, first.AddDays(1), clinic.StaffId));
    }

    [Fact]
    public async Task A_shift_on_top_of_another_is_refused_and_nothing_is_created()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);
        var start = DaysAhead(4, 9);
        var week = new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = start,
            ends_at = start.AddHours(8),
            rrule = "FREQ=DAILY;COUNT=2",
        };

        (await CreateShiftsAsync(scheduler, week)).EnsureSuccessStatusCode();

        // A retried request is the commonest way to get here.
        var again = await CreateShiftsAsync(scheduler, week);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        var error = (await BodyAsync(again)).GetProperty("error");
        Assert.Equal("shift_overlap", error.GetProperty("code").GetString());
        Assert.Equal(2, error.GetProperty("details").GetProperty("conflicts").GetArrayLength());

        var calendar = await scheduler.GetFromJsonAsync<JsonElement>(
            $"/staff/schedules?from={Date(start)}&to={Date(start.AddDays(1))}&staff_id={clinic.StaffId}", Token);
        Assert.Equal(2, calendar.GetProperty("staff")[0].GetProperty("shifts").GetArrayLength());
    }

    [Fact]
    public async Task A_break_is_a_gap_nobody_can_book_into()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);
        var start = DaysAhead(5, 9);

        var response = await CreateShiftsAsync(scheduler, new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = start,
            ends_at = start.AddHours(8),
            @break = new { starts_at = start.AddHours(3), ends_at = start.AddHours(4) },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, (await BodyAsync(response)).GetProperty("data").GetArrayLength());

        var slots = await OpenSlotsAsync(clinic, start, clinic.StaffId);
        Assert.Contains("11:30", slots);
        Assert.DoesNotContain("12:00", slots);
        Assert.DoesNotContain("12:30", slots);
        Assert.Contains("13:00", slots);
    }

    [Fact]
    public async Task Time_off_closes_the_time_and_names_who_could_take_the_appointments_in_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);

        // Booked while the seeded vet is the only one rostered, so it is theirs.
        var booked = await BookingScenario.BookAsStaffAsync(fixture, clinic, hoursFromNow: 5, Token);

        var colleague = await IdentitySeed.StaffAsync(
            fixture, [Scopes.Appointment.Read], withTwoFactor: false, branches: [clinic.BranchId], cancellationToken: Token);
        (await CreateShiftsAsync(scheduler, new
        {
            staff_id = colleague.StaffId,
            branch_id = clinic.BranchId,
            starts_at = clinic.ShiftStartsAt,
            ends_at = clinic.ShiftEndsAt,
        })).EnsureSuccessStatusCode();

        var response = await scheduler.PostAsJsonAsync("/staff/schedules/time-off", new
        {
            staff_id = clinic.StaffId,
            starts_at = ClinicSeed.SlotAt(4),
            ends_at = ClinicSeed.SlotAt(8),
            reason = "ลาป่วย",
        }, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await BodyAsync(response);

        Assert.Equal(1, result.GetProperty("shifts_cancelled").GetInt32());
        Assert.Equal(2, result.GetProperty("shifts_remaining").GetArrayLength());

        var conflict = Assert.Single(result.GetProperty("conflicts").EnumerateArray());
        Assert.Equal(booked, conflict.GetProperty("appointment_id").GetString());
        Assert.Equal(
            [colleague.StaffId],
            conflict.GetProperty("suggested_staff_ids").EnumerateArray().Select(id => id.GetString()));

        // The appointment is still there; new ones cannot land in the leave.
        Assert.NotNull(await ClinicSeed.ReloadAppointmentAsync(fixture, booked, Token));

        using var staff = fixture.StaffOf(clinic);
        using var refused = BookingScenario.Idempotent(
            "/staff/appointments", BookingScenario.Booking(clinic, ClinicSeed.SlotAt(6), clinic.StaffId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await staff.SendAsync(refused, Token)).StatusCode);

        // And a shift cannot be put back over it.
        var shiftInLeave = await CreateShiftsAsync(scheduler, new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = ClinicSeed.SlotAt(5),
            ends_at = ClinicSeed.SlotAt(6),
        });
        Assert.Equal("staff_on_time_off", (await BodyAsync(shiftInLeave)).GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Free_time_is_the_shift_minus_what_is_booked()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);
        await BookingScenario.BookAsStaffAsync(fixture, clinic, hoursFromNow: 5, Token);

        var day = ClinicSeed.SlotAt(5);
        var availability = await scheduler.GetFromJsonAsync<JsonElement>(
            $"/staff/schedules/availability?date={Date(day)}&branch={clinic.BranchId}&staff_id={clinic.StaffId}", Token);

        Assert.False(availability.GetProperty("closed").GetBoolean());
        var person = Assert.Single(availability.GetProperty("staff").EnumerateArray());
        var free = person.GetProperty("free").EnumerateArray().ToList();

        Assert.Single(person.GetProperty("booked").EnumerateArray());
        Assert.DoesNotContain(free, window =>
            window.GetProperty("starts_at").GetDateTimeOffset() <= day && window.GetProperty("ends_at").GetDateTimeOffset() > day);
    }

    [Fact]
    public async Task An_assignment_rule_decides_who_gets_a_booking_that_names_nobody()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);

        var colleague = await IdentitySeed.StaffAsync(
            fixture, [Scopes.Appointment.Read], withTwoFactor: false, branches: [clinic.BranchId], cancellationToken: Token);
        (await CreateShiftsAsync(scheduler, new
        {
            staff_id = colleague.StaffId,
            branch_id = clinic.BranchId,
            starts_at = clinic.ShiftStartsAt,
            ends_at = clinic.ShiftEndsAt,
        })).EnsureSuccessStatusCode();

        // Prefer whoever the stable default order would not pick, so the rule is what decides.
        var preferred = string.CompareOrdinal(clinic.StaffId, colleague.StaffId) < 0 ? colleague.StaffId : clinic.StaffId;

        var rules = await scheduler.PutAsJsonAsync("/staff/schedules/assignment-rules", new
        {
            rules = new[] { new { service_id = clinic.ServiceId, staff_ids = new[] { preferred } } },
        }, Token);
        Assert.Equal(HttpStatusCode.OK, rules.StatusCode);
        Assert.True(rules.Headers.ETag is not null);

        using var staff = fixture.StaffOf(clinic);
        using var booking = BookingScenario.Idempotent("/staff/appointments", BookingScenario.Booking(clinic, ClinicSeed.SlotAt(5)));
        var created = await staff.SendAsync(booking, Token);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(preferred, (await BodyAsync(created)).GetProperty("staff_id").GetString());

        // Leave the rule set as other tests expect it: empty.
        (await scheduler.PutAsJsonAsync("/staff/schedules/assignment-rules", new { rules = Array.Empty<object>() }, Token))
            .EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_rule_naming_someone_who_does_not_work_here_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);

        var response = await scheduler.PutAsJsonAsync("/staff/schedules/assignment-rules", new
        {
            rules = new[] { new { species = "cat", staff_ids = new[] { "stf_nobody" } } },
        }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("FREQ=MONTHLY;COUNT=2")]
    [InlineData("FREQ=WEEKLY")]
    public async Task A_rule_it_cannot_follow_is_a_validation_error(string rrule)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);
        var start = DaysAhead(6, 9);

        var response = await CreateShiftsAsync(scheduler, new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = start,
            ends_at = start.AddHours(8),
            rrule,
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task The_calendar_shows_at_most_a_month()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);

        var response = await scheduler.GetAsync("/staff/schedules?from=2026-10-01&to=2026-11-15", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Changing_the_roster_needs_schedule_write()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.StaffOf(clinic, Scopes.Schedule.Read);
        var start = DaysAhead(7, 9);

        var response = await CreateShiftsAsync(reader, new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = start,
            ends_at = start.AddHours(8),
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/staff/schedules", Token)).StatusCode);
    }

    [Fact]
    public async Task Someone_at_another_branch_cannot_be_scheduled()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = Scheduler(clinic);
        var start = DaysAhead(8, 9);

        var response = await scheduler.PostAsJsonAsync("/staff/schedules/time-off", new
        {
            staff_id = elsewhere.StaffId,
            starts_at = start,
            ends_at = start.AddHours(4),
        }, Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
