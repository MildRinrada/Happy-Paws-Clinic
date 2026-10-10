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

    private static Article Draft(string slug = "dog-dental-care") =>
        Article.Draft(slug, "ฟันสุนัข", "คำโปรย", "เนื้อหา", "สพ.ญ. มินท์", 3);

    [Fact]
    public void A_draft_is_never_public_until_it_is_published()
    {
        var article = Draft();

        Assert.Equal(ArticleStatus.Draft, article.StatusAt(Now));
        Assert.False(article.IsPublishedAt(Now.AddYears(10)));

        article.PublishNow(Now);

        Assert.Equal(ArticleStatus.Published, article.StatusAt(Now));
        Assert.True(article.IsPublishedAt(Now));
        Assert.Equal(Now, article.FirstPublishedAt);
    }

    [Fact]
    public void Unpublishing_makes_a_draft_again_and_publishing_again_keeps_the_first_date()
    {
        var article = Draft();
        article.PublishNow(Now);

        article.Unpublish();
        Assert.Equal(ArticleStatus.Draft, article.StatusAt(Now.AddDays(1)));
        Assert.False(article.IsPublishedAt(Now.AddDays(1)));

        article.PublishNow(Now.AddDays(3));
        Assert.Equal(Now, article.PublishedAt);
    }

    [Fact]
    public void Publishing_twice_or_unpublishing_a_draft_is_a_state_conflict()
    {
        var article = Draft();

        Assert.Equal("state_conflict", Assert.Throws<StateConflictException>(article.Unpublish).Code);

        article.PublishNow(Now);
        Assert.Throws<StateConflictException>(() => article.PublishNow(Now.AddMinutes(1)));
    }

    [Fact]
    public void The_slug_moves_freely_on_a_draft_and_is_fixed_once_ever_published()
    {
        var article = Draft();
        article.ChangeSlug("dog-teeth");
        Assert.Equal("dog-teeth", article.Slug);

        article.PublishNow(Now);
        article.Unpublish();

        Assert.True(article.IsSlugLocked);
        Assert.Throws<StateConflictException>(() => article.ChangeSlug("dog-teeth-2"));
        article.ChangeSlug("dog-teeth");
    }

    [Fact]
    public void A_deleted_article_is_off_the_site_and_refuses_every_change()
    {
        var article = Draft();
        article.PublishNow(Now);

        article.Delete("stf_editor", Now.AddHours(1));

        Assert.True(article.IsDeleted);
        Assert.Null(article.PublishedAt);
        Assert.False(article.IsPublishedAt(Now.AddDays(1)));
        Assert.Throws<StateConflictException>(() => article.PublishNow(Now.AddDays(1)));
        Assert.Throws<StateConflictException>(() => article.Edit("ใหม่", null, null, null, null));
        Assert.Throws<StateConflictException>(() => article.Delete("stf_editor", Now.AddDays(1)));
    }

    [Fact]
    public void A_new_cover_replaces_the_old_only_when_its_own_file_arrives()
    {
        var article = Publish(cover: "/images/articles/cat.jpg");

        article.ExpectCover("articles/x/cover-1.jpg");
        Assert.Equal("/images/articles/cat.jpg", article.CoverImagePath);

        Assert.False(article.CoverArrived("articles/x/cover-0.jpg"));
        Assert.True(article.CoverArrived("articles/x/cover-1.jpg"));

        Assert.Equal("articles/x/cover-1.jpg", article.CoverImageKey);
        Assert.Null(article.CoverImagePath);
        Assert.Null(article.PendingCoverKey);

        article.RemoveCover();
        Assert.Null(article.CoverImageKey);
    }

    [Fact]
    public void Text_written_by_staff_is_cleaned_before_it_is_kept()
    {
        var article = Article.Draft("cat-fever", " ไข้\nแมว ", "คำโปรย\u0000", "หนึ่ง\r\n\r\n\r\nสอง", "หมอ\u202Eมินท์", 2);

        Assert.Equal("ไข้ แมว", article.Title);
        Assert.Equal("คำโปรย", article.Summary);
        Assert.Equal("หนึ่ง\n\nสอง", article.Body);
        Assert.Equal("หมอมินท์", article.AuthorName);
    }
}
