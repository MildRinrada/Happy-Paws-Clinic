using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Appointments;

/// <summary>One place on the internet can hold only so many times, and guess only so many codes.</summary>
[Collection(nameof(GuestBookingLimitsCollection))]
public class GuestBookingLimitTests(GuestBookingLimitsFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string NewPhone() => $"06{Random.Shared.Next(10_000_000, 99_999_999)}";

    [Fact]
    public async Task One_address_can_hold_only_so_many_times_and_try_only_so_many_codes()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.CreateClient();
        string? holdId = null;
        var phone = NewPhone();

        for (var i = 0; i < GuestBookingLimitsFixture.PerHour; i++)
        {
            phone = NewPhone();
            var allowed = await client.PostAsJsonAsync(
                "/booking/guest/start",
                new { phone, service_id = clinic.ServiceId, branch_id = clinic.BranchId, starts_at = ClinicSeed.SlotAt(5 + i) },
                Token);

            Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
            holdId = (await allowed.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("hold").GetProperty("id").GetString();
        }

        var over = await client.PostAsJsonAsync(
            "/booking/guest/start",
            new { phone = NewPhone(), service_id = clinic.ServiceId, branch_id = clinic.BranchId, starts_at = ClinicSeed.SlotAt(9) },
            Token);

        Assert.Equal(HttpStatusCode.TooManyRequests, over.StatusCode);

        // Guesses are capped the same way, whichever number they are for.
        var guess = new { hold_id = holdId, phone, code = "000000", display_name = "x", accept_privacy_notice = true };

        for (var i = 0; i < GuestBookingLimitsFixture.PerHour; i++)
        {
            var refused = await client.PostAsJsonAsync("/booking/guest/verify", guess, Token);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, refused.StatusCode);
        }

        var throttled = await client.PostAsJsonAsync("/booking/guest/verify", guess, Token);
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
    }
}
