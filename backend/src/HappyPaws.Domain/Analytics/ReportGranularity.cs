namespace HappyPaws.Domain.Analytics;

/// <summary>How a report's days are grouped into the rows of its series.</summary>
public enum ReportGranularity
{
    /// <summary>One row per clinic day.</summary>
    Day,

    /// <summary>One row per week, starting on Monday as the Thai calendar and ISO 8601 do.</summary>
    Week,

    /// <summary>One row per calendar month.</summary>
    Month,
}
