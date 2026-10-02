using HappyPaws.Domain.Articles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Articles;

internal sealed class ArticleConfiguration : EntityConfiguration<Article>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles");

        builder.Property(article => article.Slug).HasMaxLength(Article.SlugMaxLength).IsRequired();
        builder.Property(article => article.Title).HasMaxLength(Article.TitleMaxLength).IsRequired();
        builder.Property(article => article.Summary).HasMaxLength(Article.SummaryMaxLength).IsRequired();
        builder.Property(article => article.Body).HasMaxLength(Article.BodyMaxLength).IsRequired();
        builder.Property(article => article.AuthorName).HasMaxLength(Article.AuthorMaxLength).IsRequired();
        builder.Property(article => article.CoverImagePath).HasMaxLength(300);

        // The slug is the article's address, so two articles cannot share one.
        builder.HasIndex(article => article.Slug).IsUnique();

        // Every public read is "published by now, newest first".
        builder.HasIndex(article => article.PublishedAt);
    }
}
