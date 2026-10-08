using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Domain.Analytics;

namespace HappyPaws.Application.Analytics.GetRetentionAnalytics;

/// <summary><c>GET /staff/analytics/retention?from=&amp;to=&amp;branch=</c>.</summary>
/// <param name="From">Any day in the first month; five months before <paramref name="To"/>'s month when absent.</param>
/// <param name="To">Last clinic day counted; today when absent.</param>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
public sealed record GetRetentionAnalyticsQuery(DateOnly? From = null, DateOnly? To = null, string? BranchId = null);

/// <summary>
/// "ลูกค้าใหม่/กลับมา · cohort รายเดือน".
/// <para>
/// A <b>visit</b> is a consultation opened for the customer or an invoice issued to them that was
/// not voided. Both, because a grooming customer who never sees a vet is still a customer who came
/// back. A customer is <b>new</b> in the month of their first visit ever, looking back past the
/// start of the report, and <b>returning</b> in every month after it. Customers merged into another
/// record count as the record they were merged into.
/// </para>
/// <para>
/// With a branch named, "first visit" means the first visit <i>at that branch</i>: a customer who
/// moves from one branch to another is new to the second.
/// </para>
/// </summary>
public sealed class GetRetentionAnalyticsHandler(IBusinessAnalyticsQueries business, ReportWindows windows)
{
    public const int DefaultMonths = 6;

    public async Task<RetentionAnalyticsDto> HandleAsync(
        GetRetentionAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        var window = windows.ResolveMonths(query.From, query.To, query.BranchId, DefaultMonths);
        var cells = await business.RetentionAsync(window, cancellationToken);

        var months = RetentionMatrix.Months(cells, window.First, window.Last)
            .Select(month => new RetentionMonthDto(month.Month, month.Active, month.New, month.Returning))
            .ToList();

        var cohorts = RetentionMatrix.Cohorts(cells, window.First, window.Last)
            .Select(cohort => new RetentionCohortDto(
                cohort.Cohort,
                cohort.Customers,
                cohort.Retained,
                [.. cohort.Retained.Select(count => ReportMath.Rate(count, cohort.Customers))]))
            .ToList();

        return new RetentionAnalyticsDto(window.First, window.Last, window.BranchId, months, cohorts);
    }
}
