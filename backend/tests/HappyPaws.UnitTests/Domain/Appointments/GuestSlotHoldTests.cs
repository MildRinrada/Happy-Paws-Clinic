using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Domain.Appointments;

/// <summary>
/// A hold for a visitor who has not signed in yet (docs/contract-extensions.md § 27): nobody can
/// redeem it until the number it was taken for is proved, and then only for that customer.
/// </summary>
public class GuestSlotHoldTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 3, 0, 0, TimeSpan.Zero);

    private static SlotHold GuestHold(string phone = "0812345678") => SlotHold.CreateForGuest(
        "brn_1", "svc_1", "stf_1", phone, Now.AddHours(4), Now.AddHours(4).AddMinutes(30), Now);

    [Fact]
    public void The_holder_is_a_marker_of_the_number_not_the_number()
    {
        var hold = GuestHold();

        Assert.StartsWith(SlotHold.GuestHolderPrefix, hold.HeldByCustomerId);
        Assert.DoesNotContain("0812345678", hold.HeldByCustomerId, StringComparison.Ordinal);
        Assert.True(hold.HeldByCustomerId.Length <= 40);
        Assert.Equal(SlotHold.GuestHolderFor("0812345678"), hold.HeldByCustomerId);
        Assert.NotEqual(SlotHold.GuestHolderFor("0812345679"), hold.HeldByCustomerId);
    }

    [Fact]
    public void No_customer_can_redeem_a_guest_hold_directly()
    {
        var hold = GuestHold();

        var refused = Assert.Throws<DomainException>(() => hold.Consume("cus_1", Now.AddMinutes(1)));

        Assert.Equal("hold_not_yours", refused.Code);
    }

    [Fact]
    public void The_proved_number_hands_the_hold_to_its_customer_who_can_then_redeem_it()
    {
        var hold = GuestHold();

        hold.ClaimForCustomer("0812345678", "cus_1", Now.AddMinutes(2));
        hold.Consume("cus_1", Now.AddMinutes(3));

        Assert.Equal("cus_1", hold.HeldByCustomerId);
        Assert.NotNull(hold.ConsumedAt);
        Assert.Equal(Now.Add(SlotHold.Lifetime), hold.ExpiresAt);
    }

    [Fact]
    public void Another_number_cannot_claim_it()
    {
        var hold = GuestHold();

        var refused = Assert.Throws<DomainException>(() => hold.ClaimForCustomer("0899999999", "cus_2", Now.AddMinutes(1)));

        Assert.Equal("hold_not_yours", refused.Code);
    }

    [Fact]
    public void A_lapsed_hold_cannot_be_claimed()
    {
        var hold = GuestHold();

        var refused = Assert.Throws<DomainException>(() => hold.ClaimForCustomer("0812345678", "cus_1", Now.Add(SlotHold.Lifetime)));

        Assert.Equal("hold_expired", refused.Code);
    }
}
