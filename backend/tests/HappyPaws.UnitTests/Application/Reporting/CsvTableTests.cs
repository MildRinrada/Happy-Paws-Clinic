using System.Text;
using HappyPaws.Application.Reporting.Csv;

namespace HappyPaws.UnitTests.Application.Reporting;

/// <summary>The CSV an export writes: readable Thai, quoted where needed, and no formulas smuggled in.</summary>
public class CsvTableTests
{
    private static string[] Lines(CsvTable table)
    {
        var bytes = table.ToBytes();
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);

        return Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public void Thai_headers_and_values_survive_with_a_byte_order_mark()
    {
        var lines = Lines(new CsvTable(["วันที่", "สาขา"]).Add(new DateOnly(2026, 10, 7), "สาขาสุขุมวิท"));

        Assert.Equal(["วันที่,สาขา", "2026-10-07,สาขาสุขุมวิท"], lines);
    }

    [Fact]
    public void Commas_quotes_and_line_breaks_are_quoted()
    {
        var lines = Lines(new CsvTable(["a"]).Add("คุณสมใจ, \"ใจดี\""));

        Assert.Equal("\"คุณสมใจ, \"\"ใจดี\"\"\"", lines[1]);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://x\")")]
    [InlineData("+66")]
    [InlineData("-1+1")]
    [InlineData("@SUM(A1)")]
    public void Text_that_a_spreadsheet_would_run_is_defused(string name)
    {
        var lines = Lines(new CsvTable(["ชื่อ"]).Add(name));

        Assert.StartsWith("'", lines[1].TrimStart('"'));
    }

    [Fact]
    public void Numbers_stay_numbers_even_when_negative()
    {
        var lines = Lines(new CsvTable(["บาท", "จำนวน", "นาที", "ใช่"]).Add(CsvTable.Baht(-125_050), 3, 12.5, true));

        Assert.Equal("-1250.50,3,12.5,1", lines[1]);
    }

    [Fact]
    public void A_row_must_fill_every_column()
    {
        Assert.Throws<ArgumentException>(() => new CsvTable(["a", "b"]).Add("only one"));
    }

    [Fact]
    public void Empty_cells_are_empty()
    {
        var lines = Lines(new CsvTable(["a", "b"]).Add(null, "x"));

        Assert.Equal(",x", lines[1]);
    }
}
