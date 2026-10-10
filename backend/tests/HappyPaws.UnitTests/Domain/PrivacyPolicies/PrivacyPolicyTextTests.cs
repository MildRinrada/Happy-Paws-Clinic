using HappyPaws.Domain.PrivacyPolicies;

namespace HappyPaws.UnitTests.Domain.PrivacyPolicies;

/// <summary>
/// The body format of a privacy notice section (<c>docs/contract-extensions.md § 35</c>): blank
/// line, <c>- </c> list items, and links to the clinic's own pages. Nothing else is markup, and
/// nothing in it can lead a reader off the clinic's site.
/// </summary>
public class PrivacyPolicyTextTests
{
    private static string Text(IReadOnlyList<PolicyInline> inlines) => string.Concat(inlines.Select(inline => inline.Text));

    [Fact]
    public void A_blank_line_separates_paragraphs_and_lines_next_to_each_other_are_one_paragraph()
    {
        var blocks = PrivacyPolicyText.Parse("บรรทัดแรก\nบรรทัดต่อ\n\n\nย่อหน้าที่สอง");

        Assert.Equal(2, blocks.Count);
        Assert.All(blocks, block => Assert.False(block.IsList));
        Assert.Equal("บรรทัดแรก บรรทัดต่อ", Text(blocks[0].Items[0]));
        Assert.Equal("ย่อหน้าที่สอง", Text(blocks[1].Items[0]));
    }

    [Fact]
    public void Lines_starting_with_a_hyphen_and_a_space_are_one_list_between_paragraphs()
    {
        var blocks = PrivacyPolicyText.Parse("ก่อนรายการ\n- หนึ่ง\n  - สอง  \nหลังรายการ\n-ไม่ใช่รายการ");

        Assert.Equal(3, blocks.Count);
        Assert.False(blocks[0].IsList);
        Assert.True(blocks[1].IsList);
        Assert.Equal(["หนึ่ง", "สอง"], blocks[1].Items.Select(Text));
        Assert.Equal("หลังรายการ -ไม่ใช่รายการ", Text(blocks[2].Items[0]));
    }

    [Fact]
    public void A_link_to_a_site_path_becomes_a_link_inside_its_paragraph_or_item()
    {
        var blocks = PrivacyPolicyText.Parse("ดูที่ [บัญชีของฉัน](/portal/account) ได้เลย\n\n- [ติดต่อ](/contact#map)");

        var paragraph = blocks[0].Items[0];
        Assert.Equal(3, paragraph.Count);
        Assert.Equal("ดูที่ ", paragraph[0].Text);
        Assert.Equal(new PolicyInline("บัญชีของฉัน", "/portal/account"), paragraph[1]);
        Assert.Equal(" ได้เลย", paragraph[2].Text);

        var item = Assert.Single(blocks[1].Items);
        Assert.Equal("/contact#map", Assert.Single(item).Path);
    }

    [Theory]
    [InlineData("[คลิก](https://evil.example/login)")]
    [InlineData("[คลิก](//evil.example)")]
    [InlineData("[คลิก](mailto:a@b.c)")]
    [InlineData("[คลิก](evil.example/page)")]
    public void A_link_that_leaves_the_clinic_site_is_refused_on_save_and_read_as_text(string body)
    {
        var problem = Assert.Single(PrivacyPolicyText.Problems(body));
        Assert.Contains("หน้าในเว็บของคลินิกเอง", problem);

        var inline = Assert.Single(PrivacyPolicyText.Parse(body)[0].Items[0]);
        Assert.False(inline.IsLink);
        Assert.Equal(body, inline.Text);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("ดู <a href=\"/x\">ที่นี่</a>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("< /div>")]
    [InlineData("<!-- comment -->")]
    public void Html_tags_are_refused(string body)
    {
        Assert.Contains(PrivacyPolicyText.Problems(body), problem => problem.Contains("HTML"));
    }

    [Theory]
    [InlineData("อุณหภูมิ < 39 องศา")]
    [InlineData("รักน้อง <3")]
    [InlineData("ดูเว็บ https://www.pdpc.or.th ได้")]
    [InlineData("[ชื่อในวงเล็บ] ไม่ใช่ลิงก์")]
    public void Ordinary_text_with_brackets_or_an_address_is_accepted_and_shown_as_typed(string body)
    {
        Assert.Empty(PrivacyPolicyText.Problems(body));
        Assert.Equal(body, Text(PrivacyPolicyText.Parse(body)[0].Items[0]));
    }

    [Fact]
    public void A_bare_hyphen_or_an_empty_body_is_refused()
    {
        Assert.NotEmpty(PrivacyPolicyText.Problems("- หนึ่ง\n-\n- สาม"));
        Assert.Equal(["ต้องมีเนื้อหา"], PrivacyPolicyText.Problems("\n\n  \n"));
    }

    [Theory]
    [InlineData("/portal/account", true)]
    [InlineData("/privacy#cookies", true)]
    [InlineData("/book?service=vaccine", true)]
    [InlineData("/", true)]
    [InlineData("//evil.example", false)]
    [InlineData("/a b", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("https://happypaws.example/portal", false)]
    [InlineData("/\\evil.example", false)]
    public void Only_a_path_of_the_clinic_site_counts_as_one(string path, bool expected)
    {
        Assert.Equal(expected, PrivacyPolicyText.IsSitePath(path));
    }
}
