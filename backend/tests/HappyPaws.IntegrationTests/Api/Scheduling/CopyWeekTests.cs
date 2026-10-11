using System.Net;
using System.Net.Http.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.RosterScenario;

namespace HappyPaws.IntegrationTests.Api.Scheduling;

/// <summary>
/// <c>POST /staff/schedules/shifts/copy-week</c> (<c>docs/contract-extensions.md § 49</c>): a branch's
/// week lands on another week at the same clinic times, all or nothing, with create's clash answer.
/// </summary>
[Collection(nameof(ApiCollection))]
public class CopyWeekTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<string> ColleagueAsync(SeededClinic clinic) =>
        (await IdentitySeed.StaffAsync(
            fixture, [Scopes.Appointment.Read], withTwoFactor: false, branches: [clinic.BranchId], cancellationToken: Token)).StaffId;

    private static Task<HttpResponseMessage> CopyAsync(HttpClient client, object body) =>
        client.PostAsJsonAsync("/staff/schedules/shifts/copy-week", body, Token);

    /// <summary>A week three or more weeks out, clear of every other test's shifts for these people.</summary>
    private static DateOnly FarMonday(int weeks) => MondayWeeksAhead(weeks);

    /// <summary>Monday 09:00 to 18:00 with lunch, and Wednesday 09:00 to 17:00, for one person.</summary>
    private static async Task RosterAsync(HttpClient scheduler, SeededClinic clinic, string staffId, DateOnly monday)
    {
        await CreateShiftsAsync(scheduler, new
        {
            staff_id = staffId,
            branch_id = clinic.BranchId,
            starts_at = At(monday, 9),
            ends_at = At(monday, 18),
            @break = new { starts_at = At(monday, 12), ends_at = At(monday, 13) },
        }, Token);
        await CreateShiftsAsync(scheduler, new
        {
            staff_id = staffId,
            branch_id = clinic.BranchId,
            starts_at = At(monday.AddDays(2), 9),
            ends_at = At(monday.AddDays(2), 17),
        }, Token);
    }

    [Fact]
    public async Task A_week_lands_on_the_next_at_the_same_clinic_times()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var colleague = await ColleagueAsync(clinic);
        var from = FarMonday(4);
        var to = from.AddDays(7);

        await RosterAsync(scheduler, clinic, clinic.StaffId, from);
        await CreateShiftsAsync(scheduler, new
        {
            staff_id = colleague,
            branch_id = clinic.BranchId,
            starts_at = At(from.AddDays(1), 8),
            ends_at = At(from.AddDays(1), 16),
        }, Token);

        var response = await CopyAsync(scheduler, new { branch_id = clinic.BranchId, from_week = Day(from), to_week = Day(to) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await BodyAsync(response, Token);
        Assert.Equal(4, body.GetProperty("data").GetArrayLength());
        Assert.Equal(0, body.GetProperty("skipped_started").GetInt32());

        var mine = await ShiftsOnCalendarAsync(scheduler, clinic, clinic.StaffId, to, to.AddDays(6), Token);
        Assert.Equal(
            [At(to, 9), At(to, 13), At(to.AddDays(2), 9)],
            mine.Select(shift => shift.GetProperty("starts_at").GetDateTimeOffset()).Order());
        var theirs = Assert.Single(await ShiftsOnCalendarAsync(scheduler, clinic, colleague, to, to.AddDays(6), Token));
        Assert.Equal(At(to.AddDays(1), 16), theirs.GetProperty("ends_at").GetDateTimeOffset());

        Assert.Single(await AuditAsync(fixture, AuditActions.WeekCopied, clinic.StaffId, Token));
        Assert.Single(await AuditAsync(fixture, AuditActions.WeekCopied, colleague, Token));
    }

    [Fact]
    public async Task One_clash_and_nothing_is_copied()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var colleague = await ColleagueAsync(clinic);
        var from = FarMonday(5);
        var to = from.AddDays(7);

        await RosterAsync(scheduler, clinic, clinic.StaffId, from);
        await RosterAsync(scheduler, clinic, colleague, from);
        await CreateShiftsAsync(scheduler, new
        {
            staff_id = colleague,
            branch_id = clinic.BranchId,
            starts_at = At(to.AddDays(2), 16),
            ends_at = At(to.AddDays(2), 20),
        }, Token);

        var response = await CopyAsync(scheduler, new { branch_id = clinic.BranchId, from_week = Day(from), to_week = Day(to) });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = (await BodyAsync(response, Token)).GetProperty("error");
        Assert.Equal("shift_overlap", error.GetProperty("code").GetString());
        var clash = Assert.Single(error.GetProperty("details").GetProperty("conflicts").EnumerateArray());
        Assert.Equal(colleague, clash.GetProperty("staff_id").GetString());

        Assert.Empty(await ShiftsOnCalendarAsync(scheduler, clinic, clinic.StaffId, to, to.AddDays(6), Token));
        Assert.Single(await ShiftsOnCalendarAsync(scheduler, clinic, colleague, to, to.AddDays(6), Token));
    }

    [Fact]
    public async Task Time_off_in_the_target_week_refuses_the_copy()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var from = FarMonday(6);
        var to = from.AddDays(7);

        await RosterAsync(scheduler, clinic, clinic.StaffId, from);
        (await scheduler.PostAsJsonAsync("/staff/schedules/time-off", new
        {
            staff_id = clinic.StaffId,
            starts_at = At(to, 0),
            ends_at = At(to, 23),
            reason = "ลาพักร้อน",
        }, Token)).EnsureSuccessStatusCode();

        var response = await CopyAsync(scheduler, new { branch_id = clinic.BranchId, from_week = Day(from), to_week = Day(to) });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("staff_on_time_off", ErrorCode(await BodyAsync(response, Token)));
        Assert.Empty(await ShiftsOnCalendarAsync(scheduler, clinic, clinic.StaffId, to, to.AddDays(6), Token));
    }

    [Fact]
    public async Task Only_the_people_named_are_copied()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var colleague = await ColleagueAsync(clinic);
        var from = FarMonday(7);
        var to = from.AddDays(7);

        await RosterAsync(scheduler, clinic, clinic.StaffId, from);
        await RosterAsync(scheduler, clinic, colleague, from);

        var response = await CopyAsync(scheduler, new
        {
            branch_id = clinic.BranchId,
            from_week = Day(from),
            to_week = Day(to),
            staff_ids = new[] { colleague },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(3, (await BodyAsync(response, Token)).GetProperty("data").GetArrayLength());
        Assert.Empty(await ShiftsOnCalendarAsync(scheduler, clinic, clinic.StaffId, to, to.AddDays(6), Token));
    }

    [Fact]
    public async Task Copies_whose_start_has_passed_are_left_out()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var colleague = await ColleagueAsync(clinic);
        var lastWeek = MondayWeeksAhead(-1);
        var thisWeek = MondayWeeksAhead(0);

        // Every day of last week, 09:00 to 10:00: the copies before now are history.
        await CreateShiftsAsync(scheduler, new
        {
            staff_id = colleague,
            branch_id = clinic.BranchId,
            starts_at = At(lastWeek, 9),
            ends_at = At(lastWeek, 10),
            rrule = "FREQ=DAILY;COUNT=7",
        }, Token);

        var now = DateTimeOffset.UtcNow;
        var started = Enumerable.Range(0, 7).Count(day => At(thisWeek.AddDays(day), 9) < now);

        var response = await CopyAsync(scheduler, new
        {
            branch_id = clinic.BranchId,
            from_week = Day(lastWeek),
            to_week = Day(thisWeek),
            staff_ids = new[] { colleague },
        });

        if (started == 7)
        {
            // Sunday after nine: nothing of the week is still to come.
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            return;
        }

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await BodyAsync(response, Token);
        Assert.Equal(started, body.GetProperty("skipped_started").GetInt32());
        Assert.Equal(7 - started, body.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task Weeks_are_named_by_their_monday()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var from = FarMonday(8);

        var response = await CopyAsync(scheduler, new
        {
            branch_id = clinic.BranchId,
            from_week = Day(from.AddDays(1)),
            to_week = Day(from.AddDays(8)),
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Copying_needs_schedule_write()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.StaffOf(clinic, Scopes.Schedule.Read);
        var from = FarMonday(9);

        var response = await CopyAsync(reader, new { branch_id = clinic.BranchId, from_week = Day(from), to_week = Day(from.AddDays(7)) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Another_branch_and_its_people_are_out_of_reach()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);
        var from = FarMonday(10);

        var theirBranch = await CopyAsync(scheduler, new
        {
            branch_id = elsewhere.BranchId,
            from_week = Day(from),
            to_week = Day(from.AddDays(7)),
        });
        var theirPerson = await CopyAsync(scheduler, new
        {
            branch_id = clinic.BranchId,
            from_week = Day(from),
            to_week = Day(from.AddDays(7)),
            staff_ids = new[] { elsewhere.StaffId },
        });

        Assert.Equal(HttpStatusCode.Forbidden, theirBranch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, theirPerson.StatusCode);
    }
}
