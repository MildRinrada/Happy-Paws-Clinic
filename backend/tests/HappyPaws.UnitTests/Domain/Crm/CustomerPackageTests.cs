using HappyPaws.Domain.Common;
using HappyPaws.Domain.Crm;

namespace HappyPaws.UnitTests.Domain.Crm;

/// <summary>"แพ็กเกจตรวจ 4 ครั้ง · ตัดสิทธิ์อัตโนมัติเมื่อใช้": sold unpaid, usable once paid, spent and given back.</summary>
public class CustomerPackageTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 9, 10, 0, 0, TimeSpan.FromHours(7));
    private static readonly DateOnly Today = new(2026, 10, 9);

    private static CustomerPackage Package(DateOnly? expiresOn = null, string? petId = null) =>
        CustomerPackage.Sell("cus_1", "ตรวจสุขภาพ 4 ครั้ง", ["svc_checkup"], 4, 180_000, "brn_1", "stf_1", At, expiresOn, petId);

    [Fact]
    public void A_sold_package_waits_for_payment_and_cannot_be_used()
    {
        var package = Package();

        Assert.Equal(PackageStatus.PendingPayment, package.StatusOn(Today));
        Assert.Throws<DomainException>(() => package.Use(1, Today));
    }

    [Fact]
    public void Once_paid_it_is_spent_until_used_up()
    {
        var package = Package();
        package.Activate(At);

        package.Use(3, Today);
        Assert.Equal(1, package.Remaining);
        Assert.Equal(PackageStatus.Active, package.StatusOn(Today));

        package.Use(1, Today);
        Assert.Equal(PackageStatus.UsedUp, package.StatusOn(Today));
        Assert.Throws<DomainException>(() => package.Use(1, Today));
    }

    [Fact]
    public void More_uses_than_are_left_are_refused_whole()
    {
        var package = Package();
        package.Activate(At);

        Assert.Throws<DomainException>(() => package.Use(5, Today));
        Assert.Equal(0, package.UsedCount);
    }

    [Fact]
    public void The_expiry_day_is_the_last_day_it_can_be_used()
    {
        var package = Package(expiresOn: Today);
        package.Activate(At);

        Assert.True(package.IsUsableOn(Today));
        Assert.False(package.IsUsableOn(Today.AddDays(1)));
        Assert.Equal(PackageStatus.Expired, package.StatusOn(Today.AddDays(1)));
    }

    [Fact]
    public void A_cancelled_package_stays_cancelled_even_if_its_bill_is_later_paid()
    {
        var package = Package();
        package.Cancel("ยกเลิกบิลขายแพ็กเกจ", At);
        package.Activate(At);

        Assert.Equal(PackageStatus.Cancelled, package.StatusOn(Today));
        Assert.Null(package.ActivatedAt);
    }

    [Fact]
    public void Released_uses_go_back_but_never_below_zero()
    {
        var package = Package();
        package.Activate(At);
        package.Use(2, Today);

        package.Release(1);
        Assert.Equal(1, package.UsedCount);

        package.Release(5);
        Assert.Equal(0, package.UsedCount);
    }

    [Fact]
    public void A_package_for_one_pet_covers_only_that_pet()
    {
        var forLuna = Package(petId: "pet_luna");
        var anyPet = Package();

        Assert.True(forLuna.Covers("svc_checkup", "pet_luna"));
        Assert.False(forLuna.Covers("svc_checkup", "pet_milo"));
        Assert.False(forLuna.Covers("svc_vaccine", "pet_luna"));
        Assert.True(anyPet.Covers("svc_checkup", null));
    }

    [Fact]
    public void A_package_needs_a_service_and_a_sensible_number_of_uses()
    {
        Assert.Throws<DomainException>(() =>
            CustomerPackage.Sell("cus_1", "ว่าง", [], 4, 0, "brn_1", "stf_1", At));
        Assert.Throws<DomainException>(() =>
            CustomerPackage.Sell("cus_1", "เยอะเกิน", ["svc_1"], 1_001, 0, "brn_1", "stf_1", At));
        Assert.Throws<DomainException>(() =>
            CustomerPackage.Sell("cus_1", "ราคาติดลบ", ["svc_1"], 1, -1, "brn_1", "stf_1", At));
    }
}
