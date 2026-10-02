using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Articles;
using HappyPaws.Application.Articles.SeedArticles;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Articles;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Articles;

/// <summary>
/// Health articles, a contract extension: seeded by the operator, read by anyone.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ArticlesTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<SeedArticlesResult> SeedAsync()
    {
        using var scope = fixture.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SeedArticlesHandler>().HandleAsync(Token);
    }

    [Fact]
    public async Task Seeding_twice_publishes_each_sample_once()
    {
        await SeedAsync();
        var second = await SeedAsync();

        Assert.Empty(second.Created);
        Assert.Equal(
            SampleArticles.All.Select(sample => sample.Slug).Order(),
            second.Skipped.Order());
    }

    [Fact]
    public async Task Anyone_can_list_published_articles_newest_first_without_signing_in()
    {
        await SeedAsync();
        using var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync("/articles?limit=50", Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var articles = (await response.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("data").EnumerateArray().ToList();

        var published = articles.Select(article => article.GetProperty("published_at").GetDateTimeOffset()).ToList();
        Assert.Equal(published.OrderDescending(), published);

        var slugs = articles.Select(article => article.GetProperty("slug").GetString()).ToList();
        Assert.All(SampleArticles.All, sample => Assert.Contains(sample.Slug, slugs));

        // A list carries no body: the home page reads three summaries, not three whole articles.
        Assert.All(articles, article => Assert.False(article.TryGetProperty("body", out _)));
    }

    [Fact]
    public async Task The_limit_is_respected_and_bounded()
    {
        await SeedAsync();
        using var anonymous = fixture.CreateClient();

        var three = await anonymous.GetFromJsonAsync<JsonElement>("/articles?limit=3", Token);
        Assert.Equal(3, three.GetProperty("data").GetArrayLength());

        var tooMany = await anonymous.GetAsync("/articles?limit=51", Token);
        Assert.Equal((HttpStatusCode)422, tooMany.StatusCode);
    }

    [Fact]
    public async Task One_article_is_read_by_its_slug_with_its_body()
    {
        await SeedAsync();
        using var anonymous = fixture.CreateClient();

        var article = await anonymous.GetFromJsonAsync<JsonElement>("/articles/cat-vaccine-schedule", Token);

        Assert.Equal("วัคซีนแมวต้องฉีดตอนไหน ครบเมื่อไร", article.GetProperty("title").GetString());
        Assert.Contains("\n\n", article.GetProperty("body").GetString());

        var missing = await anonymous.GetAsync("/articles/no-such-article", Token);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task A_scheduled_article_stays_hidden_until_its_time()
    {
        var slug = $"scheduled-{Guid.NewGuid():N}";

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
            repository.Add(Article.Publish(slug, "ยังไม่ถึงเวลา", "คำโปรย", "เนื้อหา", "สพ.ญ. มินท์", 3,
                DateTimeOffset.UtcNow.AddDays(1)));
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(Token);
        }

        using var anonymous = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/articles/{slug}", Token)).StatusCode);

        var list = await anonymous.GetFromJsonAsync<JsonElement>("/articles?limit=50", Token);
        Assert.DoesNotContain(
            slug,
            list.GetProperty("data").EnumerateArray().Select(article => article.GetProperty("slug").GetString()));
    }
}
