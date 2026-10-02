using HappyPaws.Domain.Common;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.UnitTests.Domain.ServiceCatalog;

/// <summary>
/// Pricing and availability are the two parts of a service with dates and ranges in them, which is
/// where the off-by-one lives: the day a price starts, the weight a band ends at, the minute a
/// booking window closes.
/// </summary>
public class ClinicServiceTests
{
    private static readonly DateOnly Today = new(2026, 9, 30);

    private static ClinicService Checkup(long priceSatang = 50_000) =>
        ClinicService.Create("checkup", "ตรวจสุขภาพ", ServiceCategories.Checkup, 30, priceSatang);

    [Fact]
    public void A_service_with_no_dated_price_charges_what_it_was_created_with()
    {
        var pricing = Checkup().PricingOn(Today);

        Assert.Equal(50_000, pricing.BasePriceSatang);
        Assert.Empty(pricing.Tiers);
    }

    [Fact]
    public void A_price_dated_tomorrow_does_not_change_todays_quote()
    {
        var service = Checkup();

        service.SchedulePricing(Today.AddDays(1), 60_000, []);

        Assert.Equal(50_000, service.PricingOn(Today).BasePriceSatang);
        Assert.Equal(60_000, service.PricingOn(Today.AddDays(1)).BasePriceSatang);
        Assert.Single(service.PricingAfter(Today));
    }

    [Fact]
    public void The_newest_price_that_has_started_is_the_one_in_force()
    {
        var service = Checkup();

        service.SchedulePricing(Today.AddDays(10), 70_000, []);
        service.SchedulePricing(Today, 55_000, []);
        service.SchedulePricing(Today.AddDays(5), 60_000, []);

        Assert.Equal(55_000, service.PricingOn(Today.AddDays(4)).BasePriceSatang);
        Assert.Equal(60_000, service.PricingOn(Today.AddDays(5)).BasePriceSatang);
        Assert.Equal(70_000, service.PricingOn(Today.AddDays(30)).BasePriceSatang);
    }

    [Fact]
    public void Re_sending_a_date_replaces_that_price_list_rather_than_stacking_a_second()
    {
        var service = Checkup();

        service.SchedulePricing(Today, 60_000, []);
        service.SchedulePricing(Today, 65_000, []);

        Assert.Single(service.PriceSchedule);
        Assert.Equal(65_000, service.PricingOn(Today).BasePriceSatang);
    }

    [Fact]
    public void A_tier_prices_by_weight_with_the_upper_bound_exclusive()
    {
        var service = Checkup();

        service.SchedulePricing(Today, 50_000,
        [
            new PriceTier(null, null, 10m, 40_000),
            new PriceTier(null, 10m, null, 80_000),
        ]);

        var pricing = service.PricingOn(Today);

        // Ten kilos is "ten and over", not both bands.
        Assert.Equal(40_000, pricing.PriceFor("สุนัข", 9.9m));
        Assert.Equal(80_000, pricing.PriceFor("สุนัข", 10m));
    }

    [Fact]
    public void A_tier_prices_by_kind_of_animal_regardless_of_case()
    {
        var service = Checkup();

        service.SchedulePricing(Today, 50_000, [new PriceTier("Cat", null, null, 35_000)]);

        var pricing = service.PricingOn(Today);

        Assert.Equal(35_000, pricing.PriceFor("cat", null));
        Assert.Equal(50_000, pricing.PriceFor("dog", null));
    }

    [Fact]
    public void An_animal_nobody_weighed_falls_through_to_the_base_price()
    {
        var service = Checkup();

        service.SchedulePricing(Today, 50_000, [new PriceTier(null, 0m, 10m, 40_000)]);

        // Guessing a band would be a price nobody can justify.
        Assert.Equal(50_000, service.PricingOn(Today).PriceFor("สุนัข", null));
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, 10, 5)]
    [InlineData(null, -1, 5)]
    public void A_tier_that_describes_nothing_or_an_impossible_band_is_refused(
        string? species,
        int? minWeight,
        int? maxWeight)
    {
        Assert.Throws<DomainException>(() => Checkup().SchedulePricing(
            Today, 50_000, [new PriceTier(species, minWeight, maxWeight, 40_000)]));
    }

    [Fact]
    public void A_service_with_no_branch_list_is_offered_everywhere()
    {
        Assert.True(Checkup().IsOfferedAt("brn_anywhere"));
    }

    [Fact]
    public void A_restricted_service_is_offered_only_where_it_says()
    {
        var service = Checkup();

        service.RestrictToBranches(["brn_a"]);

        Assert.True(service.IsOfferedAt("brn_a"));
        Assert.False(service.IsOfferedAt("brn_b"));
    }

    [Fact]
    public void A_booking_window_includes_its_start_and_excludes_its_end()
    {
        var service = Checkup();

        service.SetBookingWindows([new BookingWindow(DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(12, 0))]);

        // 30 September 2026 is a Wednesday.
        Assert.True(service.IsBookableOnlineAt(new DateTime(2026, 9, 30, 9, 0, 0)));
        Assert.True(service.IsBookableOnlineAt(new DateTime(2026, 9, 30, 11, 59, 0)));
        Assert.False(service.IsBookableOnlineAt(new DateTime(2026, 9, 30, 12, 0, 0)));
        Assert.False(service.IsBookableOnlineAt(new DateTime(2026, 10, 1, 9, 0, 0)));
    }

    [Fact]
    public void A_service_with_no_windows_is_bookable_at_any_time()
    {
        Assert.True(Checkup().IsBookableOnlineAt(new DateTime(2026, 9, 30, 3, 0, 0)));
    }

    [Fact]
    public void Only_the_six_categories_the_contract_names_are_known()
    {
        Assert.Equal(6, ServiceCategories.All.Count);
        Assert.True(ServiceCategories.IsKnown("grooming"));
        Assert.False(ServiceCategories.IsKnown("boarding"));
    }
}
