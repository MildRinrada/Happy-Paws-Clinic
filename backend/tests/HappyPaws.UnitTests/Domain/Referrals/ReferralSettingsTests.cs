using HappyPaws.Domain.Common;
using HappyPaws.Domain.Crm;

namespace HappyPaws.UnitTests.Domain.Referrals;

/// <summary>
/// The referral rules inside the points programme: off until the clinic turns them on, 100 and 100
/// points and five a month by default, within bounds (<c>docs/contract-extensions.md § 39</c>).
/// </summary>
public class ReferralSettingsTests
{
    [Fact]
    public void A_clinic_that_never_saved_has_the_programme_off_with_the_owners_defaults()
    {
        var settings = LoyaltySettings.CreateDefault();

        Assert.False(settings.ReferralEnabled);
        Assert.Equal(100, settings.ReferralReferrerPoints);
        Assert.Equal(100, settings.ReferralFriendPoints);
        Assert.Equal(5, settings.ReferralMonthlyCap);
    }

    [Fact]
    public void The_referral_rules_are_replaced_as_a_set()
    {
        var settings = LoyaltySettings.CreateDefault();

        settings.ReplaceReferral(enabled: true, referrerPoints: 0, friendPoints: 50, monthlyCap: 1);

        Assert.True(settings.ReferralEnabled);
        Assert.Equal(0, settings.ReferralReferrerPoints);
        Assert.Equal(50, settings.ReferralFriendPoints);
        Assert.Equal(1, settings.ReferralMonthlyCap);
    }

    [Theory]
    [InlineData(-1, 100, 5)]
    [InlineData(10_001, 100, 5)]
    [InlineData(100, -1, 5)]
    [InlineData(100, 100, 0)]
    [InlineData(100, 100, 101)]
    public void Values_outside_their_bounds_are_refused(int referrer, int friend, int cap)
    {
        var settings = LoyaltySettings.CreateDefault();

        var refused = Assert.Throws<DomainException>(() => settings.ReplaceReferral(true, referrer, friend, cap));

        Assert.Equal("invalid_loyalty_settings", refused.Code);
        Assert.False(settings.ReferralEnabled);
    }
}
