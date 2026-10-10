using System.Net;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.AnalyticsScenario;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Forecasting;

/// <summary>
/// <c>GET /staff/stats/demand-forecast</c> (<c>docs/contract-extensions.md § 33</c>): bookings,
/// walk-ins expected from the same weekday and hour, vets on the roster, and the high-load flag
/// against the branch's own history.
/// </summary>
[Collection(nameof(ApiCollection))]
public class DemandForecastTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Each_hour_has_bookings_expected_walk_ins_vets_and_a_flag_against_the_branchs_own_normal()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var today = ForecastScenario.Today;
        var day = today.AddDays(3);

        // Twelve past days with a vet in from 09:00 to 10:00 and one walk-in at 09:00: the branch's
        // normal is one patient per vet per hour. The branch's first ticket is twelve days ago.
        for (var ago = 1; ago <= 12; ago++)
        {
            await ForecastScenario.ShiftAsync(fixture, clinic, today.AddDays(-ago), 9, 10, Token);
            await ForecastScenario.WalkInsAsync(fixture, clinic, today.AddDays(-ago), 9, 1, Token);
        }

        // The same weekday one and two weeks back had 3 and 1 walk-ins at 10:00 (no vet in then).
        await ForecastScenario.WalkInsAsync(fixture, clinic, day.AddDays(-7), 10, 3, Token);
        await ForecastScenario.WalkInsAsync(fixture, clinic, day.AddDays(-14), 10, 1, Token);

        // On the day: a vet 09:00 to 12:00, two bookings at 10:00, one at 14:00 with nobody in,
        // and a cancelled and a walk-in booking that must not count.
        await ForecastScenario.ShiftAsync(fixture, clinic, day, 9, 12, Token);
        await ForecastScenario.AppointmentAsync(fixture, clinic, ForecastScenario.At(day, 10), cancellationToken: Token);
        await ForecastScenario.AppointmentAsync(fixture, clinic, ForecastScenario.At(day, 10, 30), cancellationToken: Token);
        await ForecastScenario.AppointmentAsync(fixture, clinic, ForecastScenario.At(day, 14), cancellationToken: Token);
        await ForecastScenario.AppointmentAsync(
            fixture, clinic, ForecastScenario.At(day, 11), appointment => appointment.Cancel(DateTimeOffset.UtcNow, "usr_test", "ไม่สะดวก"), cancellationToken: Token);
        await ForecastScenario.AppointmentAsync(fixture, clinic, ForecastScenario.At(day, 11), walkIn: true, cancellationToken: Token);

        using var client = fixture.Reader(clinic, Scopes.Stats.Read);
        var forecast = await ReadAsync(
            client, $"/staff/stats/demand-forecast?branch={clinic.BranchId}&from={day:yyyy-MM-dd}&to={day:yyyy-MM-dd}", Token);

        Assert.Equal(8, forecast.GetProperty("history_weeks").GetInt32());
        Assert.Equal(1.0, forecast.GetProperty("high_load_per_vet").GetDouble());

        var forecastDay = Assert.Single(forecast.GetProperty("days").EnumerateArray());
        Assert.Equal(day.ToString("yyyy-MM-dd"), forecastDay.GetProperty("date").GetString());

        var hours = forecastDay.GetProperty("hours").EnumerateArray().ToDictionary(hour => hour.GetProperty("hour").GetInt32());
        Assert.Equal([9, 10, 11, 14], hours.Keys.Order());

        // 09:00: the vet, and one walk-in on each of the two sampled weeks.
        Assert.Equal(1, hours[9].GetProperty("vets_on_shift").GetInt32());
        Assert.Equal(1.0, hours[9].GetProperty("expected_walk_ins").GetDouble());
        Assert.False(hours[9].GetProperty("high_load").GetBoolean());

        // 10:00: two booked and (3 + 1) / 2 walk-ins expected, four for one vet.
        Assert.Equal(2, hours[10].GetProperty("booked_appointments").GetInt32());
        Assert.Equal(2.0, hours[10].GetProperty("expected_walk_ins").GetDouble());
        Assert.Equal(4.0, hours[10].GetProperty("expected_total").GetDouble());
        Assert.Equal(4.0, hours[10].GetProperty("load_per_vet").GetDouble());
        Assert.True(hours[10].GetProperty("high_load").GetBoolean());
        Assert.Equal(ForecastScenario.At(day, 10), hours[10].GetProperty("starts_at").GetDateTimeOffset());

        // 11:00: a vet and nothing booked; the cancelled and walk-in bookings are not counted.
        Assert.Equal(0, hours[11].GetProperty("booked_appointments").GetInt32());
        Assert.False(hours[11].GetProperty("high_load").GetBoolean());

        // 14:00: a patient and no vet.
        Assert.Equal(0, hours[14].GetProperty("vets_on_shift").GetInt32());
        Assert.False(hours[14].TryGetProperty("load_per_vet", out _));
        Assert.True(hours[14].GetProperty("high_load").GetBoolean());

        Assert.Equal(3, forecastDay.GetProperty("booked_appointments").GetInt32());
        Assert.Equal(2, forecastDay.GetProperty("high_load_hours").GetInt32());
    }

    [Fact]
    public async Task A_week_from_today_at_the_callers_own_branch_by_default()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Reader(clinic, Scopes.Stats.Read);

        var forecast = await ReadAsync(client, "/staff/stats/demand-forecast", Token);

        Assert.Equal(clinic.BranchId, forecast.GetProperty("branch_id").GetString());
        Assert.Equal(ForecastScenario.Today.ToString("yyyy-MM-dd"), forecast.GetProperty("from").GetString());
        Assert.Equal(7, forecast.GetProperty("days").GetArrayLength());

        // No history yet: no line, so nothing can be above it.
        Assert.False(forecast.TryGetProperty("high_load_per_vet", out _));
    }

    [Theory]
    [InlineData(-1, 0, "from")]
    [InlineData(0, 14, "to")]
    [InlineData(3, 2, "to")]
    public async Task Past_days_spans_over_fourteen_days_and_reversed_spans_are_refused(int fromDays, int toDays, string field)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Reader(clinic, Scopes.Stats.Read);
        var today = ForecastScenario.Today;

        var response = await client.GetAsync(
            $"/staff/stats/demand-forecast?branch={clinic.BranchId}&from={today.AddDays(fromDays):yyyy-MM-dd}&to={today.AddDays(toDays):yyyy-MM-dd}",
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token)).RootElement;
        Assert.Equal(field, body.GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task It_needs_stats_read_and_the_callers_own_branch()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/stats/demand-forecast", Token)).StatusCode);

        using var noScope = fixture.Reader(mine, Scopes.Dashboard.Read, Scopes.Analytics.Read);
        var refused = await noScope.GetAsync($"/staff/stats/demand-forecast?branch={mine.BranchId}", Token);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("insufficient_scope", await ErrorCodeAsync(refused, Token));

        using var reader = fixture.Reader(mine, Scopes.Stats.Read);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync($"/staff/stats/demand-forecast?branch={theirs.BranchId}", Token)).StatusCode);
    }
}
