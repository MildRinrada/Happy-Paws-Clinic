using HappyPaws.Application.StaffAi.PetSummary;

namespace HappyPaws.UnitTests.Application.StaffAi;

/// <summary>Only references the model was actually given survive into the summary.</summary>
public class CitationsTests
{
    private static readonly SummarySource[] Sources =
    [
        new("R1", "allergy", "alg_1", new DateOnly(2026, 1, 5)),
        new("R2", "visit", "con_2", new DateOnly(2026, 9, 1)),
        new("R3", "medicine", "rx_3", new DateOnly(2026, 9, 1)),
    ];

    [Fact]
    public void Known_references_are_kept_and_mapped_to_kind_id_and_date_in_order_of_first_use()
    {
        var (text, cited) = Citations.Validate("แพ้เพนิซิลลิน [R1] ได้รับยาอยู่ [R3] ครั้งล่าสุด [R2][R1]", Sources);

        Assert.Equal("แพ้เพนิซิลลิน [R1] ได้รับยาอยู่ [R3] ครั้งล่าสุด [R2][R1]", text);
        Assert.Equal(["R1", "R3", "R2"], cited.Select(citation => citation.Ref));
        Assert.Equal("rx_3", cited[1].Id);
        Assert.Equal("medicine", cited[1].Kind);
        Assert.Equal(new DateOnly(2026, 9, 1), cited[1].Date);
    }

    [Fact]
    public void Unknown_references_are_dropped_from_the_text_and_the_list()
    {
        var (text, cited) = Citations.Validate("น้ำหนักลดลง [R9] และมีไข้ [R7, R2] .", Sources);

        Assert.DoesNotContain("R9", text);
        Assert.DoesNotContain("R7", text);
        Assert.Equal("น้ำหนักลดลง และมีไข้ [R2].", text);
        Assert.Equal(["R2"], cited.Select(citation => citation.Ref));
    }

    [Fact]
    public void Lower_case_and_grouped_markers_are_normalised()
    {
        var (text, cited) = Citations.Validate("ข้อมูล [r1; R3]", Sources);

        Assert.Equal("ข้อมูล [R1][R3]", text);
        Assert.Equal(2, cited.Count);
    }

    [Fact]
    public void A_summary_with_no_markers_has_no_citations()
    {
        var (text, cited) = Citations.Validate("ไม่มีข้อมูลการรักษา", Sources);

        Assert.Equal("ไม่มีข้อมูลการรักษา", text);
        Assert.Empty(cited);
    }
}
