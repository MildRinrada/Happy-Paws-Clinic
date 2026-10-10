using HappyPaws.Domain.Reviews;

namespace HappyPaws.UnitTests.Domain.Reviews;

/// <summary>
/// The name a published review is shown under: a first name without its title, or "ลูกค้า". Never
/// a surname (<c>docs/contract-extensions.md § 36</c>).
/// </summary>
public class ReviewNamesTests
{
    [Theory]
    [InlineData("คุณสมชาย ใจดี", "สมชาย")]
    [InlineData("คุณ สมชาย ใจดี", "สมชาย")]
    [InlineData("นายสมชาย ใจดี", "สมชาย")]
    [InlineData("นางสาวมณี ศรีสุข", "มณี")]
    [InlineData("นางมณี ศรีสุข", "มณี")]
    [InlineData("น.ส.มณี ศรีสุข", "มณี")]
    [InlineData("สมหญิง", "สมหญิง")]
    [InlineData("Mr. John Smith", "John")]
    [InlineData("mrs Jane Doe", "Jane")]
    [InlineData("  มายด์   ทดสอบ ", "มายด์")]
    public void The_first_name_is_the_first_word_without_a_title(string name, string expected)
    {
        Assert.Equal(expected, ReviewNames.FirstNameOf(name));
    }

    [Theory]
    [InlineData("คุณากร ศรีสุข", "คุณากร")]
    [InlineData("นายิกา ทดสอบ", "นายิกา")]
    public void A_name_that_only_starts_like_a_title_is_kept_whole(string name, string expected)
    {
        Assert.Equal(expected, ReviewNames.FirstNameOf(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("คุณ")]
    public void Nothing_usable_is_no_name(string? name) => Assert.Null(ReviewNames.FirstNameOf(name));

    [Fact]
    public void An_anonymous_review_keeps_no_name_and_shows_customer()
    {
        Assert.Null(ReviewNames.PublicNameFor(ReviewNameChoice.Anonymous, "คุณสมชาย ใจดี"));
        Assert.Equal("ลูกค้า", ReviewNames.Display(null));
        Assert.Equal("สมชาย", ReviewNames.Display(ReviewNames.PublicNameFor(ReviewNameChoice.FirstName, "คุณสมชาย ใจดี")));
    }

    [Fact]
    public void A_very_long_first_name_is_cut()
    {
        Assert.Equal(ReviewNames.MaxLength, ReviewNames.FirstNameOf(new string('ก', 80))!.Length);
    }
}
