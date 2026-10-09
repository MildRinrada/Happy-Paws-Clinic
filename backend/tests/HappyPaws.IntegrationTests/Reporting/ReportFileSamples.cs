using HappyPaws.Application.Reporting.Files;
using HappyPaws.Application.Reporting.Tables;

namespace HappyPaws.IntegrationTests.Reporting;

/// <summary>A small table with one column of every kind, and a heading, for the file writers' tests.</summary>
internal static class ReportFileSamples
{
    public static readonly ReportFileHeading Heading = new(
        "รายรับรายวัน",
        "คลินิกสัตว์ Happy Paws",
        "สาขาสุขุมวิท",
        new DateOnly(2026, 10, 1),
        new DateOnly(2026, 10, 7),
        new DateTimeOffset(2026, 10, 8, 9, 30, 0, TimeSpan.FromHours(7)));

    public static ReportTable Table(int rows = 2)
    {
        var table = new ReportTable(
        [
            ReportColumn.Day("วันที่"),
            ReportColumn.Text("ชื่อ"),
            ReportColumn.Count("จำนวน"),
            ReportColumn.Minutes("เวลาเฉลี่ย (นาที)"),
            ReportColumn.Baht("สุทธิ (บาท)"),
            ReportColumn.Month("เดือน"),
            ReportColumn.Flag("walk-in"),
        ]);

        table.Add(new DateOnly(2026, 10, 7), "=HYPERLINK(\"http://x\",\"คลิก\")", 3, 12.5m, ReportTable.Baht(-125_050), new DateOnly(2026, 10, 1), true);
        table.Add(new DateOnly(2026, 10, 8), "น้องหมูกรอบ ผู้ใหญ่\u0001ใจดี", 1_250, null, ReportTable.Baht(9_999_999), new DateOnly(2026, 9, 1), false);

        for (var i = 2; i < rows; i++)
        {
            table.Add(new DateOnly(2026, 10, 1).AddDays(i % 7), $"แถวที่ {i}", i, 1.5m, ReportTable.Baht(i * 100), new DateOnly(2026, 10, 1), i % 2 == 0);
        }

        return table;
    }
}
