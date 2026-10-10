using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Articles;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Articles.Manage;

/// <summary><c>POST /staff/articles</c>. Not in the API reference: see <c>docs/contract-extensions.md § 26</c>.</summary>
/// <param name="Title">Headline.</param>
/// <param name="Summary">One or two sentences for the cards.</param>
/// <param name="Body">Plain text, a blank line between paragraphs.</param>
/// <param name="AuthorName">Byline, as readers see it.</param>
/// <param name="Slug">The address. Made from the title when absent, which a Thai title cannot do.</param>
/// <param name="ReadingMinutes">1 to 120. Estimated from the body when absent.</param>
public sealed record CreateArticleCommand(
    string? Title,
    string? Summary,
    string? Body,
    string? AuthorName,
    string? Slug = null,
    int? ReadingMinutes = null);

public sealed class CreateArticleValidator : AbstractValidator<CreateArticleCommand>
{
    public CreateArticleValidator()
    {
        ArticleFieldRules.Title(RuleFor(command => command.Title));
        ArticleFieldRules.Summary(RuleFor(command => command.Summary));
        ArticleFieldRules.Body(RuleFor(command => command.Body));
        ArticleFieldRules.Author(RuleFor(command => command.AuthorName));
        ArticleFieldRules.ReadingMinutes(RuleFor(command => command.ReadingMinutes));
        ArticleFieldRules.Slug(RuleFor(command => command.Slug));
    }
}

/// <summary>
/// Writes a draft. Nothing reaches the public site until it is published, which is its own step,
/// so a half-written article can be saved and come back to.
/// </summary>
public sealed class CreateArticleHandler(
    IArticleRepository articles,
    ArticleSlugs slugs,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    IClock clock,
    RequestValidator validator)
{
    public async Task<StaffArticleDto> HandleAsync(CreateArticleCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        Article? article = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var slug = await slugs.ForNewAsync(command.Slug, ArticleText.CleanLine(command.Title!), token);
            var body = ArticleText.CleanBody(command.Body!);

            article = Article.Draft(
                slug,
                command.Title!,
                command.Summary!,
                body,
                command.AuthorName!,
                command.ReadingMinutes ?? ArticleText.EstimateReadingMinutes(body));

            articles.Add(article);

            audit.Record(new AuditEvent(
                AuditActions.ArticleCreated,
                AuditResources.Article,
                article.Id,
                Details: new Dictionary<string, object?>
                {
                    ["slug"] = article.Slug,
                    ["title"] = article.Title,
                    ["author_name"] = article.AuthorName,
                }));
        }, cancellationToken);

        return StaffArticleDto.From(article!, clock.UtcNow, storage);
    }
}
