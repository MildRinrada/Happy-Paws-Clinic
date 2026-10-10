using HappyPaws.Application.Forecasting.Attendance;

namespace HappyPaws.UnitTests.Application.Forecasting;

/// <summary>Who may not turn up, and why, in words (<c>docs/contract-extensions.md § 33</c>).</summary>
public class AttendanceRiskRulesTests
{
    private static AttendanceVerdict Judge(int noShows = 0, int late = 0, int ahead = 3, bool confirmed = true, int window = 365) =>
        AttendanceRiskRules.Judge(new AttendanceSignals(noShows, late, window, ahead, confirmed));

    [Fact]
    public void A_confirmed_visit_by_a_reliable_customer_is_low_with_nothing_to_explain()
    {
        var verdict = Judge();

        Assert.Equal(AttendanceRisk.Low, verdict.Risk);
        Assert.Equal(0, verdict.Score);
        Assert.Empty(verdict.Reasons);
    }

    [Fact]
    public void Being_unconfirmed_alone_stays_low_but_is_said()
    {
        var verdict = Judge(confirmed: false);

        Assert.Equal(AttendanceRisk.Low, verdict.Risk);
        Assert.Equal(["เจ้าของยังไม่ได้ยืนยันนัด"], verdict.Reasons);
    }

    [Fact]
    public void One_no_show_is_medium_and_names_the_count_and_window()
    {
        var verdict = Judge(noShows: 1);

        Assert.Equal(AttendanceRisk.Medium, verdict.Risk);
        Assert.Equal(2, verdict.Score);
        Assert.Equal(["ไม่มาตามนัด 1 ครั้งใน 12 เดือนที่ผ่านมา"], verdict.Reasons);
    }

    [Fact]
    public void Two_no_shows_are_high_on_their_own()
    {
        var verdict = Judge(noShows: 2);

        Assert.Equal(AttendanceRisk.High, verdict.Risk);
        Assert.Contains("ไม่มาตามนัด 2 ครั้งใน 12 เดือนที่ผ่านมา", verdict.Reasons);
    }

    [Fact]
    public void Points_add_up_to_high()
    {
        // 2 for the no-show, 1 for the late cancellation, 1 for not confirming.
        var verdict = Judge(noShows: 1, late: 1, confirmed: false);

        Assert.Equal(AttendanceRisk.High, verdict.Risk);
        Assert.Equal(4, verdict.Score);
        Assert.Equal(
            ["ไม่มาตามนัด 1 ครั้งใน 12 เดือนที่ผ่านมา", "ยกเลิกกระชั้น 1 ครั้งใน 12 เดือนที่ผ่านมา", "เจ้าของยังไม่ได้ยืนยันนัด"],
            verdict.Reasons);
    }

    [Theory]
    [InlineData(29, 0)]
    [InlineData(30, 1)]
    [InlineData(45, 1)]
    public void A_visit_booked_a_month_or_more_ahead_scores_a_point(int daysAhead, int expectedScore)
    {
        var verdict = Judge(ahead: daysAhead);

        Assert.Equal(expectedScore, verdict.Score);

        if (expectedScore > 0)
        {
            Assert.Equal([$"จองไว้ล่วงหน้า {daysAhead} วัน"], verdict.Reasons);
        }
    }

    [Fact]
    public void Long_lead_and_unconfirmed_together_are_medium()
    {
        Assert.Equal(AttendanceRisk.Medium, Judge(ahead: 40, confirmed: false).Risk);
    }

    [Fact]
    public void Late_cancellations_count_a_point_each()
    {
        Assert.Equal(AttendanceRisk.Low, Judge(late: 1).Risk);
        Assert.Equal(AttendanceRisk.Medium, Judge(late: 2).Risk);
        Assert.Equal(AttendanceRisk.High, Judge(late: 4).Risk);
    }

    [Fact]
    public void A_window_other_than_a_year_is_said_in_days()
    {
        Assert.Equal(["ไม่มาตามนัด 1 ครั้งใน 90 วันที่ผ่านมา"], Judge(noShows: 1, window: 90).Reasons);
    }
}
