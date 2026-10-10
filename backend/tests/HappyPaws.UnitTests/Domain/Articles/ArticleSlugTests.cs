using HappyPaws.Domain.Articles;

namespace HappyPaws.UnitTests.Domain.Articles;

/// <summary>
/// An article's address (<c>docs/contract-extensions.md § 26</c>): what a person types is tidied,
/// a title in English becomes one, and a title in Thai asks for one instead of guessing.
/// </summary>
public class ArticleSlugTests
{
    [Theory]
    [InlineData("  Cat Vaccine Schedule ", "cat-vaccine-schedule")]
    [InlineData("cat_vaccine__schedule", "cat-vaccine-schedule")]
    [InlineData("--cat---vaccine--", "cat-vaccine")]
    [InlineData("CAT-2026", "cat-2026")]
    public void What_a_person_types_is_tidied_into_an_address(string typed, string expected)
    {
        var slug = ArticleSlug.Normalize(typed);

        Assert.Equal(expected, slug);
        Assert.True(ArticleSlug.IsValid(slug));
    }

    [Theory]
    [InlineData("วัคซีนแมว")]
    [InlineData("cat/../admin")]
    [InlineData("cat?x=1")]
    [InlineData("<script>")]
    [InlineData("")]
    public void Characters_outside_the_rule_are_refused_rather_than_dropped(string typed)
    {
        Assert.False(ArticleSlug.IsValid(ArticleSlug.Normalize(typed)));
    }

    [Fact]
    public void A_slug_longer_than_eighty_characters_is_refused()
    {
        Assert.True(ArticleSlug.IsValid(new string('a', Article.SlugMaxLength)));
        Assert.False(ArticleSlug.IsValid(new string('a', Article.SlugMaxLength + 1)));
    }

    [Theory]
    [InlineData("When Should a Cat Be Vaccinated?", "when-should-a-cat-be-vaccinated")]
    [InlineData("Café & Crème: 5 tips", "cafe-creme-5-tips")]
    [InlineData("วัคซีน FeLV สำหรับแมว 2026", "felv-2026")]
    public void An_english_title_becomes_an_address(string title, string expected)
    {
        Assert.Equal(expected, ArticleSlug.FromTitle(title));
    }

    [Theory]
    [InlineData("วัคซีนแมวต้องฉีดตอนไหน ครบเมื่อไร")]
    [InlineData("ปี 2026")]
    [InlineData("!!!")]
    public void A_title_without_latin_letters_gives_no_address_to_guess(string title)
    {
        // No transliteration: a wrong romanisation is worse than asking the writer for one.
        Assert.Null(ArticleSlug.FromTitle(title));
    }

    [Fact]
    public void A_long_title_is_cut_at_a_word_never_inside_one()
    {
        var title = string.Join(' ', Enumerable.Repeat("vaccination", 12));

        var slug = ArticleSlug.FromTitle(title)!;

        Assert.True(slug.Length <= Article.SlugMaxLength);
        Assert.True(ArticleSlug.IsValid(slug));
        Assert.All(slug.Split('-'), word => Assert.Equal("vaccination", word));
    }
}
