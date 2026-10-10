using HappyPaws.Application.Articles.GetArticle;
using HappyPaws.Application.Articles.ListPublishedArticles;
using HappyPaws.Application.Articles.Manage;
using HappyPaws.Application.Articles.SeedArticles;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Articles;

/// <summary>
/// Registers health articles: the two public reads, the operator's seed, and the staff writing
/// desk (<c>docs/contract-extensions.md § 26</c>).
/// </summary>
public static class ArticlesModule
{
    public static IServiceCollection AddArticlesModule(this IServiceCollection services)
    {
        services.AddScoped<ListPublishedArticlesHandler>();
        services.AddScoped<GetArticleHandler>();
        services.AddScoped<SeedArticlesHandler>();

        services.AddScoped<ArticleSlugs>();
        services.AddScoped<ListStaffArticlesHandler>();
        services.AddScoped<GetStaffArticleHandler>();
        services.AddScoped<CreateArticleHandler>();
        services.AddScoped<UpdateArticleHandler>();
        services.AddScoped<PublishArticleHandler>();
        services.AddScoped<UnpublishArticleHandler>();
        services.AddScoped<DeleteArticleHandler>();
        services.AddScoped<RequestArticleCoverHandler>();
        services.AddScoped<ArticleCoverReceiver>();

        return services;
    }
}
