using HappyPaws.Domain.Reviews;

namespace HappyPaws.UnitTests.Domain.Reviews;

/// <summary>The figures above the public list, over published reviews (<c>docs/contract-extensions.md § 36</c>).</summary>
public class ReviewSummaryTests
{
    [Fact]
    public void Nothing_published_has_no_average_and_every_score_at_zero()
    {
        var summary = ReviewSummary.From(new Dictionary<int, int>());

        Assert.Null(summary.Average);
        Assert.Equal(0, summary.Count);
        Assert.Equal([1, 2, 3, 4, 5], summary.ByRating.Keys);
        Assert.All(summary.ByRating.Values, count => Assert.Equal(0, count));
    }

    [Fact]
    public void The_average_is_the_mean_to_one_decimal()
    {
        // 5, 5, 4, 1: 15 / 4 = 3.75, shown 3.8.
        var summary = ReviewSummary.From(new Dictionary<int, int> { [5] = 2, [4] = 1, [1] = 1 });

        Assert.Equal(3.8m, summary.Average);
        Assert.Equal(4, summary.Count);
        Assert.Equal(2, summary.ByRating[5]);
        Assert.Equal(0, summary.ByRating[3]);
    }

    [Fact]
    public void A_single_review_is_its_own_average()
    {
        Assert.Equal(2m, ReviewSummary.From(new Dictionary<int, int> { [2] = 1 }).Average);
    }

    [Fact]
    public void Scores_outside_one_to_five_are_not_counted()
    {
        var summary = ReviewSummary.From(new Dictionary<int, int> { [5] = 1, [0] = 3, [9] = 2 });

        Assert.Equal(1, summary.Count);
        Assert.Equal(5m, summary.Average);
    }
}
