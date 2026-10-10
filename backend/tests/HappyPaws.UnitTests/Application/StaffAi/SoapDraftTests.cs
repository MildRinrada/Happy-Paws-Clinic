using HappyPaws.Application.StaffAi.Prompts;
using HappyPaws.Application.StaffAi.SoapDraft;

namespace HappyPaws.UnitTests.Application.StaffAi;

/// <summary>Reading the model's draft, holding it to the form, and flagging what the notes do not say.</summary>
public class SoapDraftTests
{
    private static readonly IReadOnlyList<string> Systems = SoapDraftPrompt.DefaultBodySystems;

    [Fact]
    public void The_draft_is_read_from_fenced_json_and_held_to_the_form()
    {
        const string answer =
            """
            นี่คือร่างค่ะ
            ```json
            {"chief_complaint":"อาเจียน","subjective":"อาเจียน 2 วัน","objective":"ท้องอืด",
             "assessment":"","plan":"นัดดูอาการ",
             "examination":[{"body_system":"ระบบทางเดินอาหาร","is_normal":false,"note":"ท้องอืด"},
                            {"body_system":"ระบบทางเดินอาหาร","is_normal":true,"note":"ซ้ำ"},
                            {"body_system":"ม้าม","is_normal":true,"note":"ไม่อยู่ในฟอร์ม"},
                            {"body_system":"ตา","is_normal":true}]}
            ```
            """;

        var draft = SoapDraftParser.Parse(answer, Systems);

        Assert.NotNull(draft);
        Assert.Equal("อาเจียน 2 วัน", draft.Subjective);
        Assert.Equal(string.Empty, draft.Assessment);
        Assert.Equal(2, draft.Examination.Count);
        Assert.Equal(new DraftFindingDto("ระบบทางเดินอาหาร", false, "ท้องอืด"), draft.Examination[0]);
        Assert.Equal(new DraftFindingDto("ตา", true, null), draft.Examination[1]);
    }

    [Theory]
    [InlineData("ขอโทษค่ะ ไม่สามารถทำได้")]
    [InlineData("{ not json }")]
    [InlineData("[1,2,3]")]
    public void An_answer_that_is_not_a_json_object_is_not_a_draft(string answer)
    {
        Assert.Null(SoapDraftParser.Parse(answer, Systems));
    }

    [Fact]
    public void A_field_longer_than_the_form_takes_is_cut()
    {
        var answer = $$"""{"chief_complaint":"{{new string('ก', 2500)}}"}""";

        Assert.Equal(SoapDraftParser.ChiefComplaintMax, SoapDraftParser.Parse(answer, Systems)!.ChiefComplaint.Length);
    }

    [Fact]
    public void Doses_numbers_and_terms_the_notes_do_not_contain_are_flagged()
    {
        const string notes = "สุนัขอาเจียน 2 วัน T 39.5 ให้ Metronidazole 250mg";

        var warnings = UnverifiedTerms.Find(
            [
                ("subjective", "อาเจียน 2 วัน อุณหภูมิ 39.5"),
                ("plan", "Metronidazole 250 mg และ Cerenia 2 mg/kg วัดชีพจร 120"),
            ],
            notes);

        Assert.Contains(warnings, warning => warning is { Text: "Cerenia", Reason: UnverifiedTerms.TermNotInNotes, Field: "plan" });
        Assert.Contains(warnings, warning => warning is { Text: "2 mg/kg", Reason: UnverifiedTerms.DoseNotInNotes });
        Assert.Contains(warnings, warning => warning is { Text: "120", Reason: UnverifiedTerms.NumberNotInNotes });

        // In the notes, even written differently: not flagged.
        Assert.DoesNotContain(warnings, warning => warning.Text.Contains("Metronidazole", StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, warning => warning.Text == "250 mg");
        Assert.DoesNotContain(warnings, warning => warning.Text is "39.5" or "2");
        Assert.Equal(3, warnings.Count);
    }

    [Fact]
    public void A_draft_that_only_restates_the_notes_has_no_warnings()
    {
        const string notes = "แมวไม่กินอาหาร 3 วัน ตรวจช่องปากพบเหงือกอักเสบ";

        var warnings = UnverifiedTerms.Find(
            [("subjective", "ไม่กินอาหาร 3 วัน"), ("examination", "ช่องปากและฟัน เหงือกอักเสบ"), ("plan", "")],
            notes);

        Assert.Empty(warnings);
    }

    [Fact]
    public void Placeholders_are_not_read_as_numbers()
    {
        Assert.Empty(UnverifiedTerms.Find([("subjective", "[OWNER_1] แจ้งว่าอาเจียน")], "เจ้าของแจ้งว่าอาเจียน"));
    }
}
