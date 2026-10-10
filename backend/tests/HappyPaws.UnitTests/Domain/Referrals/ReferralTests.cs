using HappyPaws.Domain.Common;
using HappyPaws.Domain.Crm;
using HappyPaws.Domain.Referrals;

namespace HappyPaws.UnitTests.Domain.Referrals;

/// <summary>
/// A referral is recorded once, rewarded at most once, and its points move balances but never
/// levels (<c>docs/contract-extensions.md § 39</c>).
/// </summary>
public class ReferralTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 3, 0, 0, TimeSpan.Zero);

    private static Referral Pending() =>
        Referral.Record("cus_referrer", "cus_friend", "7KX4PM2Q", ReferralChannels.GuestBooking, null, Now);

    [Fact]
    public void A_qualifying_sign_up_is_pending_until_the_first_paid_bill()
    {
        var referral = Pending();

        Assert.Equal(ReferralStatus.Pending, referral.Status);
        Assert.StartsWith("rfr_", referral.Id);
        Assert.Null(referral.RejectReason);
        Assert.Null(referral.RewardedAt);
        Assert.Equal(0, referral.ReferrerPoints);
    }

    [Fact]
    public void A_sign_up_that_does_not_qualify_is_recorded_as_rejected_with_its_reason()
    {
        var referral = Referral.Record(
            "cus_referrer", "cus_friend", "7KX4PM2Q", ReferralChannels.Register, ReferralRejectReasons.SharedContact, Now);

        Assert.Equal(ReferralStatus.Rejected, referral.Status);
        Assert.Equal(ReferralRejectReasons.SharedContact, referral.RejectReason);
        Assert.Equal(Now, referral.RejectedAt);
    }

    [Fact]
    public void Rewarding_keeps_the_bill_the_points_and_the_cap_on_the_row()
    {
        var referral = Pending();

        referral.Reward("inv_1", 0, 100, overMonthlyCap: true, Now);

        Assert.Equal(ReferralStatus.Rewarded, referral.Status);
        Assert.Equal("inv_1", referral.RewardInvoiceId);
        Assert.Equal(0, referral.ReferrerPoints);
        Assert.Equal(100, referral.FriendPoints);
        Assert.True(referral.OverMonthlyCap);
        Assert.Equal(Now, referral.RewardedAt);
    }

    [Fact]
    public void A_referral_is_rewarded_once()
    {
        var referral = Pending();
        referral.Reward("inv_1", 100, 100, overMonthlyCap: false, Now);

        var again = Assert.Throws<DomainException>(() => referral.Reward("inv_2", 100, 100, overMonthlyCap: false, Now));
        Assert.Equal("state_conflict", again.Code);
        Assert.Throws<DomainException>(() => referral.Reject(ReferralRejectReasons.ProgrammeOff, Now));
        Assert.Equal("inv_1", referral.RewardInvoiceId);
    }

    [Fact]
    public void A_rejected_referral_cannot_be_rewarded_later()
    {
        var referral = Pending();
        referral.Reject(ReferralRejectReasons.ProgrammeOff, Now);

        Assert.Throws<DomainException>(() => referral.Reward("inv_1", 100, 100, overMonthlyCap: false, Now));
        Assert.Equal(ReferralRejectReasons.ProgrammeOff, referral.RejectReason);
    }

    [Fact]
    public void Only_known_reasons_are_accepted()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Referral.Record("cus_a", "cus_b", "7KX4PM2Q", ReferralChannels.GuestBooking, "because", Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Pending().Reject("because", Now));
    }

    [Fact]
    public void Referral_points_are_positive_move_the_balance_and_not_the_level()
    {
        var entry = LoyaltyEntry.Create("cus_friend", LoyaltyEntryKind.Referral, -100, Now, referralId: "rfr_1");

        Assert.Equal(100, entry.Points);
        Assert.Equal("rfr_1", entry.ReferralId);

        var position = LoyaltyBalance.Compute([entry], Now.AddMinutes(1));
        Assert.Equal(100, position.Balance);
        Assert.Equal(0, position.Lifetime);
    }

    [Fact]
    public void Only_a_referral_row_carries_a_referral()
    {
        Assert.Null(LoyaltyEntry.Create("cus_1", LoyaltyEntryKind.Adjust, 5, Now, referralId: "rfr_1").ReferralId);
    }
}
