using HappyPaws.Domain.Articles;

namespace HappyPaws.UnitTests.Domain.Articles;

/// <summary>
/// What is cleaned out of an article before it is kept (<c>docs/contract-extensions.md § 26</c>).
/// Markup is not one of them: the body is text and is shown as text, so <c>&lt;script&gt;</c> stays
/// as characters a reader sees, and never becomes something a browser runs.
/// </summary>
public class ArticleTextTests
{
    [Fact]
    public void Markup_is_kept_as_the_characters_that_were_typed_not_stripped_or_run()
    {
        const string body = "<script>alert('x')</script>\n\n<img src=x onerror=alert(1)> อุณหภูมิ < 39 องศา";

        // Unchanged: escaping is the renderer's job, and the renderer only ever writes text nodes.
        Assert.Equal(body, ArticleText.CleanBody(body));
    }

    [Fact]
    public void Control_characters_a_nul_and_a_byte_order_mark_are_removed()
    {
        Assert.Equal("ab cd", ArticleText.CleanBody("\uFEFFa\u0000b\tc\u0007d"));
    }

    [Fact]
    public void Bidirectional_overrides_that_reorder_what_is_shown_are_removed()
    {
        // U+202E makes "gnp.exe" display as "exe.png"; the isolates do the same more quietly.
        Assert.Equal("photo-gnp.exe", ArticleText.CleanBody("photo-\u202Egnp.exe"));
        Assert.Equal("ab", ArticleText.CleanLine("a\u2066\u2069b\u202A\u202C"));
    }

    [Fact]
    public void Half_of_a_broken_surrogate_pair_is_removed_and_a_whole_one_kept()
    {
        Assert.Equal("ab", ArticleText.CleanBody("a\uD83Db"));
        Assert.Equal("a🐶b", ArticleText.CleanBody("a🐶b"));
    }

    [Fact]
    public void Line_breaks_become_one_kind_and_blank_lines_separate_paragraphs_once()
    {
        const string typed = "ย่อหน้าแรก   \r\n\r\n\r\n\r\nย่อหน้าที่สอง\rบรรทัดต่อ\n \n\nสาม";

        Assert.Equal("ย่อหน้าแรก\n\nย่อหน้าที่สอง\nบรรทัดต่อ\n\nสาม", ArticleText.CleanBody(typed));
    }

    [Fact]
    public void The_zero_width_space_thai_writers_use_for_line_breaks_stays()
    {
        Assert.Equal("วัคซีน\u200Bแมว", ArticleText.CleanBody("วัคซีน\u200Bแมว"));
    }

    [Fact]
    public void A_title_is_one_line_with_single_spaces()
    {
        Assert.Equal("วัคซีน แมว", ArticleText.CleanLine("  วัคซีน\n\n  แมว\t "));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(800, 1)]
    [InlineData(801, 2)]
    [InlineData(20_000, 25)]
    [InlineData(200_000, 120)]
    public void Reading_time_is_estimated_from_length_and_kept_in_range(int length, int minutes)
    {
        Assert.Equal(minutes, ArticleText.EstimateReadingMinutes(new string('ก', length)));
    }
}
