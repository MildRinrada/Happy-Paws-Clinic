namespace HappyPaws.Api.Contracts.Articles;

/// <summary>
/// <c>POST /staff/articles</c> — a draft:
/// <code>
/// { "title": "วัคซีนแมวต้องฉีดตอนไหน", "slug": "cat-vaccine-schedule", "summary": "…",
///   "body": "ย่อหน้าแรก\n\nย่อหน้าที่สอง", "author_name": "สพ.ญ. มินท์", "reading_minutes": 4 }
/// </code>
/// <c>slug</c> is made from the title when absent, which a Thai title cannot do.
/// <c>reading_minutes</c> is estimated from the body when absent.
/// </summary>
public sealed record CreateArticleRequest(
    string? Title = null,
    string? Slug = null,
    string? Summary = null,
    string? Body = null,
    string? AuthorName = null,
    int? ReadingMinutes = null);

/// <summary>
/// <c>PATCH /staff/articles/{id}</c> — any of the fields of a draft, plus <c>remove_cover</c>.
/// A field left out is not changed. <c>If-Match</c> is required.
/// </summary>
public sealed record UpdateArticleRequest(
    string? Title = null,
    string? Slug = null,
    string? Summary = null,
    string? Body = null,
    string? AuthorName = null,
    int? ReadingMinutes = null,
    bool? RemoveCover = null);

/// <summary>
/// <c>POST /staff/articles/{id}/cover</c> — the file about to be sent:
/// <code>{ "content_type": "image/jpeg", "size_bytes": 482113 }</code>
/// </summary>
public sealed record ArticleCoverRequest(string? ContentType = null, long SizeBytes = 0);
