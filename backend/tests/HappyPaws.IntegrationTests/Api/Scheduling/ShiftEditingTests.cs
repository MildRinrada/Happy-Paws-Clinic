using System.Net;
using System.Net.Http.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.RosterScenario;

namespace HappyPaws.IntegrationTests.Api.Scheduling;

/// <summary>
/// <c>PATCH</c> and <c>DELETE /staff/schedules/shifts/{id}</c> (<c>docs/contract-extensions.md § 47</c>):
/// one block moves or goes, history stays, the create clash rules hold, and the appointments in the
/// time taken away are listed with who could take them.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ShiftEditingTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static DateOnly InDays(int days) => Today.AddDays(days);

    /// <summary>A colleague at the clinic's branch, rostered for the seeded shift's whole span.</summary>
    private async Task<string> ColleagueOnShiftAsync(SeededClinic clinic, HttpClient scheduler)
    {
        var colleague = await IdentitySeed.StaffAsync(
            fixture, [Scopes.Appointment.Read], withTwoFactor: false, branches: [clinic.BranchId], cancellationToken: Token);

        await CreateShiftsAsync(scheduler, new
        {
            staff_id = colleague.StaffId,
            branch_id = clinic.BranchId,
            starts_at = clinic.ShiftStartsAt,
            ends_at = clinic.ShiftEndsAt,
        }, Token);

        return colleague.StaffId;
    }

    /// <summary>The seeded shift, under way since an hour ago, as the calendar shows it.</summary>
    private static async Task<string> SeededShiftIdAsync(HttpClient scheduler, SeededClinic clinic)
    {
        var shifts = await ShiftsOnCalendarAsync(scheduler, clinic, clinic.StaffId, Today.AddDays(-1), Today.AddDays(1), Token);

        return shifts.Single(shift => shift.GetProperty("starts_at").GetDateTimeOffset() == clinic.ShiftStartsAt)
            .GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task A_future_shift_moves_with_its_version_and_the_calendar_shows_the_new_times()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var day = InDays(10);

        var shift = Assert.Single(await CreateShiftsAsync(scheduler, new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = At(day, 9),
            ends_at = At(day, 17),
        }, Token));
        var id = shift.GetProperty("id").GetString()!;
        var version = shift.GetProperty("version").GetUInt32();

        var response = await SendShiftAsync(
            scheduler, HttpMethod.Patch, id, Token, new { starts_at = At(day, 10), ends_at = At(day, 16) }, version);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var body = await BodyAsync(response, Token);
        var moved = body.GetProperty("shift");
        Assert.Equal(id, moved.GetProperty("id").GetString());
        Assert.Equal(At(day, 10), moved.GetProperty("starts_at").GetDateTimeOffset());
        Assert.NotEqual(version, moved.GetProperty("version").GetUInt32());
        Assert.Empty(body.GetProperty("conflicts").EnumerateArray());

        var onCalendar = Assert.Single(await ShiftsOnCalendarAsync(scheduler, clinic, clinic.StaffId, day, day, Token));
        Assert.Equal(At(day, 16), onCalendar.GetProperty("ends_at").GetDateTimeOffset());

        var audited = Assert.Single(await AuditAsync(fixture, AuditActions.ShiftChanged, id, Token));
        Assert.Contains("starts_at_before", audited.DetailsJson!);

        // The version read before the move is stale now.
        var stale = await SendShiftAsync(
            scheduler, HttpMethod.Patch, id, Token, new { starts_at = At(day, 9), ends_at = At(day, 16) }, version);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("version_conflict", ErrorCode(await BodyAsync(stale, Token)));
    }

    [Fact]
    public async Task Shortening_a_shift_under_way_lists_the_appointments_left_outside_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var booked = await BookingScenario.BookAsStaffAsync(fixture, clinic, hoursFromNow: 6, Token);
        var colleague = await ColleagueOnShiftAsync(clinic, scheduler);
        var id = await SeededShiftIdAsync(scheduler, clinic);

        var response = await SendShiftAsync(
            scheduler, HttpMethod.Patch, id, Token, new { starts_at = clinic.ShiftStartsAt, ends_at = ClinicSeed.SlotAt(4) });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conflict = Assert.Single((await BodyAsync(response, Token)).GetProperty("conflicts").EnumerateArray());
        Assert.Equal(booked, conflict.GetProperty("appointment_id").GetString());
        Assert.Equal([colleague], conflict.GetProperty("suggested_staff_ids").EnumerateArray().Select(staff => staff.GetString()));

        // The appointment stays; a person decides what to do with it.
        Assert.NotNull(await ClinicSeed.ReloadAppointmentAsync(fixture, booked, Token));
    }

    [Fact]
    public async Task The_start_of_a_shift_under_way_cannot_move()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var id = await SeededShiftIdAsync(scheduler, clinic);

        var response = await SendShiftAsync(
            scheduler, HttpMethod.Patch, id, Token, new { starts_at = clinic.ShiftStartsAt.AddHours(2), ends_at = clinic.ShiftEndsAt });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("starts_at", (await BodyAsync(response, Token)).GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_shift_under_way_cannot_be_removed()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var id = await SeededShiftIdAsync(scheduler, clinic);

        var response = await SendShiftAsync(scheduler, HttpMethod.Delete, id, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("shift_started", ErrorCode(await BodyAsync(response, Token)));
        Assert.Null((await StoredShiftAsync(fixture, id, Token))!.CancelledAt);
    }

    [Fact]
    public async Task A_shift_that_has_ended_is_history()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var yesterday = Today.AddDays(-1);
        var past = await StoreShiftAsync(fixture, clinic.StaffId, clinic.BranchId, At(yesterday, 0), At(yesterday, 4), Token);

        var change = await SendShiftAsync(
            scheduler, HttpMethod.Patch, past.Id, Token, new { starts_at = At(yesterday, 0), ends_at = At(yesterday, 3) });
        var remove = await SendShiftAsync(scheduler, HttpMethod.Delete, past.Id, Token);

        Assert.Equal(HttpStatusCode.Conflict, change.StatusCode);
        Assert.Equal("shift_ended", ErrorCode(await BodyAsync(change, Token)));
        Assert.Equal(HttpStatusCode.Conflict, remove.StatusCode);
        Assert.Equal("shift_ended", ErrorCode(await BodyAsync(remove, Token)));
        Assert.Equal(At(yesterday, 4), (await StoredShiftAsync(fixture, past.Id, Token))!.EndsAt);
    }

    [Fact]
    public async Task Removing_one_block_of_a_split_shift_leaves_the_other_and_lists_its_appointments()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var day = InDays(9);

        var blocks = await CreateShiftsAsync(scheduler, new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = At(day, 9),
            ends_at = At(day, 17),
            @break = new { starts_at = At(day, 12), ends_at = At(day, 13) },
        }, Token);
        var morning = blocks.Single(block => block.GetProperty("starts_at").GetDateTimeOffset() == At(day, 9)).GetProperty("id").GetString()!;

        using var staff = fixture.StaffOf(clinic);
        using var booking = BookingScenario.Idempotent(
            "/staff/appointments", BookingScenario.Booking(clinic, At(day, 10), clinic.StaffId));
        var created = await staff.SendAsync(booking, Token);
        created.EnsureSuccessStatusCode();
        var appointment = (await BodyAsync(created, Token)).GetProperty("id").GetString();

        var response = await SendShiftAsync(scheduler, HttpMethod.Delete, morning, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await BodyAsync(response, Token);
        Assert.Equal(morning, body.GetProperty("shift_id").GetString());
        var conflict = Assert.Single(body.GetProperty("conflicts").EnumerateArray());
        Assert.Equal(appointment, conflict.GetProperty("appointment_id").GetString());

        var left = Assert.Single(await ShiftsOnCalendarAsync(scheduler, clinic, clinic.StaffId, day, day, Token));
        Assert.Equal(At(day, 13), left.GetProperty("starts_at").GetDateTimeOffset());
        Assert.NotNull((await StoredShiftAsync(fixture, morning, Token))!.CancelledAt);
        Assert.Single(await AuditAsync(fixture, AuditActions.ShiftRemoved, morning, Token));

        // Gone is gone: there is nothing left to remove.
        Assert.Equal(HttpStatusCode.NotFound, (await SendShiftAsync(scheduler, HttpMethod.Delete, morning, Token)).StatusCode);
    }

    [Fact]
    public async Task A_moved_shift_may_not_land_on_another_shift_or_on_time_off()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var day = InDays(11);

        var blocks = await CreateShiftsAsync(scheduler, new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = At(day, 9),
            ends_at = At(day, 17),
            @break = new { starts_at = At(day, 12), ends_at = At(day, 13) },
        }, Token);
        var afternoon = blocks.Single(block => block.GetProperty("starts_at").GetDateTimeOffset() == At(day, 13)).GetProperty("id").GetString()!;

        var onTheMorning = await SendShiftAsync(
            scheduler, HttpMethod.Patch, afternoon, Token, new { starts_at = At(day, 11), ends_at = At(day, 17) });
        Assert.Equal(HttpStatusCode.Conflict, onTheMorning.StatusCode);
        Assert.Equal("shift_overlap", ErrorCode(await BodyAsync(onTheMorning, Token)));

        (await scheduler.PostAsJsonAsync("/staff/schedules/time-off", new
        {
            staff_id = clinic.StaffId,
            starts_at = At(day, 18),
            ends_at = At(day, 22),
        }, Token)).EnsureSuccessStatusCode();

        var onLeave = await SendShiftAsync(
            scheduler, HttpMethod.Patch, afternoon, Token, new { starts_at = At(day, 13), ends_at = At(day, 19) });
        Assert.Equal(HttpStatusCode.Conflict, onLeave.StatusCode);
        Assert.Equal("staff_on_time_off", ErrorCode(await BodyAsync(onLeave, Token)));

        var stored = (await StoredShiftAsync(fixture, afternoon, Token))!;
        Assert.Equal(At(day, 13), stored.StartsAt);
        Assert.Equal(At(day, 17), stored.EndsAt);
    }

    [Fact]
    public async Task Changing_or_removing_a_shift_needs_schedule_write()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        using var reader = fixture.StaffOf(clinic, Scopes.Schedule.Read);
        var day = InDays(12);

        var shift = Assert.Single(await CreateShiftsAsync(scheduler, new
        {
            staff_id = clinic.StaffId,
            branch_id = clinic.BranchId,
            starts_at = At(day, 9),
            ends_at = At(day, 17),
        }, Token));
        var id = shift.GetProperty("id").GetString()!;

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await SendShiftAsync(reader, HttpMethod.Patch, id, Token, new { starts_at = At(day, 10), ends_at = At(day, 17) })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendShiftAsync(reader, HttpMethod.Delete, id, Token)).StatusCode);
    }

    [Fact]
    public async Task A_shift_at_another_branch_is_not_found()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        using var theirScheduler = fixture.Scheduler(elsewhere);
        var day = InDays(13);

        var theirs = Assert.Single(await CreateShiftsAsync(theirScheduler, new
        {
            staff_id = elsewhere.StaffId,
            branch_id = elsewhere.BranchId,
            starts_at = At(day, 9),
            ends_at = At(day, 17),
        }, Token));
        var id = theirs.GetProperty("id").GetString()!;

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendShiftAsync(scheduler, HttpMethod.Patch, id, Token, new { starts_at = At(day, 10), ends_at = At(day, 17) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendShiftAsync(scheduler, HttpMethod.Delete, id, Token)).StatusCode);
        Assert.Null((await StoredShiftAsync(fixture, id, Token))!.CancelledAt);
    }
}
