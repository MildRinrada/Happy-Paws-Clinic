using System.Text.Json.Serialization;

namespace HappyPaws.Application.Forecasting.Inventory;

/// <summary>
/// <c>GET /staff/inventory/forecast</c>: how long each item lasts at one branch and what to order.
/// Not in the API reference: <c>docs/contract-extensions.md § 33</c>.
/// </summary>
/// <param name="BranchId">The branch forecast.</param>
/// <param name="AsOf">The clinic day the forecast is worked out on.</param>
/// <param name="CoverDays">Days of use the suggested order covers after it arrives.</param>
/// <param name="LeadTimeDays">Days assumed between ordering and delivery.</param>
/// <param name="Data">Every item in use, soonest to run out first; items nobody uses last.</param>
public sealed record InventoryForecastDto(
    string BranchId,
    DateOnly AsOf,
    int CoverDays,
    int LeadTimeDays,
    IReadOnlyList<InventoryForecastItemDto> Data);

/// <summary>One item's forecast at the branch.</summary>
/// <remarks>
/// The four windowed names are spelled out, because the snake-case policy would write
/// <c>used30d</c>.
/// <list type="bullet">
/// <item><c>available</c>: usable units now (lots not expired), as <c>GET /staff/inventory/items</c> shows.</item>
/// <item><c>on_order</c>: units on open purchase orders for the branch.</item>
/// <item><c>used_30d</c>, <c>used_90d</c>: units issued or dispensed in the last 30 and 90 whole days.</item>
/// <item><c>avg_daily_30d</c>, <c>avg_daily_90d</c>: those over the days the item had a ledger, two decimals.</item>
/// <item><c>daily_rate</c>: the higher of the two averages, the rate the forecast uses.</item>
/// <item><c>days_until_out</c>, <c>runs_out_on</c>: null when nothing was used.</item>
/// <item><c>suggested_order_qty</c>: units to order to cover the lead time plus <c>cover_days</c>.</item>
/// <item><c>history_days</c>: whole days since the item's first ledger row at the branch.</item>
/// <item><c>confidence</c>: <c>low</c> under 14 days of history, <c>medium</c> under 90, else <c>high</c>.</item>
/// </list>
/// </remarks>
public sealed record InventoryForecastItemDto(
    string ItemId,
    string Name,
    string Kind,
    string Unit,
    int ReorderLevel,
    int Available,
    int OnOrder,
    [property: JsonPropertyName("used_30d")] int Used30d,
    [property: JsonPropertyName("used_90d")] int Used90d,
    [property: JsonPropertyName("avg_daily_30d")] double AvgDaily30d,
    [property: JsonPropertyName("avg_daily_90d")] double AvgDaily90d,
    double DailyRate,
    int? DaysUntilOut,
    DateOnly? RunsOutOn,
    int SuggestedOrderQty,
    int HistoryDays,
    string Confidence);
