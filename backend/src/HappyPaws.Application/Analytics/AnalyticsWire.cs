using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Analytics;

namespace HappyPaws.Application.Analytics;

/// <summary>The <c>?granularity=</c> vocabulary of the stats and analytics reads.</summary>
public static class AnalyticsWire
{
    /// <summary>Parses <c>day</c>, <c>week</c> or <c>month</c>; <paramref name="fallback"/> when absent.</summary>
    public static ReportGranularity ParseGranularity(string? value, ReportGranularity fallback) =>
        value?.Trim().ToLowerInvariant() switch
        {
            null or "" => fallback,
            "day" => ReportGranularity.Day,
            "week" => ReportGranularity.Week,
            "month" => ReportGranularity.Month,
            _ => throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["granularity"] = ["granularity ต้องเป็น day, week หรือ month"],
            }),
        };

    public static string ToWire(ReportGranularity granularity) => granularity.ToString().ToLowerInvariant();
}
