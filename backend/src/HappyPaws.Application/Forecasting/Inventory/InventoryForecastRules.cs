namespace HappyPaws.Application.Forecasting.Inventory;

/// <summary>How sure a stock forecast is, from how long the item has a ledger at the branch.</summary>
public enum ForecastConfidence
{
    /// <summary>Fewer than <see cref="InventoryForecastRules.LowConfidenceDays"/> days of history.</summary>
    Low,

    /// <summary>Enough for the short average, not yet for the long one.</summary>
    Medium,

    /// <summary>At least <see cref="InventoryForecastRules.LongWindowDays"/> days of history.</summary>
    High,
}

/// <summary>
/// The arithmetic of <c>GET /staff/inventory/forecast</c> (<c>docs/contract-extensions.md § 33</c>).
/// Pure: the handler hands in counts, this hands back numbers, and the unit tests pin each rule.
/// <list type="bullet">
/// <item><b>Usage</b> is what left the shelf for use: <c>issue</c> (used in a procedure, broken,
/// thrown away) and <c>dispense</c>. Not transfers, which move stock rather than use it, and not
/// count adjustments, which correct the books rather than record demand.</item>
/// <item><b>Average per day</b> over the last 30 and 90 whole clinic days (today, half over, is
/// left out), divided by the days the item actually had a ledger at the branch when that is
/// shorter, so a drug stocked a week ago is not averaged as though it sat unused for months.</item>
/// <item><b>Rate</b> is the higher of the two averages: running out of a drug costs more than
/// holding a few extra boxes.</item>
/// <item><b>Days until out</b> is usable units over the rate, rounded down; none when nothing was
/// used.</item>
/// <item><b>Suggested order</b> covers the lead time plus the cover period at the rate, less what is
/// usable and what is already on order, never below zero.</item>
/// </list>
/// </summary>
public static class InventoryForecastRules
{
    public const int ShortWindowDays = 30;
    public const int LongWindowDays = 90;

    /// <summary>Below this many days of ledger history the forecast is <see cref="ForecastConfidence.Low"/>.</summary>
    public const int LowConfidenceDays = 14;

    /// <summary>Days between ordering and the goods arriving, assumed for every supplier.</summary>
    public const int LeadTimeDays = 7;

    public const int DefaultCoverDays = 30;
    public const int MaxCoverDays = 180;

    /// <summary>Kills floating-point dust before rounding up: 0.1 × 30 must order 3, not 4.</summary>
    private const double Epsilon = 1e-9;

    /// <summary>Units per day over a window, counting only the days the item had a ledger.</summary>
    /// <param name="used">Units used inside the window.</param>
    /// <param name="windowDays">The window's length.</param>
    /// <param name="historyDays">Whole days since the item's first ledger row, up to yesterday.</param>
    public static double DailyAverage(int used, int windowDays, int historyDays)
    {
        var days = Math.Min(windowDays, historyDays);

        return days <= 0 || used <= 0 ? 0 : (double)used / days;
    }

    public static double DailyRate(double shortAverage, double longAverage) => Math.Max(shortAverage, longAverage);

    /// <summary>Whole days the usable stock lasts at the rate; null when nothing is being used.</summary>
    public static int? DaysUntilOut(int available, double rate) =>
        rate <= 0 ? null : (int)Math.Floor((Math.Max(available, 0) / rate) + Epsilon);

    /// <summary>Units to order now to last the lead time and the cover period.</summary>
    public static int SuggestedOrder(double rate, int available, int onOrder, int coverDays, int leadTimeDays = LeadTimeDays)
    {
        if (rate <= 0)
        {
            return 0;
        }

        var needed = (int)Math.Ceiling((rate * (leadTimeDays + coverDays)) - Epsilon);

        return Math.Max(0, needed - Math.Max(available, 0) - Math.Max(onOrder, 0));
    }

    public static ForecastConfidence ConfidenceOf(int historyDays) => historyDays switch
    {
        < LowConfidenceDays => ForecastConfidence.Low,
        < LongWindowDays => ForecastConfidence.Medium,
        _ => ForecastConfidence.High,
    };

    /// <summary>Whole clinic days from the first ledger day up to yesterday; zero when there is none.</summary>
    public static int HistoryDays(DateOnly? firstMovementDay, DateOnly today) =>
        firstMovementDay is { } first ? Math.Max(0, today.DayNumber - first.DayNumber) : 0;

    /// <summary>Two decimals, for the wire.</summary>
    public static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
