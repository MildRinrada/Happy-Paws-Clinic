using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Analytics;

/// <summary>
/// The span and the branches one report covers, already checked against the caller.
/// </summary>
/// <param name="First">First clinic day, included.</param>
/// <param name="Last">Last clinic day, included.</param>
/// <param name="From">Midnight in Bangkok at the start of <paramref name="First"/>.</param>
/// <param name="To">Midnight in Bangkok after <paramref name="Last"/>. Excluded.</param>
/// <param name="BranchId">The one branch asked for, when one was.</param>
/// <param name="BranchIds">
/// The branches to count, or null for every branch. Null only for a caller <see cref="BranchGuard"/>
/// does not restrict and who did not narrow the report to one branch.
/// </param>
public sealed record ReportWindow(
    DateOnly First,
    DateOnly Last,
    DateTimeOffset From,
    DateTimeOffset To,
    string? BranchId,
    IReadOnlyCollection<string>? BranchIds);

/// <summary>
/// Turns <c>?from=&amp;to=&amp;branch=</c> into a <see cref="ReportWindow"/>.
/// <para>
/// Days are clinic days. <c>?from=2026-10-01</c> starts at midnight in Bangkok, which is 17:00 UTC
/// the day before; a report that cut its days in UTC would book every payment taken before 07:00
/// to the previous day. <see cref="ClinicDays"/> is the same helper the roster uses.
/// </para>
/// <para>
/// The branch rule is every staff read's rule (<see cref="BranchGuard"/>): a named branch the caller
/// does not work at is <c>403</c>, and with no branch named a branch-bound caller sees the sum of
/// their own branches, never the clinic's.
/// </para>
/// </summary>
public sealed class ReportWindows(ClinicDays days, BranchGuard branchGuard)
{
    /// <summary>A year and a day, so "this day last year to today" fits.</summary>
    public const int LongestSpanDays = 366;

    /// <summary>The clinic's today.</summary>
    public DateOnly Today => days.Today;

    /// <summary>A report over a span of days ending today unless the caller says otherwise.</summary>
    /// <param name="from">First day asked for.</param>
    /// <param name="to">Last day asked for.</param>
    /// <param name="branchId">One branch, or null for every branch the caller may see.</param>
    /// <param name="defaultDays">How many days back from <paramref name="to"/> when <paramref name="from"/> is absent.</param>
    public ReportWindow Resolve(DateOnly? from, DateOnly? to, string? branchId, int defaultDays)
    {
        var last = to ?? (from is { } start && start > days.Today ? start : days.Today);
        var first = from ?? last.AddDays(1 - defaultDays);

        return Build(first, last, branchId);
    }

    /// <summary>
    /// A report counted in whole months: the first day moves back to the start of its month, and a
    /// missing <paramref name="from"/> means <paramref name="defaultMonths"/> months back.
    /// </summary>
    /// <param name="from">Any day in the first month.</param>
    /// <param name="to">Last day counted.</param>
    /// <param name="branchId">One branch, or null for every branch the caller may see.</param>
    /// <param name="defaultMonths">Months to cover, the current one included, when <paramref name="from"/> is absent.</param>
    public ReportWindow ResolveMonths(DateOnly? from, DateOnly? to, string? branchId, int defaultMonths)
    {
        var last = to ?? days.Today;
        var first = from ?? last.AddMonths(1 - defaultMonths);

        if (last < first)
        {
            throw Invalid("to", "วันสุดท้ายต้องไม่อยู่ก่อนวันแรก");
        }

        return Build(new DateOnly(first.Year, first.Month, 1), last, branchId);
    }

    /// <summary>Today, at one branch or at every branch the caller may see.</summary>
    /// <param name="branchId">One branch, or null.</param>
    public ReportWindow ForToday(string? branchId) => Build(days.Today, days.Today, branchId);

    private ReportWindow Build(DateOnly first, DateOnly last, string? branchId)
    {
        if (last < first)
        {
            throw Invalid("to", "วันสุดท้ายต้องไม่อยู่ก่อนวันแรก");
        }

        if (last.DayNumber - first.DayNumber + 1 > LongestSpanDays)
        {
            throw Invalid("to", $"ดูรายงานได้ครั้งละไม่เกิน {LongestSpanDays} วัน");
        }

        IReadOnlyCollection<string>? branches = branchGuard.VisibleBranchIds;

        if (branchId is { Length: > 0 })
        {
            branchGuard.EnsureCanAccess(branchId);
            branches = [branchId];
        }

        return new ReportWindow(
            first,
            last,
            days.StartOf(first),
            days.StartOf(last.AddDays(1)),
            branchId is { Length: > 0 } ? branchId : null,
            branches);
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
