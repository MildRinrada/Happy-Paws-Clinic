using HappyPaws.Application.Analytics.Dtos;
using HappyPaws.Domain.Analytics;

namespace HappyPaws.Application.Analytics.GetWorkloadStats;

/// <summary><c>GET /staff/stats/workload?from=&amp;to=&amp;branch=</c>.</summary>
/// <param name="From">First clinic day; 29 days before <paramref name="To"/> when absent.</param>
/// <param name="To">Last clinic day, included; today when absent.</param>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
public sealed record GetWorkloadStatsQuery(DateOnly? From = null, DateOnly? To = null, string? BranchId = null);

/// <summary>
/// "เคสต่อหมอ เวลาเฉลี่ยต่อเคส งานค้าง".
/// <para>
/// A <b>case</b> is a consultation closed inside the span, credited to the vet who ran it, and its
/// time is opened to closed. <b>งานค้าง</b> is what is waiting on each vet <i>now</i> whatever span
/// was asked for: cases still open, and today's tickets assigned to them that have not gone in yet.
/// A backlog is a fact about the present; "the backlog last March" is a different question that
/// these tables do not keep the history to answer.
/// </para>
/// </summary>
public sealed class GetWorkloadStatsHandler(IOperationalStatsQueries operations, ReportWindows windows)
{
    public const int DefaultDays = 30;

    public async Task<WorkloadStatsDto> HandleAsync(
        GetWorkloadStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var window = windows.Resolve(query.From, query.To, query.BranchId, DefaultDays);
        var rows = await operations.WorkloadAsync(window, windows.Today, cancellationToken);

        var vets = rows.Where(row => row.StaffId is not null).ToList();

        var totals = new WorkloadTotalsDto(
            vets.Sum(row => row.Cases),
            ReportMath.AverageMinutes(vets.Sum(row => row.CaseSeconds), vets.Sum(row => row.Cases)),
            vets.Sum(row => row.OpenCases),
            rows.Where(row => row.StaffId is null).Sum(row => row.WaitingQueue));

        var staff = vets
            .OrderByDescending(row => row.Cases)
            .ThenByDescending(row => row.OpenCases + row.WaitingQueue)
            .ThenBy(row => row.DisplayName ?? row.StaffId, StringComparer.Ordinal)
            .Select(row => new WorkloadStaffDto(
                row.StaffId!,
                row.DisplayName,
                row.Cases,
                ReportMath.AverageMinutes(row.CaseSeconds, row.Cases),
                row.Appointments,
                row.OpenCases,
                row.WaitingQueue))
            .ToList();

        return new WorkloadStatsDto(window.First, window.Last, window.BranchId, totals, staff);
    }
}
