namespace HappyPaws.Domain.Analytics;

/// <summary>How many distinct customers of one first-visit month came in during one calendar month.</summary>
/// <param name="Month">First day of the month the visits happened in.</param>
/// <param name="Cohort">First day of the month of the customer's first ever visit.</param>
/// <param name="Customers">Distinct customers.</param>
public sealed record RetentionCell(DateOnly Month, DateOnly Cohort, int Customers);

/// <summary>One month's customers, split by whether this was their first month.</summary>
public sealed record RetentionMonth(DateOnly Month, int Active, int New, int Returning);

/// <summary>
/// The customers whose first visit fell in <paramref name="Cohort"/>, and how many of them came
/// back in each month after. <c>Retained[0]</c> is the cohort itself; <c>Retained[k]</c> is month k.
/// </summary>
public sealed record RetentionCohort(DateOnly Cohort, int Customers, IReadOnlyList<int> Retained);

/// <summary>
/// Folds month-by-cohort counts into the two views of "ลูกค้าใหม่/กลับมา · cohort รายเดือน".
/// <para>
/// The query returns one small grid of counts and this does the rest, so the rule for what is
/// "new" and what is "returning" lives in one place that a unit test can read: a customer is new in
/// the month of their first visit and returning in every month after it.
/// </para>
/// </summary>
public static class RetentionMatrix
{
    /// <summary>Per-month activity for every month from <paramref name="firstMonth"/> to <paramref name="lastMonth"/>.</summary>
    public static IReadOnlyList<RetentionMonth> Months(
        IReadOnlyCollection<RetentionCell> cells,
        DateOnly firstMonth,
        DateOnly lastMonth) =>
    [
        .. ReportPeriods.Between(firstMonth, lastMonth, ReportGranularity.Month).Select(month =>
        {
            var inMonth = cells.Where(cell => cell.Month == month).ToList();
            var fresh = inMonth.Where(cell => cell.Cohort == month).Sum(cell => cell.Customers);
            var returning = inMonth.Where(cell => cell.Cohort < month).Sum(cell => cell.Customers);

            return new RetentionMonth(month, fresh + returning, fresh, returning);
        }),
    ];

    /// <summary>
    /// One row per cohort that started inside the range. A cohort that started before it has no
    /// "month zero" in view, so its retention cannot be read from these numbers.
    /// </summary>
    public static IReadOnlyList<RetentionCohort> Cohorts(
        IReadOnlyCollection<RetentionCell> cells,
        DateOnly firstMonth,
        DateOnly lastMonth)
    {
        var months = ReportPeriods.Between(firstMonth, lastMonth, ReportGranularity.Month);

        return
        [
            .. months.Select(cohort =>
            {
                var retained = months
                    .Where(month => month >= cohort)
                    .Select(month => cells
                        .Where(cell => cell.Cohort == cohort && cell.Month == month)
                        .Sum(cell => cell.Customers))
                    .ToList();

                return new RetentionCohort(cohort, retained[0], retained);
            }),
        ];
    }
}
