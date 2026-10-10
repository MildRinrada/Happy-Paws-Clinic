using System.Net;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Forecasting;

/// <summary>
/// <c>GET /staff/appointments/attendance-risk</c> (<c>docs/contract-extensions.md § 33</c>): the
/// day's open bookings, each judged from the customer's own no-shows and late cancellations, the
/// lead time and confirmation, with the reasons in Thai.
/// </summary>
[Collection(nameof(ApiCollection))]
public class AttendanceRiskTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_customer_who_missed_twice_is_high_risk_with_the_reasons_and_a_reliable_one_is_low()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var reliable = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var now = DateTimeOffset.UtcNow;
        var day = ForecastScenario.Today.AddDays(2);

        // The clinic's customer: two no-shows and a late cancellation this year, one no-show two years ago.
        foreach (var daysAgo in new[] { 40, 100, 800 })
        {
            await ForecastScenario.AppointmentAsync(fixture, clinic, now.AddDays(-daysAgo), appointment => appointment.MarkNoShow(now), cancellationToken: Token);
        }

        await ForecastScenario.AppointmentAsync(
            fixture, clinic, now.AddDays(-20), appointment => appointment.Cancel(now.AddDays(-21), "usr_owner", "ติดธุระ", isLate: true), cancellationToken: Token);

        var risky = await ForecastScenario.AppointmentAsync(fixture, clinic, ForecastScenario.At(day, 10), cancellationToken: Token);
        var safe = await ForecastScenario.AppointmentAsync(
            fixture, clinic, ForecastScenario.At(day, 11), appointment => appointment.Confirm(now), reliable.CustomerId, reliable.PetId, cancellationToken: Token);

        // Not open bookings: a cancelled visit and a walk-in.
        await ForecastScenario.AppointmentAsync(
            fixture, clinic, ForecastScenario.At(day, 12), appointment => appointment.Cancel(now, "usr_test", "ยกเลิก"), cancellationToken: Token);
        await ForecastScenario.AppointmentAsync(fixture, clinic, ForecastScenario.At(day, 13), walkIn: true, cancellationToken: Token);

        using var client = fixture.StaffOf(clinic, Scopes.Appointment.Read);
        var answer = await OkAsync(
            await client.GetAsync($"/staff/appointments/attendance-risk?branch={clinic.BranchId}&date={day:yyyy-MM-dd}", Token), Token);

        Assert.Equal(365, answer.GetProperty("window_days").GetInt32());

        var rows = answer.GetProperty("data").EnumerateArray().ToList();
        Assert.Equal([risky.Id, safe.Id], rows.Select(row => row.GetProperty("appointment_id").GetString()));

        var high = rows[0];
        Assert.Equal("high", high.GetProperty("risk").GetString());
        Assert.Equal("booked", high.GetProperty("status").GetString());
        Assert.Equal("ลูน่า", high.GetProperty("pet_name").GetString());
        Assert.Equal(clinic.CustomerId, high.GetProperty("customer_id").GetString());
        Assert.Equal(
            ["ไม่มาตามนัด 2 ครั้งใน 12 เดือนที่ผ่านมา", "ยกเลิกกระชั้น 1 ครั้งใน 12 เดือนที่ผ่านมา", "เจ้าของยังไม่ได้ยืนยันนัด"],
            high.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
        Assert.Equal(2, high.GetProperty("signals").GetProperty("no_shows").GetInt32());
        Assert.Equal(2, high.GetProperty("signals").GetProperty("days_booked_ahead").GetInt32());
        Assert.Equal(6, high.GetProperty("score").GetInt32());

        var low = rows[1];
        Assert.Equal("low", low.GetProperty("risk").GetString());
        Assert.Equal("confirmed", low.GetProperty("status").GetString());
        Assert.Empty(low.GetProperty("reasons").EnumerateArray());
    }

    [Fact]
    public async Task Without_a_branch_a_branch_bound_caller_sees_their_own_branches_only()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var day = ForecastScenario.Today.AddDays(1);

        var ours = await ForecastScenario.AppointmentAsync(fixture, mine, ForecastScenario.At(day, 9), cancellationToken: Token);
        var other = await ForecastScenario.AppointmentAsync(fixture, theirs, ForecastScenario.At(day, 9), cancellationToken: Token);

        using var client = fixture.StaffOf(mine, Scopes.Appointment.Read);
        var answer = await OkAsync(await client.GetAsync($"/staff/appointments/attendance-risk?date={day:yyyy-MM-dd}", Token), Token);
        var ids = answer.GetProperty("data").EnumerateArray().Select(row => row.GetProperty("appointment_id").GetString()).ToList();

        Assert.Contains(ours.Id, ids);
        Assert.DoesNotContain(other.Id, ids);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.GetAsync($"/staff/appointments/attendance-risk?branch={theirs.BranchId}&date={day:yyyy-MM-dd}", Token)).StatusCode);
    }

    [Fact]
    public async Task A_past_day_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.StaffOf(clinic, Scopes.Appointment.Read);

        var response = await client.GetAsync(
            $"/staff/appointments/attendance-risk?branch={clinic.BranchId}&date={ForecastScenario.Today.AddDays(-1):yyyy-MM-dd}", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token)).RootElement;
        Assert.Equal("date", body.GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task It_needs_a_session_and_appointment_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/appointments/attendance-risk", Token)).StatusCode);

        using var writer = fixture.StaffOf(clinic, Scopes.Appointment.Write);
        var refused = await writer.GetAsync($"/staff/appointments/attendance-risk?branch={clinic.BranchId}", Token);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("insufficient_scope", await ErrorCodeAsync(refused, Token));
    }
}
