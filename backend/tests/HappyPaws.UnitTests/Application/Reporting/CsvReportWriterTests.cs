using System.Text;
using HappyPaws.Application.Reporting.Csv;
using HappyPaws.Application.Reporting.Files;
using HappyPaws.Application.Reporting.Tables;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.UnitTests.Application.Reporting;

/// <summary>The CSV an export writes: readable Thai, quoted where needed, and no formulas smuggled in.</summary>
public class CsvReportWriterTests
{
    private static readonly ReportFileHeading Heading = new(
        "รายรับรายวัน", "Happy Paws", "ทุกสาขา", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7), DateTimeOffset.UnixEpoch);

    private static string[] Lines(ReportTable table)
    {
        var bytes = new CsvReportWriter().Write(table, Heading);
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);

        return Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    }

    private static ReportTable Names(string name) => new ReportTable([ReportColumn.Text("ชื่อ")]).Add(name);

    [Fact]
    public void It_is_the_csv_writer()
    {
        var writer = new CsvReportWriter();

        Assert.Equal(ReportFormat.Csv, writer.Format);
        Assert.Equal("csv", writer.Extension);
        Assert.StartsWith("text/csv", writer.ContentType);
    }

    [Fact]
    public void Thai_headers_and_values_survive_with_a_byte_order_mark()
    {
        var lines = Lines(new ReportTable([ReportColumn.Day("วันที่"), ReportColumn.Text("สาขา")]).Add(new DateOnly(2026, 10, 7), "สาขาสุขุมวิท"));

        Assert.Equal(["วันที่,สาขา", "2026-10-07,สาขาสุขุมวิท"], lines);
    }

    [Fact]
    public void Commas_quotes_and_line_breaks_are_quoted()
    {
        var lines = Lines(Names("คุณสมใจ, \"ใจดี\""));

        Assert.Equal("\"คุณสมใจ, \"\"ใจดี\"\"\"", lines[1]);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://x\")")]
    [InlineData("+66")]
    [InlineData("-1+1")]
    [InlineData("@SUM(A1)")]
    public void Text_that_a_spreadsheet_would_run_is_defused(string name)
    {
        var lines = Lines(Names(name));

        Assert.StartsWith("'", lines[1].TrimStart('"'));
    }

    [Fact]
    public void Numbers_stay_numbers_even_when_negative()
    {
        var table = new ReportTable(
            [ReportColumn.Baht("บาท"), ReportColumn.Count("จำนวน"), ReportColumn.Minutes("นาที"), ReportColumn.Flag("ใช่")])
            .Add(ReportTable.Baht(-125_050), 3, 12.5, true);

        Assert.Equal("-1250.50,3,12.5,1", Lines(table)[1]);
    }

    [Fact]
    public void Empty_cells_are_empty()
    {
        var lines = Lines(new ReportTable([ReportColumn.Minutes("a"), ReportColumn.Text("b")]).Add(null, "x"));

        Assert.Equal(",x", lines[1]);
    }
}
