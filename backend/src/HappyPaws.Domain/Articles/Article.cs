using System.Text.RegularExpressions;
using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Articles;

/// <summary>
/// A health article the clinic's vets write for pet owners. <b>Not in the API contract</b>: see
/// <c>docs/contract-extensions.md § 9</c>.
/// <para>
/// The body is plain text, paragraphs separated by a blank line. Not Markdown and not HTML, on
/// purpose: the public site renders it as text, so an article can never carry script into a page
/// anyone can open, and there is no sanitiser to keep up to date.
/// </para>
/// <para>
/// An article is public once <see cref="PublishedAt"/> has passed. There is no draft state beyond
/// that, because nothing writes articles through the API yet; they arrive from the operator's
/// <c>seed-articles</c> command.
/// </para>
/// </summary>
public sealed partial class Article : Entity
{
    public const int SlugMaxLength = 80;
    public const int TitleMaxLength = 200;
    public const int SummaryMaxLength = 500;
    public const int BodyMaxLength = 20_000;
    public const int AuthorMaxLength = 120;

    private Article()
    {
    }

    /// <summary>The address of the article — <c>/articles/dog-sickness-signs</c>. Lowercase, digits and hyphens.</summary>
    public string Slug { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>One or two sentences for a list or a card.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>Plain text. A blank line starts a new paragraph.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>The byline as readers see it — "สพ.ญ. มินท์". A name, not a staff id: an article outlives a contract.</summary>
    public string AuthorName { get; private set; } = string.Empty;

    public int ReadingMinutes { get; private set; }

    /// <summary>A path under the web app's <c>/public</c>, or null while the picture has not been supplied.</summary>
    public string? CoverImagePath { get; private set; }

    /// <summary>When the article became public. In the future means scheduled, not yet shown.</summary>
    public DateTimeOffset PublishedAt { get; private set; }

    public static Article Publish(
        string slug,
        string title,
        string summary,
        string body,
        string authorName,
        int readingMinutes,
        DateTimeOffset publishedAt,
        string? coverImagePath = null)
    {
        var article = new Article
        {
            Id = EntityId.New(EntityId.Article),
            Slug = slug.Trim(),
            Title = title.Trim(),
            Summary = summary.Trim(),
            Body = body.Trim(),
            AuthorName = authorName.Trim(),
            ReadingMinutes = readingMinutes,
            PublishedAt = publishedAt,
            CoverImagePath = string.IsNullOrWhiteSpace(coverImagePath) ? null : coverImagePath.Trim(),
        };

        article.EnsureValid();

        return article;
    }

    /// <summary>True once the article may be shown to the public.</summary>
    public bool IsPublishedAt(DateTimeOffset now) => PublishedAt <= now;

    private void EnsureValid()
    {
        Require(Slug.Length <= SlugMaxLength && SlugPattern().IsMatch(Slug),
            "slug ต้องเป็นตัวพิมพ์เล็ก ตัวเลข และขีดกลางเท่านั้น");
        Require(Title.Length is > 0 and <= TitleMaxLength, "ต้องมีชื่อบทความ");
        Require(Summary.Length is > 0 and <= SummaryMaxLength, "ต้องมีคำโปรย");
        Require(Body.Length is > 0 and <= BodyMaxLength, "ต้องมีเนื้อหา");
        Require(AuthorName.Length is > 0 and <= AuthorMaxLength, "ต้องมีชื่อผู้เขียน");
        Require(ReadingMinutes is >= 1 and <= 120, "เวลาอ่านต้องอยู่ระหว่าง 1 ถึง 120 นาที");
        Require(CoverImagePath is null || CoverImagePath.StartsWith('/'),
            "รูปปกต้องเป็น path ใต้ /public ของหน้าเว็บ ขึ้นต้นด้วย /");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new DomainException("invalid_article", message);
        }
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
