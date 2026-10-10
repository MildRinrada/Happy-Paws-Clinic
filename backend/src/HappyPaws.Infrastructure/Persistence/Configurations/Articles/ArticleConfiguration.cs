using HappyPaws.Domain.Articles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Articles;

internal sealed class ArticleConfiguration : EntityConfiguration<Article>
{
    /// <summary>Named so <c>PostgresErrorTranslator</c> can answer two writers racing for one slug.</summary>
    public const string SlugIndex = "ix_articles_slug";

    protected override void ConfigureEntity(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles", table =>
        {
            // A deleted article is never public, whatever writes the row (docs/contract-extensions.md § 26).
            table.HasCheckConstraint(
                "ck_articles_deleted_not_public",
                "deleted_at IS NULL OR published_at IS NULL");
        });

        builder.Property(article => article.Slug).HasMaxLength(Article.SlugMaxLength).IsRequired();
        builder.Property(article => article.Title).HasMaxLength(Article.TitleMaxLength).IsRequired();
        builder.Property(article => article.Summary).HasMaxLength(Article.SummaryMaxLength).IsRequired();
        builder.Property(article => article.Body).HasMaxLength(Article.BodyMaxLength).IsRequired();
        builder.Property(article => article.AuthorName).HasMaxLength(Article.AuthorMaxLength).IsRequired();
        builder.Property(article => article.CoverImagePath).HasMaxLength(300);
        builder.Property(article => article.CoverImageKey).HasMaxLength(Article.CoverKeyMaxLength);
        builder.Property(article => article.PendingCoverKey).HasMaxLength(Article.CoverKeyMaxLength);
        builder.Property(article => article.DeletedByStaffId).HasMaxLength(40);

        builder.Ignore(article => article.IsDeleted);
        builder.Ignore(article => article.IsSlugLocked);

        // The slug is the article's address, so two articles cannot share one. Deleted articles
        // are in the index on purpose: their address is never handed to another article.
        builder.HasIndex(article => article.Slug).IsUnique().HasDatabaseName(SlugIndex);

        // Every public read is "published by now, newest first".
        builder.HasIndex(article => article.PublishedAt);

        // The staff list pages newest written first.
        builder.HasIndex(article => new { article.CreatedAt, article.Id });
    }
}
