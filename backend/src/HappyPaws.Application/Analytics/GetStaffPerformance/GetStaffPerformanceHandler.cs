using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Domain.Analytics;

namespace HappyPaws.Application.Analytics.GetStaffPerformance;

/// <summary><c>GET /staff/analytics/staff-performance?from=&amp;to=&amp;branch=</c>.</summary>
/// <param name="From">First clinic day; 29 days before <paramref name="To"/> when absent.</param>
/// <param name="To">Last clinic day, included; today when absent.</param>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
public sealed record GetStaffPerformanceQuery(DateOnly? From = null, DateOnly? To = null, string? BranchId = null);

/// <summary>
/// "เคส รายได้ และคะแนนรีวิวต่อคน", behind <c>analytics:hr</c> because it ranks people.
/// <para>
/// Everyone active at the branches in view is listed, with zeros where they did nothing, plus anyone
/// else who did something there. A ranking that silently dropped the people with no cases would hide
/// exactly the row a manager is looking for.
/// </para>
/// <para>
/// <b>Revenue</b> is the revenue report's cash basis narrowed to the bills a vet's cases produced
/// (payments less refunds inside the span). Money at the desk is reported separately as
/// <c>collected_satang</c>, since the cashier who took a payment did not earn it.
/// </para>
/// <para>
/// <b>Review scores</b> are not reported: nothing in the system collects a review yet, and a score
/// of zero would read as a bad one.
/// </para>
/// </summary>
public sealed class GetStaffPerformanceHandler(IBusinessAnalyticsQueries business, ReportWindows windows)
{
    public const int DefaultDays = 30;

    public async Task<StaffPerformanceDto> HandleAsync(
        GetStaffPerformanceQuery query,
        CancellationToken cancellationToken = default)
    {
        var window = windows.Resolve(query.From, query.To, query.BranchId, DefaultDays);
        var rows = await business.StaffPerformanceAsync(window, cancellationToken);

        var staff = rows
            .OrderByDescending(row => row.Cases)
            .ThenByDescending(row => row.AttributedRevenueSatang)
            .ThenBy(row => row.DisplayName, StringComparer.Ordinal)
            .Select(row => new StaffPerformanceEntryDto(
                row.StaffId,
                row.DisplayName,
                row.StaffType,
                row.Cases,
                ReportMath.AverageMinutes(row.CaseSeconds, row.Cases),
                row.Treatments,
                row.AttributedRevenueSatang,
                row.CollectedSatang))
            .ToList();

        return new StaffPerformanceDto(window.First, window.Last, window.BranchId, staff);
    }
}
