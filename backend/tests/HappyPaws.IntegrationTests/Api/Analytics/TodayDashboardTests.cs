using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.AnalyticsScenario;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Analytics;

/// <summary><c>GET /staff/dashboard/today</c>: today's cards, and the two that depend on who asks.</summary>
[Collection(nameof(ApiCollection))]
public class TodayDashboardTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_cards_count_todays_queue_cases_and_appointments_at_the_branch()
    {
        // A case open on a checked-in patient, and a walk-in still waiting behind them.
        var open = await ConsultationScenario.OpenCaseAsync(fixture, Token);
        var clinic = open.Clinic;
        var past = await AdvancedBookingSeed.PastAppointmentAsync(fixture, clinic, Token);
        var (_, otherPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        using (var counter = fixture.Vet(clinic))
        using (var walkIn = Idempotent("/staff/queue/tickets", new { pet_id = otherPet, service_id = clinic.ServiceId, branch_id = clinic.BranchId }))
        {
            (await counter.SendAsync(walkIn, Token)).EnsureSuccessStatusCode();
        }

        // The booked visit is three hours out, which after 21:00 in Bangkok is tomorrow.
        var appointments = await ClinicSeed.AppointmentsAtAsync(fixture, clinic.BranchId, Token);
        var startingToday = appointments.Count(appointment =>
            ClinicSeed.ClinicDate(appointment.StartsAt) == Today.ToString("yyyy-MM-dd"));
        Assert.Contains(appointments, appointment => appointment.Id == past);

        using var vet = fixture.Reader(clinic, Scopes.Dashboard.Read);
        var dashboard = await ReadAsync(vet, $"/staff/dashboard/today?branch={clinic.BranchId}", Token);

        Assert.Equal(Today.ToString("yyyy-MM-dd"), dashboard.GetProperty("date").GetString());
        Assert.Equal(clinic.BranchId, dashboard.GetProperty("branch_id").GetString());
        Assert.Equal(startingToday, dashboard.GetProperty("appointments").GetProperty("total").GetInt32());

        var queue = dashboard.GetProperty("queue");
        Assert.Equal(2, queue.GetProperty("issued").GetInt32());
        Assert.Equal(1, queue.GetProperty("waiting").GetInt32());
        Assert.Equal(1, queue.GetProperty("in_room").GetInt32());
        Assert.True(queue.GetProperty("average_wait_minutes").GetDecimal() >= 0);
        Assert.True(queue.GetProperty("longest_wait_minutes").GetInt32() >= 0);

        Assert.Equal(1, dashboard.GetProperty("consultations").GetProperty("open").GetInt32());
        Assert.Equal(0, dashboard.GetProperty("consultations").GetProperty("closed_today").GetInt32());

        // The vet's own day: the case they have open, and nobody waiting for them by name.
        var mine = dashboard.GetProperty("mine");
        Assert.Equal(1, mine.GetProperty("open_consultations").GetInt32());
        Assert.Equal(0, mine.GetProperty("queue_waiting").GetInt32());
    }

    [Fact]
    public async Task Todays_takings_appear_only_for_a_caller_who_may_already_see_the_money()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await BillingScenario.RaiseIdAsync(cashier, clinic, Token);
        await PayAsync(cashier, invoiceId, 100_000, "cash", Token);
        await PayAsync(cashier, invoiceId, 46_000, "card", Token);
        await RefundAsync(cashier, invoiceId, 6_000, "cash", Token);

        var path = $"/staff/dashboard/today?branch={clinic.BranchId}";

        using var receptionist = fixture.Reader(clinic, Scopes.Dashboard.Read);
        var without = await ReadAsync(receptionist, path, Token);
        Assert.False(without.TryGetProperty("revenue", out _));

        using var cashDesk = fixture.Reader(clinic, Scopes.Dashboard.Read, Scopes.Invoice.Write);
        var revenue = (await ReadAsync(cashDesk, path, Token)).GetProperty("revenue");

        Assert.Equal(146_000, revenue.GetProperty("collected_satang").GetInt64());
        Assert.Equal(6_000, revenue.GetProperty("refunded_satang").GetInt64());
        Assert.Equal(140_000, revenue.GetProperty("net_satang").GetInt64());
        Assert.Equal(2, revenue.GetProperty("payments").GetInt32());

        using var analyst = fixture.Reader(clinic, Scopes.Dashboard.Read, Scopes.Analytics.Read);
        Assert.True((await ReadAsync(analyst, path, Token)).TryGetProperty("revenue", out _));
    }
}
