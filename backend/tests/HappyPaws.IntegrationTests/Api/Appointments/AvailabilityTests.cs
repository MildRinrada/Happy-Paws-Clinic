using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Appointments;

/// <summary>
/// The public booking feed and the ten-minute hold that takes a slot off it.
/// </summary>
[Collection(nameof(ApiCollection))]
public class AvailabilityTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<JsonElement> FeedAsync(SeededClinic clinic, DateTimeOffset day)
    {
        // No Authorization header at all: the contract marks this endpoint public, and a booking
        // page has to work before anyone signs in.
        using var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync(
            $"/booking/availability?service={clinic.ServiceId}&date={ClinicSeed.ClinicDate(day)}&branch={clinic.BranchId}",
            Token);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static IEnumerable<DateTimeOffset> StartTimes(JsonElement feed) =>
        feed.GetProperty("slots").EnumerateArray().Select(slot => slot.GetProperty("starts_at").GetDateTimeOffset());

    [Fact]
    public async Task Availability_is_public_and_lists_slots_for_the_rostered_vet()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        // Asked for the day a known-bookable slot falls on, not for "today". Late in a clinic
        // evening the lead time legitimately consumes what is left of the day and today's feed is
        // correctly empty — a test that assumed otherwise would fail after about 22:00 Bangkok and
        // pass every morning.
        var feed = await FeedAsync(clinic, ClinicSeed.SlotAt(6));

        Assert.Equal(clinic.ServiceId, feed.GetProperty("service_id").GetString());
        Assert.Equal(30, feed.GetProperty("duration_minutes").GetInt32());

        var slots = feed.GetProperty("slots").EnumerateArray().ToList();
        Assert.NotEmpty(slots);
        Assert.All(slots, slot => Assert.Equal(clinic.StaffId, slot.GetProperty("staff_id").GetString()));
    }

    [Fact]
    public async Task Availability_never_offers_a_slot_inside_the_booking_lead_time()
    {
        // Lead time is two hours by default and the seeded shift starts an hour ago, so a feed
        // that ignored it would offer slots that have already passed.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        var feed = await FeedAsync(clinic, ClinicSeed.SlotAt(6));

        Assert.All(
            StartTimes(feed),
            start => Assert.True(start >= DateTimeOffset.UtcNow.AddMinutes(119), $"{start} is inside the lead time"));
    }

    [Fact]
    public async Task Availability_is_bounded_to_the_day_that_was_asked_for()
    {
        // The seeded shift runs a full twenty-four hours, so it spills into the next clinic day.
        // Without the clamp, Monday's feed would quietly include Tuesday's small hours.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var day = ClinicSeed.SlotAt(6);

        var feed = await FeedAsync(clinic, day);

        Assert.All(
            StartTimes(feed),
            start => Assert.Equal(ClinicSeed.ClinicDate(day), ClinicSeed.ClinicDate(start)));
    }

    [Fact]
    public async Task A_service_closed_to_online_booking_is_invisible_to_the_public_feed()
    {
        // Answered as 404, not 403: the feed must not be usable to enumerate the internal
        // service list.
        var clinic = await ClinicSeed.ClinicAsync(fixture, onlineBookingEnabled: false, cancellationToken: Token);
        using var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync(
            $"/booking/availability?service={clinic.ServiceId}&date={ClinicSeed.ClinicDate(DateTimeOffset.UtcNow)}&branch={clinic.BranchId}",
            Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_hold_lasts_ten_minutes_and_takes_the_slot_off_everyone_elses_board()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var startsAt = ClinicSeed.SlotAt(4);

        using var customer = fixture.CustomerOf(clinic);

        var held = await customer.PostAsJsonAsync(
            "/booking/holds",
            new { service_id = clinic.ServiceId, starts_at = startsAt, branch_id = clinic.BranchId },
            Token);

        Assert.Equal(HttpStatusCode.Created, held.StatusCode);

        var hold = await held.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.StartsWith("hold_", hold.GetProperty("id").GetString());
        Assert.InRange(
            hold.GetProperty("expires_at").GetDateTimeOffset(),
            DateTimeOffset.UtcNow.AddMinutes(9),
            DateTimeOffset.UtcNow.AddMinutes(11));

        Assert.DoesNotContain(startsAt, StartTimes(await FeedAsync(clinic, startsAt)));
    }
}
