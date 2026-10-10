using System.Net;
using System.Net.Http.Json;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Reviews;

/// <summary>
/// <c>POST /reviews/invitations/{token}</c> is capped per address (<c>docs/contract-extensions.md § 36</c>),
/// in a host whose cap is two an hour. Every attempt counts, an unknown token included: the cap is
/// on how fast one place can try links.
/// </summary>
[Collection(nameof(GuestBookingLimitsCollection))]
public class ReviewSubmitLimitTests(GuestBookingLimitsFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_third_attempt_in_an_hour_from_one_address_is_rate_limited()
    {
        using var anonymous = fixture.CreateClient();
        var path = "/reviews/invitations/hprv_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        for (var attempt = 0; attempt < GuestBookingLimitsFixture.PerHour; attempt++)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsJsonAsync(path, ReviewScenario.Body(), Token)).StatusCode);
        }

        var limited = await anonymous.PostAsJsonAsync(path, ReviewScenario.Body(), Token);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);

        // Reading a link is not what is capped.
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(path, Token)).StatusCode);
    }
}
