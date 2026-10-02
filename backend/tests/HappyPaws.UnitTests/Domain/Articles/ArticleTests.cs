using HappyPaws.Application.Articles.SeedArticles;
using HappyPaws.Domain.Articles;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Domain.Articles;

/// <summary>
/// The slug is an address and the body is shown to anyone, so those are the two things an article
/// refuses to get wrong.
/// </summary>
public class ArticleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    private static Article Publish(string slug = "cat-vaccine-schedule", string? cover = null, int minutes = 4) =>
        Article.Publish(slug, "วัคซีนแมว", "คำโปรย", "ย่อหน้าแรก\n\nย่อหน้าที่สอง", "สพ.ญ. มินท์", minutes, Now, cover);

    [Fact]
    public void An_article_is_public_from_the_moment_it_is_published()
    {
        var article = Publish();

        Assert.False(article.IsPublishedAt(Now.AddSeconds(-1)));
        Assert.True(article.IsPublishedAt(Now));
        Assert.StartsWith("art_", article.Id);
    }

    [Theory]
    [InlineData("Cat-Vaccines")]
    [InlineData("cat vaccines")]
    [InlineData("cat--vaccines")]
    [InlineData("-cat")]
    [InlineData("วัคซีนแมว")]
    [InlineData("")]
    public void A_slug_that_is_not_a_clean_address_is_refused(string slug)
    {
        var refused = Assert.Throws<DomainException>(() => Publish(slug));

        Assert.Equal("invalid_article", refused.Code);
    }

    [Fact]
    public void A_cover_must_be_a_path_on_the_web_app_not_an_outside_address()
    {
        // An outside URL would make the public page load a picture from somewhere the clinic does
        // not control.
        Assert.Throws<DomainException>(() => Publish(cover: "https://example.com/cat.jpg"));
        Assert.Equal("/images/articles/cat.jpg", Publish(cover: "/images/articles/cat.jpg").CoverImagePath);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public void Reading_time_must_be_sensible(int minutes)
    {
        Assert.Throws<DomainException>(() => Publish(minutes: minutes));
    }

    [Fact]
    public void Every_sample_article_is_valid_and_has_its_own_slug()
    {
        var samples = SampleArticles.All;

        foreach (var sample in samples)
        {
            Article.Publish(sample.Slug, sample.Title, sample.Summary, sample.Body, sample.AuthorName,
                sample.ReadingMinutes, Now.AddDays(-sample.DaysAgo));
        }

        Assert.Equal(samples.Count, samples.Select(sample => sample.Slug).Distinct().Count());
    }
}
