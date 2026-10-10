using HappyPaws.Application.Forecasting.Inventory;

namespace HappyPaws.UnitTests.Application.Forecasting;

/// <summary>How long stock lasts and what to order (<c>docs/contract-extensions.md § 33</c>).</summary>
public class InventoryForecastRulesTests
{
    [Fact]
    public void An_average_is_units_over_the_window()
    {
        Assert.Equal(2.0, InventoryForecastRules.DailyAverage(used: 60, windowDays: 30, historyDays: 400));
        Assert.Equal(1.0, InventoryForecastRules.DailyAverage(used: 90, windowDays: 90, historyDays: 90));
    }

    [Fact]
    public void A_new_item_is_averaged_over_the_days_it_has_been_stocked_not_the_whole_window()
    {
        // Stocked ten days ago and 20 used: two a day, not 20 / 90.
        Assert.Equal(2.0, InventoryForecastRules.DailyAverage(used: 20, windowDays: 90, historyDays: 10));
    }

    [Theory]
    [InlineData(0, 30, 100)]
    [InlineData(10, 30, 0)]
    public void No_use_or_no_history_averages_zero(int used, int window, int history)
    {
        Assert.Equal(0, InventoryForecastRules.DailyAverage(used, window, history));
    }

    [Fact]
    public void The_rate_is_the_busier_of_the_two_averages()
    {
        Assert.Equal(3.0, InventoryForecastRules.DailyRate(3.0, 1.5));
        Assert.Equal(2.5, InventoryForecastRules.DailyRate(1.0, 2.5));
    }

    [Fact]
    public void Days_until_out_round_down_and_are_absent_when_nothing_is_used()
    {
        Assert.Equal(6, InventoryForecastRules.DaysUntilOut(available: 20, rate: 3));
        Assert.Equal(0, InventoryForecastRules.DaysUntilOut(available: 0, rate: 3));
        Assert.Equal(0, InventoryForecastRules.DaysUntilOut(available: -4, rate: 3));
        Assert.Null(InventoryForecastRules.DaysUntilOut(available: 20, rate: 0));
    }

    [Fact]
    public void Floating_point_dust_does_not_cost_a_day()
    {
        // 3 / 0.1 is 29.999999999999996 in binary floating point; the stock lasts 30 days.
        Assert.Equal(30, InventoryForecastRules.DaysUntilOut(available: 3, rate: 0.1));
    }

    [Fact]
    public void An_order_covers_the_lead_time_and_the_cover_period_less_what_is_held_and_on_order()
    {
        // 2 a day for 7 + 30 days is 74; 20 on the shelf and 10 on the way leaves 44.
        Assert.Equal(44, InventoryForecastRules.SuggestedOrder(rate: 2, available: 20, onOrder: 10, coverDays: 30));
        Assert.Equal(7, InventoryForecastRules.LeadTimeDays);
    }

    [Fact]
    public void An_order_rounds_up_to_whole_units_without_floating_point_dust()
    {
        // A third a day for 37 days is 12.33, so 13. And 0.1 a day for 30 days is 3, not 4.
        Assert.Equal(13, InventoryForecastRules.SuggestedOrder(rate: 1.0 / 3, available: 0, onOrder: 0, coverDays: 30));
        Assert.Equal(3, InventoryForecastRules.SuggestedOrder(rate: 0.1, available: 0, onOrder: 0, coverDays: 30, leadTimeDays: 0));
    }

    [Fact]
    public void Enough_stock_or_no_use_orders_nothing()
    {
        Assert.Equal(0, InventoryForecastRules.SuggestedOrder(rate: 1, available: 500, onOrder: 0, coverDays: 30));
        Assert.Equal(0, InventoryForecastRules.SuggestedOrder(rate: 0, available: 0, onOrder: 0, coverDays: 30));
    }

    [Theory]
    [InlineData(0, ForecastConfidence.Low)]
    [InlineData(13, ForecastConfidence.Low)]
    [InlineData(14, ForecastConfidence.Medium)]
    [InlineData(89, ForecastConfidence.Medium)]
    [InlineData(90, ForecastConfidence.High)]
    public void Fewer_than_fourteen_days_of_history_is_low_confidence(int historyDays, ForecastConfidence expected)
    {
        Assert.Equal(expected, InventoryForecastRules.ConfidenceOf(historyDays));
    }

    [Fact]
    public void History_counts_whole_days_up_to_yesterday()
    {
        var today = new DateOnly(2026, 10, 10);

        Assert.Equal(5, InventoryForecastRules.HistoryDays(today.AddDays(-5), today));
        Assert.Equal(0, InventoryForecastRules.HistoryDays(today, today));
        Assert.Equal(0, InventoryForecastRules.HistoryDays(null, today));
    }
}
