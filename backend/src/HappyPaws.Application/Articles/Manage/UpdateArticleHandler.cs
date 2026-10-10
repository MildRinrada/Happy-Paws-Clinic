using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Articles;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;

namespace HappyPaws.Application.Articles.Manage;

/// <summary>
/// <c>PATCH /staff/articles/{id}</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 26</c>. A field left out is not changed.
/// </summary>
/// <param name="ArticleId">From the path.</param>
/// <param name="Slug">A new address. Refused once the article has ever been published.</param>
/// <param name="Title">Headline.</param>
/// <param name="Summary">One or two sentences.</param>
/// <param name="Body">Plain text, a blank line between paragraphs.</param>
/// <param name="AuthorName">Byline.</param>
/// <param name="ReadingMinutes">1 to 120.</param>
/// <param name="RemoveCover"><c>true</c> takes the cover away.</param>
/// <param name="IfMatchVersion">Required: the article's <c>version</c>.</param>
public sealed record UpdateArticleCommand(
    string ArticleId,
    string? Slug = null,
    string? Title = null,
    string? Summary = null,
    string? Body = null,
    string? AuthorName = null,
    int? ReadingMinutes = null,
    bool? RemoveCover = null,
    uint? IfMatchVersion = null);

public sealed class UpdateArticleValidator : AbstractValidator<UpdateArticleCommand>
{
    public UpdateArticleValidator()
    {
        When(command => command.Title is not null, () => ArticleFieldRules.Title(RuleFor(command => command.Title)));
        When(command => command.Summary is not null, () => ArticleFieldRules.Summary(RuleFor(command => command.Summary)));
        When(command => command.Body is not null, () => ArticleFieldRules.Body(RuleFor(command => command.Body)));
        When(command => command.AuthorName is not null, () => ArticleFieldRules.Author(RuleFor(command => command.AuthorName)));
        ArticleFieldRules.ReadingMinutes(RuleFor(command => command.ReadingMinutes));
        ArticleFieldRules.Slug(RuleFor(command => command.Slug));
    }
}

/// <summary>
/// Changes a draft or a published article. A published article changes on the public site as soon
/// as this commits.
/// <para>
/// <c>If-Match</c> is <b>required</b>, unlike most edits in this system: an article is long text two
/// people may have open, and a blind save would throw away the other person's paragraphs without
/// a word. The slug is fixed once the article has ever been published (<c>409 state_conflict</c>),
/// and must be free of every other article, deleted ones included (<c>409 article_slug_taken</c>).
/// Audited with the fields that changed; the body is recorded by length, not copied into the log.
/// A request that changes nothing writes nothing.
/// </para>
/// </summary>
public sealed class UpdateArticleHandler(
    IArticleRepository articles,
    ArticleSlugs slugs,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    IClock clock,
    RequestValidator validator)
{
    public async Task<StaffArticleDto> HandleAsync(UpdateArticleCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Article? article = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            article = await articles.FindAsync(command.ArticleId, token)
                ?? throw new NotFoundException("article", command.ArticleId);

            ConcurrencyGuard.EnsureRequired(article, command.IfMatchVersion);

            var was = (article.Slug, article.Title, article.Summary, article.Body, article.AuthorName, article.ReadingMinutes,
                Cover: article.CoverImageKey is not null || article.CoverImagePath is not null);

            if (command.Slug is { } typed && ArticleSlug.Normalize(typed) != article.Slug)
            {
                // Refused before the new slug is even looked up: a fixed address stays fixed.
                if (article.IsSlugLocked)
                {
                    throw new StateConflictException("article", "published_before", "change_slug");
                }

                article.ChangeSlug(await slugs.ForExistingAsync(typed, article.Id, token));
            }

            article.Edit(command.Title, command.Summary, command.Body, command.AuthorName, command.ReadingMinutes);

            if (command.RemoveCover is true)
            {
                article.RemoveCover();
            }

            var changes = new Dictionary<string, object?>();

            void Note(string field, object? before, object? after)
            {
                if (!Equals(before, after))
                {
                    changes[field] = new Dictionary<string, object?> { ["before"] = before, ["after"] = after };
                }
            }

            Note("slug", was.Slug, article.Slug);
            Note("title", was.Title, article.Title);
            Note("summary", was.Summary, article.Summary);
            Note("author_name", was.AuthorName, article.AuthorName);
            Note("reading_minutes", was.ReadingMinutes, article.ReadingMinutes);
            Note("cover", was.Cover, article.CoverImageKey is not null || article.CoverImagePath is not null);

            if (was.Body != article.Body)
            {
                changes["body"] = new Dictionary<string, object?> { ["before_length"] = was.Body.Length, ["after_length"] = article.Body.Length };
            }

            if (changes.Count > 0)
            {
                audit.Record(new AuditEvent(
                    AuditActions.ArticleUpdated,
                    AuditResources.Article,
                    article.Id,
                    Details: new Dictionary<string, object?>
                    {
                        ["slug"] = article.Slug,
                        ["status"] = WireEnum.Of(article.StatusAt(clock.UtcNow)),
                        ["changes"] = changes,
                    }));
            }
        }, cancellationToken);

        return StaffArticleDto.From(article!, clock.UtcNow, storage);
    }
}
