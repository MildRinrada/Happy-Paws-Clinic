using System.Net;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Referrals;

/// <summary>
/// <c>GET /referrals/{code}</c> is capped per address (<c>docs/contract-extensions.md § 39</c>), in a
/// host whose cap is two a minute. Every check counts, an unknown code included: the cap is on how
/// fast one place can try codes.
/// </summary>
[Collection(nameof(ReferralCheckLimitCollection))]
public class ReferralCheckLimitTests(ReferralCheckLimitFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_third_check_in_a_minute_from_one_address_is_rate_limited()
    {
        using var anonymous = fixture.CreateClient();

        for (var attempt = 0; attempt < ReferralCheckLimitFixture.PerMinute; attempt++)
        {
            Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/referrals/ZZZZZZZ{attempt + 2}", Token)).StatusCode);
        }

        var limited = await anonymous.GetAsync("/referrals/ZZZZZZZZ", Token);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}
