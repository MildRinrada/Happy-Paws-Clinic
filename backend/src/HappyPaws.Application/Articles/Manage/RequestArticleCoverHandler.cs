using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Auditing;

namespace HappyPaws.Application.Articles.Manage;

/// <summary><c>POST /staff/articles/{id}/cover</c>. See <c>docs/contract-extensions.md § 26</c>.</summary>
/// <param name="ArticleId">From the path.</param>
/// <param name="ContentType"><c>image/jpeg</c>, <c>image/png</c> or <c>image/webp</c>.</param>
/// <param name="SizeBytes">The file's size; the URL accepts no more.</param>
/// <param name="IfMatchVersion">The article's <c>version</c>, when sent.</param>
public sealed record RequestArticleCoverCommand(
    string ArticleId,
    string? ContentType,
    long SizeBytes,
    uint? IfMatchVersion = null);

public sealed class RequestArticleCoverValidator : AbstractValidator<RequestArticleCoverCommand>
{
    public RequestArticleCoverValidator()
    {
        RuleFor(command => command.ContentType)
            .Must(type => type is not null && ArticleCovers.Extensions.ContainsKey(type))
            .WithMessage("รูปปกต้องเป็นไฟล์ JPEG, PNG หรือ WebP");

        RuleFor(command => command.SizeBytes)
            .InclusiveBetween(1, ArticleCovers.MaxBytes)
            .WithMessage("รูปปกต้องมีขนาดไม่เกิน 5 MB");
    }
}

/// <summary>
/// Hands out the signed URL for a new cover. The article's current cover stays until the new file
/// arrives at that URL, when <see cref="ArticleCoverReceiver"/> swaps it in; an upload that never
/// arrives changes nothing. Audited as <c>article.updated</c> when asked, because the picture on a
/// published article changes on the public site the moment the file lands, with no request of
/// its own to record.
/// </summary>
public sealed class RequestArticleCoverHandler(
    IArticleRepository articles,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    IClock clock,
    RequestValidator validator)
{
    public async Task<ArticleCoverUploadDto> HandleAsync(RequestArticleCoverCommand command, CancellationToken cancellationToken = default)
    {
        command = command with { ContentType = command.ContentType?.Split(';')[0].Trim().ToLowerInvariant() };
        await validator.ValidateAsync(command, cancellationToken);

        Domain.Articles.Article? article = null;
        string? key = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            article = await articles.FindAsync(command.ArticleId, token)
                ?? throw new NotFoundException("article", command.ArticleId);

            ConcurrencyGuard.Ensure(article, command.IfMatchVersion);

            key = ArticleCovers.NewKey(article.Id, command.ContentType!);
            article.ExpectCover(key);

            audit.Record(new AuditEvent(
                AuditActions.ArticleUpdated,
                AuditResources.Article,
                article.Id,
                Details: new Dictionary<string, object?>
                {
                    ["slug"] = article.Slug,
                    ["status"] = WireEnum.Of(article.StatusAt(clock.UtcNow)),
                    ["changes"] = new Dictionary<string, object?>
                    {
                        ["cover_upload"] = new Dictionary<string, object?>
                        {
                            ["content_type"] = command.ContentType,
                            ["size_bytes"] = command.SizeBytes,
                        },
                    },
                }));
        }, cancellationToken);

        var ticket = storage.CreateUpload(key!, command.ContentType!, command.SizeBytes, ArticleCovers.UploadLifetime);

        return new ArticleCoverUploadDto(StaffArticleDto.From(article!, clock.UtcNow, storage), UploadTicketDto.From(ticket));
    }
}
