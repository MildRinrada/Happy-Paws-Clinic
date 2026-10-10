using System.Globalization;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// A clinic whose per-address caps on booking without signing up (docs/contract-extensions.md
/// § 27) and on trading a waiting-room screen's setup code (§ 30) are two an hour, so they can be
/// reached in a test. Its own container: every test client is the same address, and the main
/// fixture lifts these caps for that reason. Each cap has its own bucket.
/// </summary>
public sealed class GuestBookingLimitsFixture : ApiFixture
{
    public const int PerHour = 2;

    protected override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>
    {
        ["RateLimits:GuestHoldsPerIpPerHour"] = PerHour.ToString(CultureInfo.InvariantCulture),
        ["RateLimits:GuestVerifiesPerIpPerHour"] = PerHour.ToString(CultureInfo.InvariantCulture),
        ["RateLimits:DisplayClaimsPerIpPerHour"] = PerHour.ToString(CultureInfo.InvariantCulture),
    };
}

[CollectionDefinition(nameof(GuestBookingLimitsCollection))]
public sealed class GuestBookingLimitsCollection : ICollectionFixture<GuestBookingLimitsFixture>;
