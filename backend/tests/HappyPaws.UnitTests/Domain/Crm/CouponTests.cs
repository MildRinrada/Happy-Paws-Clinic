using HappyPaws.Domain.Common;
using HappyPaws.Domain.Crm;

namespace HappyPaws.UnitTests.Domain.Crm;

/// <summary>"โค้ดส่วนลด · จำกัดบริการ จำนวน และช่วงเวลา": the code, the amount, and the three limits.</summary>
public class CouponTests
{
    private static readonly DateOnly From = new(2026, 10, 1);
    private static readonly DateOnly Until = new(2026, 10, 31);

    private static Coupon Percent(decimal percent = 10, int? max = null, int? perCustomer = null, string? branchId = null) =>
        Coupon.Create("vaccine10", "ลดค่าวัคซีน", percent, null, [], max, perCustomer, From, Until, branchId, "stf_owner");

    [Fact]
    public void Codes_are_matched_in_upper_case_without_spaces()
    {
        var coupon = Percent();

        Assert.Equal("VACCINE10", coupon.Code);
        Assert.Equal("VACCINE10", Coupon.NormalizeCode("  vaccine10 "));
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("HAS SPACE")]
    [InlineData("ลดราคา")]
    [InlineData("THIS-CODE-IS-FAR-TOO-LONG")]
    public void A_code_that_cannot_be_typed_at_the_counter_is_refused(string code) =>
        Assert.Throws<DomainException>(() =>
            Coupon.Create(code, "x", 10, null, [], null, null, From, Until, null, "stf_owner"));

    [Fact]
    public void A_generated_code_is_well_formed_and_avoids_look_alike_characters()
    {
        for (var i = 0; i < 50; i++)
        {
            var code = Coupon.GenerateCode();

            Assert.True(Coupon.IsWellFormedCode(code));
            Assert.DoesNotContain(code, character => character is '0' or 'O' or '1' or 'I' or 'L' or 'U');
        }
    }

    [Fact]
    public void A_coupon_is_a_rate_or_an_amount_never_both()
    {
        Assert.Throws<DomainException>(() =>
            Coupon.Create("BOTH1", "x", 10, 5_000, [], null, null, From, Until, null, "stf_owner"));
        Assert.Throws<DomainException>(() =>
            Coupon.Create("NONE1", "x", null, null, [], null, null, From, Until, null, "stf_owner"));
    }

    [Fact]
    public void A_rate_rounds_down_and_an_amount_is_capped_at_what_it_applies_to()
    {
        var rate = Percent(12.5m);
        var amount = Coupon.Create("FLAT100", "ลด 100", null, 10_000, [], null, null, From, Until, null, "stf_owner");

        Assert.Equal(1_249, rate.DiscountFor(9_999));
        Assert.Equal(10_000, amount.DiscountFor(50_000));
        Assert.Equal(6_000, amount.DiscountFor(6_000));
        Assert.Equal(0, amount.DiscountFor(0));
    }

    [Fact]
    public void Outside_its_dates_a_coupon_is_refused()
    {
        var coupon = Percent();

        Assert.Equal("coupon_not_started", Assert.Throws<DomainException>(() => coupon.EnsureRedeemable(From.AddDays(-1), "brn_1", 0)).Code);
        Assert.Equal("coupon_expired", Assert.Throws<DomainException>(() => coupon.EnsureRedeemable(Until.AddDays(1), "brn_1", 0)).Code);

        coupon.EnsureRedeemable(From, "brn_1", 0);
        coupon.EnsureRedeemable(Until, "brn_1", 0);
    }

    [Fact]
    public void A_single_use_coupon_is_used_up_after_one_use_and_comes_back_when_released()
    {
        var coupon = Percent(max: 1);

        coupon.EnsureRedeemable(From, "brn_1", 0);
        coupon.Redeem();

        Assert.Equal("coupon_used_up", Assert.Throws<DomainException>(() => coupon.EnsureRedeemable(From, "brn_1", 0)).Code);

        coupon.Release();
        coupon.EnsureRedeemable(From, "brn_1", 0);
    }

    [Fact]
    public void The_per_customer_limit_and_the_branch_are_enforced()
    {
        var coupon = Percent(perCustomer: 1, branchId: "brn_1");

        Assert.Equal("coupon_customer_limit", Assert.Throws<DomainException>(() => coupon.EnsureRedeemable(From, "brn_1", 1)).Code);
        Assert.Equal("coupon_not_applicable", Assert.Throws<DomainException>(() => coupon.EnsureRedeemable(From, "brn_2", 0)).Code);
    }
}
