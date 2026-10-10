using System.Net;
using System.Net.Http.Json;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Queueing;

/// <summary>
/// Trading a setup code for a display token is open to anyone, so guesses are capped per address
/// (<c>docs/contract-extensions.md § 30</c>). Run against the fixture whose cap is two an hour.
/// </summary>
[Collection(nameof(GuestBookingLimitsCollection))]
public class DisplayClaimLimitTests(GuestBookingLimitsFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Guessing_setup_codes_is_capped_per_address()
    {
        using var anonymous = fixture.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt <= GuestBookingLimitsFixture.PerHour; attempt++)
        {
            var response = await anonymous.PostAsJsonAsync("/queue/displays/claim", new { code = "AAAA-AAAA" }, Token);
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(GuestBookingLimitsFixture.PerHour), status => Assert.Equal(HttpStatusCode.UnprocessableEntity, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }
}
