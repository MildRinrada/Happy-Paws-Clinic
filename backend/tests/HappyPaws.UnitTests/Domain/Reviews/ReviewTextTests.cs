using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Reviews;
using HappyPaws.Domain.Reviews;

namespace HappyPaws.UnitTests.Domain.Reviews;

/// <summary>What a review and a reply may say (<c>docs/contract-extensions.md § 36</c>).</summary>
public class ReviewTextTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void A_score_is_one_to_five(int rating) => Assert.True(ReviewText.IsRating(rating));

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(null)]
    public void Anything_else_is_not_a_score(int? rating) => Assert.False(ReviewText.IsRating(rating));

    [Fact]
    public void Words_are_optional_and_blank_words_are_none()
    {
        Assert.Null(ReviewText.Problem(null));
        Assert.Null(ReviewText.Problem("   \n  "));
        Assert.Null(ReviewText.Clean("   \n  "));
    }

    [Fact]
    public void Text_is_cleaned_like_an_article()
    {
        var cleaned = ReviewText.Clean("หมอใจดี‮\r\n\r\n\r\n\r\nแนะนำเลย  ");

        Assert.Equal("หมอใจดี\n\nแนะนำเลย", cleaned);
    }

    [Fact]
    public void A_thousand_characters_is_the_most()
    {
        Assert.Null(ReviewText.Problem(new string('ก', ReviewText.MaxLength)));
        Assert.NotNull(ReviewText.Problem(new string('ก', ReviewText.MaxLength + 1)));
    }

    [Theory]
    [InlineData("<b>ดีมาก</b>")]
    [InlineData("ดู <a href=\"https://spam.example\">ที่นี่</a>")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<!-- comment -->")]
    [InlineData("ดี<br/>มาก")]
    public void Anything_that_looks_like_HTML_is_refused(string text) => Assert.NotNull(ReviewText.Problem(text));

    [Theory]
    [InlineData("ไข้ < 39 องศาแล้ว")]
    [InlineData("รักหมอ <3")]
    [InlineData("5 > 4")]
    public void Angle_brackets_in_a_sentence_are_fine(string text) => Assert.Null(ReviewText.Problem(text));

    [Fact]
    public void A_reply_must_say_something()
    {
        Assert.NotNull(ReviewText.ReplyProblem(null));
        Assert.NotNull(ReviewText.ReplyProblem("  "));
        Assert.Null(ReviewText.ReplyProblem("ขอบคุณค่ะ"));
        Assert.NotNull(ReviewText.ReplyProblem(new string('ก', ReviewText.ReplyMaxLength + 1)));
    }

    [Fact]
    public void Every_problem_with_a_review_names_its_field()
    {
        var error = Assert.Throws<ValidationFailedException>(() => ReviewRules.Check(0, "<b>x</b>", "full_name"));

        Assert.Contains("rating", error.Errors.Keys);
        Assert.Contains("text", error.Errors.Keys);
        Assert.Contains("display_name", error.Errors.Keys);
    }

    [Theory]
    [InlineData("first_name", ReviewNameChoice.FirstName)]
    [InlineData("anonymous", ReviewNameChoice.Anonymous)]
    public void The_name_choice_is_one_of_two(string wire, ReviewNameChoice expected)
    {
        Assert.Equal(expected, ReviewRules.Check(4, null, wire).NameChoice);
    }
}
