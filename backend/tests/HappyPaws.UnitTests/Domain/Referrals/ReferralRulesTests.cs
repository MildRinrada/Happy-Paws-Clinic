using HappyPaws.Domain.Referrals;

namespace HappyPaws.UnitTests.Domain.Referrals;

/// <summary>
/// Who counts as a referred friend, and what the person who invited them gets under the monthly cap
/// (<c>docs/contract-extensions.md § 39</c>).
/// </summary>
public class ReferralRulesTests
{
    private static readonly ReferralParty Referrer = new("cus_referrer", "0812345678", "somchai@example.test");

    private static ReferralParty Friend(string? phone = "0898765432", string? email = null, string id = "cus_friend") =>
        new(id, phone, email);

    [Fact]
    public void A_new_customer_with_their_own_contact_details_qualifies()
    {
        Assert.Null(ReferralRules.Judge(Referrer, Friend(), friendIsNew: true));
        Assert.Null(ReferralRules.Judge(Referrer, Friend(email: "friend@example.test"), friendIsNew: true));
    }

    [Fact]
    public void A_customer_cannot_use_their_own_code()
    {
        Assert.Equal(
            ReferralRejectReasons.SelfReferral,
            ReferralRules.Judge(Referrer, Friend(Referrer.Phone, id: Referrer.CustomerId), friendIsNew: false));
    }

    [Fact]
    public void Someone_who_was_already_a_customer_is_not_a_new_friend()
    {
        Assert.Equal(ReferralRejectReasons.NotNewCustomer, ReferralRules.Judge(Referrer, Friend(), friendIsNew: false));
    }

    [Theory]
    [InlineData("0812345678")]
    [InlineData("081-234-5678")]
    [InlineData("+66 81 234 5678")]
    [InlineData("66812345678")]
    public void A_friend_on_the_referrers_phone_number_is_rejected_however_it_is_written(string phone)
    {
        Assert.Equal(ReferralRejectReasons.SharedContact, ReferralRules.Judge(Referrer, Friend(phone), friendIsNew: true));
    }

    [Theory]
    [InlineData("somchai@example.test")]
    [InlineData(" SOMCHAI@Example.TEST ")]
    public void A_friend_with_the_referrers_email_is_rejected_whatever_the_case(string email)
    {
        Assert.Equal(ReferralRejectReasons.SharedContact, ReferralRules.Judge(Referrer, Friend(email: email), friendIsNew: true));
    }

    [Fact]
    public void Missing_contact_details_never_match()
    {
        var bare = new ReferralParty("cus_bare", null, null);

        Assert.Null(ReferralRules.Judge(bare, Friend(phone: null), friendIsNew: true));
        Assert.False(ReferralRules.SamePhone(null, null));
        Assert.False(ReferralRules.SameEmail("", "  "));
    }

    [Fact]
    public void Short_numbers_match_only_when_every_digit_does()
    {
        Assert.True(ReferralRules.SamePhone("1669", "1669"));
        Assert.False(ReferralRules.SamePhone("1669", "21669"));
    }

    [Fact]
    public void Own_code_is_judged_before_anything_else()
    {
        // The owner using their own code is also not new and shares every contact; the reason
        // recorded is the one that explains it.
        var self = ReferralRules.Judge(Referrer, Referrer, friendIsNew: false);

        Assert.Equal(ReferralRejectReasons.SelfReferral, self);
    }

    [Theory]
    [InlineData(0, 5, 100, 100)]
    [InlineData(4, 5, 100, 100)]
    [InlineData(5, 5, 100, 0)]
    [InlineData(9, 5, 100, 0)]
    [InlineData(0, 1, 250, 250)]
    [InlineData(1, 1, 250, 0)]
    public void The_referrer_is_paid_until_the_months_cap_is_reached(int rewardedThisMonth, int cap, int points, int expected)
    {
        Assert.Equal(expected, ReferralRules.ReferrerPointsFor(rewardedThisMonth, cap, points));
    }
}
