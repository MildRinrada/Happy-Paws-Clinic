using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.AnalyticsScenario;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Analytics;

/// <summary>
/// <c>/staff/stats/*</c>: the numbers come out right for data made through the real endpoints.
/// </summary>
[Collection(nameof(ApiCollection))]
public class OperationalStatsTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static int Int(JsonElement element, string name) => element.GetProperty(name).GetInt32();

    [Fact]
    public async Task Appointment_stats_count_outcomes_and_compute_both_rates()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);

        await BookAsStaffAsync(fixture, clinic, 3, Token);
        await BookAsStaffAsync(fixture, clinic, 4, Token);
        var cancelled = await BookAsStaffAsync(fixture, clinic, 5, Token);
        var missed = await AdvancedBookingSeed.PastAppointmentAsync(fixture, clinic, Token);

        (await staff.PostAsJsonAsync($"/appointments/{cancelled}/cancel", new { reason = "หมอป่วย" }, Token)).EnsureSuccessStatusCode();
        (await staff.PostAsync($"/staff/appointments/{missed}/no-show", null, Token)).EnsureSuccessStatusCode();

        using var reader = fixture.Reader(clinic, Scopes.Stats.Read);
        var stats = await ReadAsync(reader, $"/staff/stats/appointments?{Around(clinic)}", Token);
        var totals = stats.GetProperty("totals");

        Assert.Equal("day", stats.GetProperty("granularity").GetString());
        Assert.Equal(4, Int(totals, "total"));
        Assert.Equal(2, Int(totals, "upcoming"));
        Assert.Equal(1, Int(totals, "cancelled"));
        Assert.Equal(0, Int(totals, "late_cancelled"));
        Assert.Equal(1, Int(totals, "no_show"));
        Assert.Equal(0, Int(totals, "walk_in"));

        // Three were booked at the counter. The seeded past visit carries no booking staff, which is
        // what a portal booking looks like.
        Assert.Equal(1, Int(totals, "booked_online"));

        Assert.Equal(0.25m, totals.GetProperty("cancellation_rate").GetDecimal());

        // A cancelled appointment is one nobody was expected at: 1 no-show out of 3 expected.
        Assert.Equal(0.3333m, totals.GetProperty("no_show_rate").GetDecimal());

        var series = stats.GetProperty("series").EnumerateArray().ToList();
        Assert.Equal(3, series.Count);
        Assert.Equal(4, series.Sum(period => Int(period, "total")));
        Assert.Equal(1, series.Sum(period => Int(period, "no_show")));

        var weekly = await ReadAsync(reader, $"/staff/stats/appointments?{Around(clinic)}&granularity=week", Token);
        Assert.Equal("week", weekly.GetProperty("granularity").GetString());
        Assert.All(
            weekly.GetProperty("series").EnumerateArray(),
            period => Assert.Equal(DayOfWeek.Monday, DateOnly.Parse(period.GetProperty("period").GetString()!).DayOfWeek));
        Assert.Equal(4, weekly.GetProperty("series").EnumerateArray().Sum(period => Int(period, "total")));
    }

    [Fact]
    public async Task A_branch_bound_reader_without_a_branch_sees_only_their_own_branches()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        await AdvancedBookingSeed.PastAppointmentAsync(fixture, mine, Token);
        await AdvancedBookingSeed.PastAppointmentAsync(fixture, theirs, Token);
        await BookAsStaffAsync(fixture, theirs, 3, Token);

        using var reader = fixture.Reader(mine, Scopes.Stats.Read);
        var range = $"from={Today.AddDays(-1):yyyy-MM-dd}&to={Today.AddDays(1):yyyy-MM-dd}";
        var stats = await ReadAsync(reader, $"/staff/stats/appointments?{range}", Token);

        Assert.False(stats.TryGetProperty("branch_id", out _));
        Assert.Equal(1, Int(stats.GetProperty("totals"), "total"));
    }

    [Fact]
    public async Task Queue_stats_report_waits_peak_hour_and_what_is_still_outstanding()
    {
        // One patient seen from check-in to close; one walk-in still waiting.
        var open = await ConsultationScenario.OpenCaseAsync(fixture, Token);
        var clinic = open.Clinic;
        using var vet = fixture.Vet(clinic);

        await ConsultationScenario.DiagnoseAsync(vet, open.ConsultationId, Token);
        await ConsultationScenario.CloseAsync(vet, open.ConsultationId, Token);

        var (_, otherPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using (var walkIn = Idempotent("/staff/queue/tickets", new { pet_id = otherPet, service_id = clinic.ServiceId, branch_id = clinic.BranchId }))
        {
            (await vet.SendAsync(walkIn, Token)).EnsureSuccessStatusCode();
        }

        using var reader = fixture.Reader(clinic, Scopes.Stats.Read);
        var stats = await ReadAsync(reader, $"/staff/stats/queue?{Around(clinic)}", Token);
        var totals = stats.GetProperty("totals");

        Assert.Equal(2, Int(totals, "tickets"));
        Assert.Equal(1, Int(totals, "served"));
        Assert.Equal(1, Int(totals, "outstanding"));
        Assert.Equal(0, Int(totals, "left"));
        Assert.True(totals.GetProperty("average_wait_minutes").GetDecimal() >= 0);
        Assert.True(totals.GetProperty("average_room_minutes").GetDecimal() >= 0);

        var hours = stats.GetProperty("peak_hours").EnumerateArray().ToList();
        Assert.Equal(24, hours.Count);
        Assert.Equal(2, hours.Sum(hour => Int(hour, "tickets")));
        var peak = Int(totals, "peak_hour");
        Assert.Equal(hours.Max(hour => Int(hour, "tickets")), Int(hours[peak], "tickets"));

        Assert.Equal(2, stats.GetProperty("series").EnumerateArray().Sum(day => Int(day, "tickets")));
    }

    [Fact]
    public async Task Workload_counts_cases_per_vet_and_what_is_still_open()
    {
        var first = await ConsultationScenario.OpenCaseAsync(fixture, Token);
        var clinic = first.Clinic;
        using var vet = fixture.Vet(clinic);

        await ConsultationScenario.DiagnoseAsync(vet, first.ConsultationId, Token);
        await ConsultationScenario.CloseAsync(vet, first.ConsultationId, Token);

        // A second patient checked in and on the table, case still open.
        var (_, otherPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var booking = Idempotent("/staff/appointments", new
        {
            pet_id = otherPet,
            service_id = clinic.ServiceId,
            starts_at = ClinicSeed.SlotAt(6),
            branch_id = clinic.BranchId,
            staff_id = clinic.StaffId,
        });
        var booked = await vet.SendAsync(booking, Token);
        booked.EnsureSuccessStatusCode();
        var second = (await booked.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!;

        await ConsultationScenario.CheckInAsync(vet, second, Token);
        await ConsultationScenario.OpenAsync(vet, second, Token);

        using var reader = fixture.Reader(clinic, Scopes.Stats.Read);
        var stats = await ReadAsync(reader, $"/staff/stats/workload?{Around(clinic)}", Token);

        var row = stats.GetProperty("staff").EnumerateArray()
            .Single(person => person.GetProperty("staff_id").GetString() == clinic.StaffId);

        // Booking without a vet lets the slot resolver pick one, so read back who it picked.
        var withThisVet = (await ClinicSeed.AppointmentsAtAsync(fixture, clinic.BranchId, Token))
            .Count(appointment => appointment.StaffId == clinic.StaffId);

        Assert.Equal(1, Int(row, "cases"));
        Assert.Equal(1, Int(row, "open_cases"));
        Assert.Equal(withThisVet, Int(row, "appointments"));
        Assert.True(withThisVet >= 1);
        Assert.True(row.GetProperty("average_case_minutes").GetDecimal() >= 0);

        var totals = stats.GetProperty("totals");
        Assert.Equal(1, Int(totals, "cases"));
        Assert.Equal(1, Int(totals, "open_cases"));
    }
}
