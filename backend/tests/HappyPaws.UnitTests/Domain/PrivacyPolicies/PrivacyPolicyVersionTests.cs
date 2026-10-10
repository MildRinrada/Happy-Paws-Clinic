using HappyPaws.Domain.Common;
using HappyPaws.Domain.PrivacyPolicies;

namespace HappyPaws.UnitTests.Domain.PrivacyPolicies;

/// <summary>
/// The template, the section rules, and the publishing rules of the clinic's privacy notice
/// (<c>docs/contract-extensions.md § 35</c>). A published version is what a consent points at, so
/// it never changes.
/// </summary>
public class PrivacyPolicyVersionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static PrivacyPolicySection Section(string anchor = "cookies", string heading = "คุกกี้", string body = "ย่อหน้า") =>
        new(anchor, heading, body);

    [Fact]
    public void The_shipped_template_is_valid_and_versioned()
    {
        Assert.Empty(PrivacyPolicyRules.Problems(PrivacyPolicyRules.Clean(PrivacyPolicyTemplate.Sections)));
        Assert.Matches(@"^\d{4}-\d{2}-template-\d+$", PrivacyPolicyTemplate.Version);
        Assert.InRange(PrivacyPolicyTemplate.Sections.Count, 1, PrivacyPolicyRules.MaxSections);

        // Already in the form it is kept in: the migration stores it as written.
        Assert.Equal(PrivacyPolicyTemplate.Sections, PrivacyPolicyRules.Clean(PrivacyPolicyTemplate.Sections));
    }

    [Fact]
    public void The_template_describes_the_staff_ai_assistant_and_leaves_the_controller_to_the_clinic_settings()
    {
        var ai = Assert.Single(PrivacyPolicyTemplate.Sections, section => section.Anchor == "ai-assistants");

        Assert.Contains("Google Gemini", ai.Body);
        Assert.Contains("เลขบัตรประชาชน", ai.Body);
        Assert.Contains("ไม่ถูกนำไปใช้ฝึกโมเดล", ai.Body);
        Assert.DoesNotContain(PrivacyPolicyTemplate.Sections, section => section.Anchor == PrivacyPolicyRules.ControllerAnchor);
    }

    [Fact]
    public void The_template_uses_no_dash_or_middle_dot_characters()
    {
        foreach (var section in PrivacyPolicyTemplate.Sections)
        {
            foreach (var forbidden in new[] { '\u2014', '\u2013', '\u00B7' })
            {
                Assert.DoesNotContain(forbidden, section.Heading + section.Body);
            }
        }
    }

    [Fact]
    public void A_draft_from_the_template_is_a_draft_that_remembers_the_template()
    {
        var draft = PrivacyPolicyVersion.DraftFromTemplate("stf_1");

        Assert.False(draft.IsPublished);
        Assert.True(draft.IsFromTemplate);
        Assert.Null(draft.Label);
        Assert.Equal(PrivacyPolicyTemplate.Version, draft.TemplateVersion);
        Assert.StartsWith("ppv_", draft.Id);
        Assert.Equal(PrivacyPolicyTemplate.Sections.Count, draft.Sections.Count);
    }

    [Fact]
    public void A_copy_keeps_the_text_and_the_template_it_descends_from()
    {
        var source = PrivacyPolicyVersion.DraftFromTemplate(null);
        var copy = PrivacyPolicyVersion.DraftFrom(source, "stf_2");

        Assert.Equal(PrivacyPolicySource.Version, copy.CreatedFrom);
        Assert.Equal(source.Id, copy.BasedOnVersionId);
        Assert.Equal(source.TemplateVersion, copy.TemplateVersion);
        Assert.Equal(source.Sections, copy.Sections);
        Assert.False(copy.IsFromTemplate);
    }

    [Fact]
    public void Section_problems_are_reported_under_the_field_the_editor_shows()
    {
        var problems = PrivacyPolicyRules.Problems(PrivacyPolicyRules.Clean(
        [
            Section("Data We Collect"),
            Section("cookies", heading: "  "),
            Section("cookies", body: "<b>ตัวหนา</b>"),
            Section(PrivacyPolicyRules.ControllerAnchor),
            Section("long", body: new string('ก', PrivacyPolicyRules.BodyMaxLength + 1)),
        ]));

        Assert.Equal(
            ["sections[0].anchor", "sections[1].heading", "sections[2].anchor", "sections[2].body", "sections[3].anchor", "sections[4].body"],
            problems.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void No_sections_or_too_many_are_refused()
    {
        Assert.Contains("sections", PrivacyPolicyRules.Problems([]).Keys);

        var many = Enumerable.Range(0, PrivacyPolicyRules.MaxSections + 1).Select(index => Section($"s{index}")).ToList();
        Assert.Contains("sections", PrivacyPolicyRules.Problems(many).Keys);
    }

    [Fact]
    public void A_draft_is_rewritten_with_its_text_cleaned()
    {
        var draft = PrivacyPolicyVersion.DraftFromTemplate("stf_1");

        draft.ReplaceSections([Section(" cookies ", " คุกกี้\tของเว็บ ", "ย่อหน้า   \r\n\r\n\r\n- รายการ")]);

        var section = Assert.Single(draft.Sections);
        Assert.Equal(new PrivacyPolicySection("cookies", "คุกกี้ ของเว็บ", "ย่อหน้า\n\n- รายการ"), section);
    }

    [Fact]
    public void Publishing_fixes_the_label_date_and_text_for_good()
    {
        var draft = PrivacyPolicyVersion.DraftFromTemplate("stf_1");

        draft.Publish(" 2026-11 ", Today.AddDays(5), legalReviewDone: true, "stf_owner", Now, Today);

        Assert.True(draft.IsPublished);
        Assert.Equal("2026-11", draft.Label);
        Assert.Equal(Today.AddDays(5), draft.EffectiveDate);
        Assert.True(draft.LegalReviewDone);
        Assert.Equal("stf_owner", draft.PublishedByStaffId);
        Assert.Equal(Now, draft.PublishedAt);

        var edit = Assert.Throws<StateConflictException>(() => draft.ReplaceSections([Section()]));
        Assert.Equal("published", edit.CurrentState);

        Assert.Throws<StateConflictException>(() => draft.Publish("2026-12", Today, false, "stf_owner", Now, Today));
    }

    [Theory]
    [InlineData("2026 11")]
    [InlineData("-2026")]
    [InlineData("ฉบับที่ 2")]
    [InlineData("2026/11")]
    [InlineData("")]
    [InlineData(PrivacyPolicyRules.LegacyLabel)]
    public void A_label_that_is_not_a_clean_address_or_is_the_legacy_one_is_refused(string label)
    {
        var draft = PrivacyPolicyVersion.DraftFromTemplate("stf_1");

        Assert.Throws<DomainException>(() => draft.Publish(label, Today, false, "stf_owner", Now, Today));
        Assert.False(draft.IsPublished);
    }

    [Fact]
    public void The_effective_date_may_be_today_or_later_but_not_earlier()
    {
        var past = PrivacyPolicyVersion.DraftFromTemplate("stf_1");
        Assert.Throws<DomainException>(() => past.Publish("2026-11", Today.AddDays(-1), false, "stf_owner", Now, Today));

        var today = PrivacyPolicyVersion.DraftFromTemplate("stf_1");
        today.Publish("2026-11", Today, false, "stf_owner", Now, Today);
        Assert.Equal(Today, today.EffectiveDate);
    }

    [Theory]
    [InlineData("2026-11", true)]
    [InlineData("v2", true)]
    [InlineData("2027.01_a", true)]
    [InlineData("a234567890123456789012345678901b", true)]
    [InlineData("a2345678901234567890123456789012c", false)]
    public void A_label_is_short_and_safe_in_an_address(string label, bool expected)
    {
        Assert.Equal(expected, PrivacyPolicyRules.IsLabel(label));
    }
}
