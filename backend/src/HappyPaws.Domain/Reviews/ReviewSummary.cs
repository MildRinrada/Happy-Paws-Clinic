namespace HappyPaws.Domain.Reviews;

/// <summary>
/// The figures above the public review list (<c>docs/contract-extensions.md § 36</c>), computed over
/// <b>published</b> reviews only. A pending review has not been read by anyone yet and a rejected
/// one was taken off for a listed reason, so neither is in the count or the average.
/// </summary>
/// <param name="Average">The mean score to one decimal place, or null when nothing is published.</param>
/// <param name="Count">How many published reviews there are.</param>
/// <param name="ByRating">How many of them gave each score, 1 to 5. Every score is present, zero included.</param>
public sealed record ReviewSummary(decimal? Average, int Count, IReadOnlyDictionary<int, int> ByRating)
{
    /// <summary>The summary of a count per score. Scores outside 1 to 5 are ignored.</summary>
    public static ReviewSummary From(IReadOnlyDictionary<int, int> countsByRating)
    {
        var byRating = new SortedDictionary<int, int>();

        for (var rating = ReviewText.MinRating; rating <= ReviewText.MaxRating; rating++)
        {
            byRating[rating] = countsByRating.TryGetValue(rating, out var count) && count > 0 ? count : 0;
        }

        var total = byRating.Values.Sum();

        if (total == 0)
        {
            return new ReviewSummary(null, 0, byRating);
        }

        var sum = byRating.Sum(entry => (long)entry.Key * entry.Value);
        var average = Math.Round((decimal)sum / total, 1, MidpointRounding.AwayFromZero);

        return new ReviewSummary(average, total, byRating);
    }
}
