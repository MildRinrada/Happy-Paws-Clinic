using HappyPaws.Domain.Analytics;

namespace HappyPaws.UnitTests.Domain.Analytics;

/// <summary>New, returning, and how each month's newcomers came back.</summary>
public class RetentionMatrixTests
{
    private static readonly DateOnly August = new(2026, 8, 1);
    private static readonly DateOnly September = new(2026, 9, 1);
    private static readonly DateOnly October = new(2026, 10, 1);

    /// <summary>
    /// Five customers. Two started in June (before the report), three in August; of August's three,
    /// one came back in September and two in October.
    /// </summary>
    private static readonly RetentionCell[] Cells =
    [
        new(August, new DateOnly(2026, 6, 1), 2),
        new(August, August, 3),
        new(September, August, 1),
        new(October, August, 2),
        new(October, new DateOnly(2026, 6, 1), 1),
    ];

    [Fact]
    public void A_customer_is_new_in_their_first_month_and_returning_after_it()
    {
        var months = RetentionMatrix.Months(Cells, August, new DateOnly(2026, 10, 4));

        Assert.Equal(
            [new RetentionMonth(August, 5, 3, 2), new RetentionMonth(September, 1, 0, 1), new RetentionMonth(October, 3, 0, 3)],
            months);
    }

    [Fact]
    public void Each_cohort_in_range_is_followed_month_by_month()
    {
        var cohorts = RetentionMatrix.Cohorts(Cells, August, new DateOnly(2026, 10, 4));

        Assert.Equal(3, cohorts.Count);
        Assert.Equal(3, cohorts[0].Customers);
        Assert.Equal([3, 1, 2], cohorts[0].Retained);
        Assert.Equal([0, 0], cohorts[1].Retained);
        Assert.Equal([0], cohorts[2].Retained);
    }

    [Fact]
    public void A_cohort_that_began_before_the_range_is_not_listed()
    {
        var cohorts = RetentionMatrix.Cohorts(Cells, August, October);

        Assert.DoesNotContain(cohorts, cohort => cohort.Cohort < August);
    }
}
