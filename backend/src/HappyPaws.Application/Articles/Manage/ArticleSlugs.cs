using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles.Manage;

/// <summary>
/// Turns what staff typed, or the title, into a slug no other article holds
/// (<c>docs/contract-extensions.md § 26</c>).
/// <para>
/// A deleted article keeps its slug. Handing the address of a deleted article to a different one
/// would make an old link, a search result or a LINE message open a page about something else.
/// </para>
/// </summary>
public sealed class ArticleSlugs(IArticleRepository articles)
{
    public const string InvalidMessage =
        "slug ใช้ได้แค่ตัวอักษรภาษาอังกฤษพิมพ์เล็ก ตัวเลข และขีดกลาง ยาวไม่เกิน 80 ตัวอักษร";

    public const string NeedsSlugMessage =
        "ชื่อเรื่องไม่มีตัวอักษรภาษาอังกฤษให้สร้าง slug ได้ กรุณาตั้ง slug เอง เช่น cat-vaccine-schedule";

    /// <summary>
    /// The slug for a new article: the one typed, tidied, or one made from the title. Refuses with
    /// <c>422</c> on <c>slug</c> when neither gives a clean address, and with
    /// <c>409 article_slug_taken</c> when another article holds it.
    /// </summary>
    public async Task<string> ForNewAsync(string? typed, string title, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(typed)
            ? ArticleSlug.FromTitle(title) ?? throw Invalid(NeedsSlugMessage)
            : Clean(typed);

        await EnsureFreeAsync(slug, articleId: null, cancellationToken);

        return slug;
    }

    /// <summary>A slug typed for an existing article, tidied and checked to be free of other articles.</summary>
    public async Task<string> ForExistingAsync(string typed, string articleId, CancellationToken cancellationToken)
    {
        var slug = Clean(typed);
        await EnsureFreeAsync(slug, articleId, cancellationToken);

        return slug;
    }

    /// <summary>The answer when the slug is held, here or from the database's unique index.</summary>
    public static ConflictException Taken(string? slug, SlugHolder? holder)
    {
        var details = new Dictionary<string, object>();

        if (slug is not null)
        {
            details["slug"] = slug;
        }

        if (holder is not null)
        {
            details["article_id"] = holder.ArticleId;
            details["deleted"] = holder.Deleted;
        }

        return new ConflictException(
            ErrorCodes.ArticleSlugTaken,
            holder?.Deleted == true
                ? "slug นี้เคยใช้กับบทความที่ลบไปแล้ว จึงใช้ซ้ำไม่ได้ กรุณาตั้ง slug ใหม่"
                : "มีบทความอื่นใช้ slug นี้อยู่แล้ว กรุณาตั้ง slug ใหม่")
        {
            MessageEn = "Another article already has this slug.",
            Field = "slug",
            Details = details.Count > 0 ? details : null,
        };
    }

    private static string Clean(string typed)
    {
        var slug = ArticleSlug.Normalize(typed);

        return ArticleSlug.IsValid(slug) ? slug : throw Invalid(InvalidMessage);
    }

    private async Task EnsureFreeAsync(string slug, string? articleId, CancellationToken cancellationToken)
    {
        if (await articles.FindSlugHolderAsync(slug, cancellationToken) is { } holder && holder.ArticleId != articleId)
        {
            throw Taken(slug, holder);
        }
    }

    private static ValidationFailedException Invalid(string message) =>
        new(new Dictionary<string, string[]> { ["slug"] = [message] });
}
