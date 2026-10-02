using HappyPaws.Application.Articles.GetArticle;
using HappyPaws.Application.Articles.ListPublishedArticles;
using HappyPaws.Application.Articles.SeedArticles;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Articles;

/// <summary>Registers health articles: the two public reads and the operator's seed.</summary>
public static class ArticlesModule
{
    public static IServiceCollection AddArticlesModule(this IServiceCollection services)
    {
        services.AddScoped<ListPublishedArticlesHandler>();
        services.AddScoped<GetArticleHandler>();
        services.AddScoped<SeedArticlesHandler>();

        return services;
    }
}
