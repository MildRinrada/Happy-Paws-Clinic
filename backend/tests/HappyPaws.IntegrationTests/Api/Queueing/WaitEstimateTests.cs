using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Queueing;

/// <summary>
/// <c>GET /queue/wait-estimate</c> (<c>docs/contract-extensions.md § 37</c>): open to anyone, counts
/// only, one branch at a time, closed on a holiday, and reused for thirty seconds.
/// <para>
/// The shared database never sets the clinic's weekly hours (the tests that do run in a fresh
/// instance), so a branch here is open while its seeded vet is on shift, which is now.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class WaitEstimateTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    [Fact]
    public async Task Anyone_gets_the_counts_and_a_ten_minute_range_with_a_short_public_cache()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await WaitEstimateScenario.WaitingAsync(fixture, clinic, 3, cancellationToken: Token);
        using var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync(WaitEstimateScenario.Path(clinic), Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("public, max-age=30", response.Headers.CacheControl?.ToString());

        var body = await BodyAsync(response);
        Assert.Equal(clinic.BranchId, body.GetProperty("branch_id").GetString());
        Assert.True(body.GetProperty("open_now").GetBoolean());
        Assert.Equal(3, body.GetProperty("waiting").GetInt32());
        Assert.Equal(1, body.GetProperty("vets_on_duty").GetInt32());

        // No timed visits yet, so 20 minutes each: 3 × 20 ÷ 1 = 60.
        Assert.Equal(60, body.GetProperty("estimate_minutes_low").GetInt32());
        Assert.Equal(70, body.GetProperty("estimate_minutes_high").GetInt32());
        Assert.True(body.TryGetProperty("updated_at", out _));
    }

    [Fact]
    public async Task The_branch_s_own_median_time_in_the_room_is_used_once_it_has_enough_visits()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await WaitEstimateScenario.ServedAsync(fixture, clinic, 5, minutesInRoom: 35, firstSequence: 100, Token);
        await WaitEstimateScenario.WaitingAsync(fixture, clinic, 2, cancellationToken: Token);
        using var anonymous = fixture.CreateClient();

        var body = await BodyAsync(await anonymous.GetAsync(WaitEstimateScenario.Path(clinic), Token));

        // Served tickets are not waiting; 2 × 35 ÷ 1 = 70.
        Assert.Equal(2, body.GetProperty("waiting").GetInt32());
        Assert.Equal(70, body.GetProperty("estimate_minutes_low").GetInt32());
        Assert.Equal(80, body.GetProperty("estimate_minutes_high").GetInt32());
    }

    [Fact]
    public async Task Nobody_waiting_is_zero_to_ten()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var anonymous = fixture.CreateClient();

        var body = await BodyAsync(await anonymous.GetAsync(WaitEstimateScenario.Path(clinic), Token));

        Assert.Equal(0, body.GetProperty("waiting").GetInt32());
        Assert.Equal(0, body.GetProperty("estimate_minutes_low").GetInt32());
        Assert.Equal(10, body.GetProperty("estimate_minutes_high").GetInt32());
    }

    [Fact]
    public async Task The_body_holds_counts_only_and_nothing_about_who_is_waiting()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var tickets = await WaitEstimateScenario.WaitingAsync(fixture, clinic, 2, cancellationToken: Token);
        using var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync(WaitEstimateScenario.Path(clinic), Token);
        var text = await response.Content.ReadAsStringAsync(Token);
        var body = JsonDocument.Parse(text).RootElement;

        Assert.Equal(
            ["branch_id", "estimate_minutes_high", "estimate_minutes_low", "open_now", "updated_at", "vets_on_duty", "waiting"],
            body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));

        foreach (var ticket in tickets)
        {
            Assert.DoesNotContain(ticket.Id, text, StringComparison.Ordinal);
            Assert.DoesNotContain(ticket.Number, text, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("tkt_", text, StringComparison.Ordinal);
        Assert.DoesNotContain(clinic.PetId, text, StringComparison.Ordinal);
        Assert.DoesNotContain("ลูน่า", text, StringComparison.Ordinal);
        Assert.DoesNotContain(clinic.CustomerId, text, StringComparison.Ordinal);
        Assert.DoesNotContain(clinic.StaffId, text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_clinic_with_several_branches_must_name_one_and_an_unknown_branch_is_refused()
    {
        // The shared database holds a branch per test already, so there are several.
        await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var anonymous = fixture.CreateClient();

        var missing = await anonymous.GetAsync("/queue/wait-estimate", Token);
        var unknown = await anonymous.GetAsync("/queue/wait-estimate?branch=br_nowhere", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, missing.StatusCode);
        Assert.Equal("branch", (await BodyAsync(missing)).GetProperty("error").GetProperty("field").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
        Assert.Equal("branch", (await BodyAsync(unknown)).GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_branch_shut_by_a_holiday_today_is_closed_with_no_estimate()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await WaitEstimateScenario.HolidayTodayAsync(fixture, clinic, Token);
        using var anonymous = fixture.CreateClient();

        var body = await BodyAsync(await anonymous.GetAsync(WaitEstimateScenario.Path(clinic), Token));

        Assert.False(body.GetProperty("open_now").GetBoolean());
        Assert.False(body.TryGetProperty("estimate_minutes_low", out _));
        Assert.False(body.TryGetProperty("estimate_minutes_high", out _));

        // The weekly hours were never set, so there is no "opens at" to give.
        Assert.False(body.TryGetProperty("opens_at", out _));
    }

    [Fact]
    public async Task An_answer_is_reused_for_thirty_seconds()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await WaitEstimateScenario.WaitingAsync(fixture, clinic, 1, cancellationToken: Token);
        using var anonymous = fixture.CreateClient();

        var first = await BodyAsync(await anonymous.GetAsync(WaitEstimateScenario.Path(clinic), Token));
        await WaitEstimateScenario.WaitingAsync(fixture, clinic, 2, firstSequence: 2, cancellationToken: Token);
        var second = await BodyAsync(await anonymous.GetAsync(WaitEstimateScenario.Path(clinic), Token));

        Assert.Equal(1, second.GetProperty("waiting").GetInt32());
        Assert.Equal(first.GetProperty("updated_at").GetString(), second.GetProperty("updated_at").GetString());
    }
}

/// <summary>
/// The per-address cap on the wait estimate (<c>RateLimits:WaitEstimatePerIpPerMinute</c>, two in
/// <see cref="WaitEstimateRateLimitFixture"/>): a third read in the minute is <c>429</c>.
/// </summary>
[Collection(nameof(WaitEstimateRateLimitCollection))]
public class WaitEstimateRateLimitTests(WaitEstimateRateLimitFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_third_read_in_a_minute_from_one_address_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var anonymous = fixture.CreateClient();
        var path = WaitEstimateScenario.Path(clinic);

        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(path, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(path, Token)).StatusCode);

        var refused = await anonymous.GetAsync(path, Token);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal(
            "rate_limited",
            (await refused.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("code").GetString());
    }
}
