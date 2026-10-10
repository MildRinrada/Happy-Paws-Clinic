using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Articles;

/// <summary>
/// A health article the clinic's vets write for pet owners. <b>Not in the API contract</b>: see
/// <c>docs/contract-extensions.md § 9</c> (reading) and <c>§ 26</c> (writing and publishing).
/// <para>
/// The body is plain text, paragraphs separated by a blank line. Not Markdown and not HTML, on
/// purpose: the public site renders it as text, so an article can never carry script into a page
/// anyone can open, and there is no sanitiser to keep up to date. <see cref="ArticleText"/> cleans
/// out what has no place in text before it is kept.
/// </para>
/// <para>
/// An article is public while <see cref="PublishedAt"/> is set and has passed. A draft has none;
/// unpublishing clears it. <see cref="FirstPublishedAt"/> remembers the first time the article went
/// out: from then on its slug is fixed, because the address may already be shared, and publishing
/// it again brings back its original date rather than jumping it to the top of the list.
/// </para>
/// <para>
/// Deleting keeps the row, with <see cref="DeletedAt"/> set and the article off the site, so its
/// slug is never handed to a different article and a sample the clinic deleted is not seeded back.
/// </para>
/// </summary>
public sealed class Article : Entity
{
    public const int SlugMaxLength = 80;
    public const int TitleMaxLength = 200;
    public const int SummaryMaxLength = 500;
    public const int BodyMaxLength = 20_000;
    public const int AuthorMaxLength = 120;
    public const int CoverKeyMaxLength = 300;

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

    /// <summary>
    /// A path under the web app's <c>/public</c>, or null. Set by the seed only; a cover uploaded by
    /// staff is <see cref="CoverImageKey"/> and replaces it.
    /// </summary>
    public string? CoverImagePath { get; private set; }

    /// <summary>The storage key of the uploaded cover, once its file has arrived.</summary>
    public string? CoverImageKey { get; private set; }

    /// <summary>The storage key an upload URL was issued for, until its file arrives.</summary>
    public string? PendingCoverKey { get; private set; }

    /// <summary>When the article became public. Null for a draft; in the future means scheduled.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>The first time the article was published. Fixes the slug from then on.</summary>
    public DateTimeOffset? FirstPublishedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public string? DeletedByStaffId { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    /// <summary>True once the article has ever been published: its address may be out in the world.</summary>
    public bool IsSlugLocked => FirstPublishedAt is not null;

    /// <summary>An article that is public from <paramref name="publishedAt"/>. Used by the operator's seed.</summary>
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
            FirstPublishedAt = publishedAt,
            CoverImagePath = string.IsNullOrWhiteSpace(coverImagePath) ? null : coverImagePath.Trim(),
        };

        article.EnsureValid();

        return article;
    }

    /// <summary>A draft written by staff. Only staff see it until it is published.</summary>
    public static Article Draft(
        string slug,
        string title,
        string summary,
        string body,
        string authorName,
        int readingMinutes)
    {
        var article = new Article
        {
            Id = EntityId.New(EntityId.Article),
            Slug = slug.Trim(),
            Title = ArticleText.CleanLine(title),
            Summary = ArticleText.CleanLine(summary),
            Body = ArticleText.CleanBody(body),
            AuthorName = ArticleText.CleanLine(authorName),
            ReadingMinutes = readingMinutes,
        };

        article.EnsureValid();

        return article;
    }

    /// <summary>True once the article may be shown to the public.</summary>
    public bool IsPublishedAt(DateTimeOffset now) => !IsDeleted && PublishedAt is { } published && published <= now;

    public ArticleStatus StatusAt(DateTimeOffset now) =>
        PublishedAt switch
        {
            null => ArticleStatus.Draft,
            { } published when published > now => ArticleStatus.Scheduled,
            _ => ArticleStatus.Published,
        };

    /// <summary>
    /// Changes what the article says. A value left null is unchanged. Text is cleaned the same way
    /// as on a new draft.
    /// </summary>
    public void Edit(string? title, string? summary, string? body, string? authorName, int? readingMinutes)
    {
        EnsureNotDeleted("edit");

        Title = title is null ? Title : ArticleText.CleanLine(title);
        Summary = summary is null ? Summary : ArticleText.CleanLine(summary);
        Body = body is null ? Body : ArticleText.CleanBody(body);
        AuthorName = authorName is null ? AuthorName : ArticleText.CleanLine(authorName);
        ReadingMinutes = readingMinutes ?? ReadingMinutes;

        EnsureValid();
    }

    /// <summary>
    /// Moves the article to another address. Refused once it has ever been published: links to the
    /// old address may be in a LINE message or a search engine, and they would land on a 404.
    /// </summary>
    public void ChangeSlug(string slug)
    {
        EnsureNotDeleted("change_slug");

        if (slug == Slug)
        {
            return;
        }

        if (IsSlugLocked)
        {
            throw new StateConflictException("article", "published_before", "change_slug");
        }

        Slug = slug.Trim();
        EnsureValid();
    }

    /// <summary>
    /// Puts the article on the public site now. An article published before comes back under its
    /// first date, so taking it down to fix a typo does not move it to the top of the list.
    /// </summary>
    public void PublishNow(DateTimeOffset now)
    {
        EnsureNotDeleted("publish");

        if (IsPublishedAt(now))
        {
            throw new StateConflictException("article", "published", "publish");
        }

        var at = FirstPublishedAt is { } first && first <= now ? first : now;
        PublishedAt = at;
        FirstPublishedAt = at;
    }

    /// <summary>Takes the article off the public site, or cancels its scheduled time. The slug stays fixed.</summary>
    public void Unpublish()
    {
        EnsureNotDeleted("unpublish");

        if (PublishedAt is null)
        {
            throw new StateConflictException("article", "draft", "unpublish");
        }

        PublishedAt = null;
    }

    /// <summary>
    /// Takes the article off the site for good. The row stays, so the slug stays taken and the
    /// audit rows still point at something.
    /// </summary>
    public void Delete(string staffId, DateTimeOffset now)
    {
        EnsureNotDeleted("delete");

        PublishedAt = null;
        PendingCoverKey = null;
        DeletedAt = now;
        DeletedByStaffId = staffId;
    }

    /// <summary>Records the key an upload URL was issued for. The current cover stays until the file arrives.</summary>
    public void ExpectCover(string key)
    {
        EnsureNotDeleted("upload_cover");
        PendingCoverKey = key;
    }

    /// <summary>
    /// The file for the expected key has arrived: it becomes the cover, in place of any earlier
    /// upload or seeded picture. A key that is not the one expected changes nothing.
    /// </summary>
    public bool CoverArrived(string key)
    {
        if (IsDeleted || PendingCoverKey != key)
        {
            return false;
        }

        CoverImageKey = key;
        CoverImagePath = null;
        PendingCoverKey = null;

        return true;
    }

    /// <summary>Takes the cover away, uploaded or seeded. The public page shows its empty frame.</summary>
    public void RemoveCover()
    {
        EnsureNotDeleted("remove_cover");

        CoverImageKey = null;
        CoverImagePath = null;
        PendingCoverKey = null;
    }

    private void EnsureNotDeleted(string attempted)
    {
        if (IsDeleted)
        {
            throw new StateConflictException("article", "deleted", attempted);
        }
    }

    private void EnsureValid()
    {
        Require(ArticleSlug.IsValid(Slug), "slug ต้องเป็นตัวพิมพ์เล็ก ตัวเลข และขีดกลางเท่านั้น");
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
}
