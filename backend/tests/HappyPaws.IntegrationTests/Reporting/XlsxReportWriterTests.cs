using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;
using HappyPaws.Application.Reporting.Tables;
using HappyPaws.Domain.Reporting;
using HappyPaws.Infrastructure.Reporting;
using static HappyPaws.IntegrationTests.Reporting.ReportFileSamples;

namespace HappyPaws.IntegrationTests.Reporting;

/// <summary>
/// The Excel file an export writes, opened again with the Open XML SDK: a valid workbook, typed
/// cells, a frozen header, Thai intact, and not one formula.
/// </summary>
public class XlsxReportWriterTests
{
    private static SpreadsheetDocument Open(byte[] bytes) => SpreadsheetDocument.Open(new MemoryStream(bytes), isEditable: false);

    private static Worksheet SheetOf(SpreadsheetDocument document) => document.WorkbookPart!.WorksheetParts.Single().Worksheet!;

    private static Cell CellAt(Worksheet sheet, string reference) =>
        sheet.Descendants<Cell>().Single(cell => cell.CellReference?.Value == reference);

    private static decimal Number(Cell cell) => decimal.Parse(cell.CellValue!.Text, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static DateTime Date(Cell cell) => DateTime.FromOADate(double.Parse(cell.CellValue!.Text, CultureInfo.InvariantCulture));

    private static string NumberFormatOf(SpreadsheetDocument document, Cell cell)
    {
        var stylesheet = document.WorkbookPart!.WorkbookStylesPart!.Stylesheet!;
        var format = stylesheet.CellFormats!.Elements<CellFormat>().ElementAt((int)(cell.StyleIndex?.Value ?? 0));
        var id = format.NumberFormatId?.Value ?? 0;

        return id switch
        {
            3 => "#,##0",
            4 => "#,##0.00",
            _ => stylesheet.NumberingFormats?.Elements<NumberingFormat>()
                .SingleOrDefault(custom => custom.NumberFormatId?.Value == id)?.FormatCode?.Value ?? "General",
        };
    }

    [Fact]
    public void It_is_the_xlsx_writer()
    {
        var writer = new XlsxReportWriter();

        Assert.Equal(ReportFormat.Xlsx, writer.Format);
        Assert.Equal("xlsx", writer.Extension);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", writer.ContentType);
    }

    [Fact]
    public void The_workbook_is_valid_open_xml_with_one_sheet_named_after_the_report()
    {
        using var document = Open(new XlsxReportWriter().Write(Table(), Heading));

        Assert.Empty(new OpenXmlValidator().Validate(document, TestContext.Current.CancellationToken));
        var sheet = Assert.Single(document.WorkbookPart!.Workbook!.Sheets!.Elements<Sheet>());
        Assert.Equal("รายรับรายวัน", sheet.Name?.Value);
        Assert.Equal("รายรับรายวัน", document.PackageProperties.Title);
        Assert.Equal("คลินิกสัตว์ Happy Paws", document.PackageProperties.Creator);
        Assert.Contains("สาขาสุขุมวิท", document.PackageProperties.Subject);
    }

    [Fact]
    public void The_header_row_is_thai_text_and_stays_frozen()
    {
        using var document = Open(new XlsxReportWriter().Write(Table(), Heading));
        var sheet = SheetOf(document);

        var header = CellAt(sheet, "A1");
        Assert.Equal(CellValues.InlineString, header.DataType?.Value);
        Assert.Equal("วันที่", header.InlineString!.Text!.Text);
        Assert.Equal("เวลาเฉลี่ย (นาที)", CellAt(sheet, "D1").InlineString!.Text!.Text);

        var pane = sheet.Descendants<Pane>().Single();
        Assert.Equal(PaneStateValues.Frozen, pane.State?.Value);
        Assert.Equal("A2", pane.TopLeftCell?.Value);
        Assert.Equal(1d, pane.VerticalSplit?.Value);

        var widths = sheet.Descendants<Column>().ToList();
        Assert.Equal(7, widths.Count);
        Assert.All(widths, column => Assert.InRange(column.Width!.Value, 8d, 60d));
    }

    [Fact]
    public void Numbers_money_dates_and_flags_are_typed_cells()
    {
        using var document = Open(new XlsxReportWriter().Write(Table(), Heading));
        var sheet = SheetOf(document);

        var day = CellAt(sheet, "A2");
        Assert.Null(day.DataType);
        Assert.Equal(new DateTime(2026, 10, 7), Date(day));
        Assert.Equal("yyyy-mm-dd", NumberFormatOf(document, day));

        var count = CellAt(sheet, "C3");
        Assert.Equal(CellValues.Number, count.DataType?.Value);
        Assert.Equal(1_250m, Number(count));
        Assert.Equal("#,##0", NumberFormatOf(document, count));

        var minutes = CellAt(sheet, "D2");
        Assert.Equal(12.5m, Number(minutes));
        Assert.Equal("0.0", NumberFormatOf(document, minutes));

        var baht = CellAt(sheet, "E2");
        Assert.Equal(CellValues.Number, baht.DataType?.Value);
        Assert.Equal(-1_250.50m, Number(baht));
        Assert.Equal("#,##0.00", NumberFormatOf(document, baht));
        Assert.Equal(99_999.99m, Number(CellAt(sheet, "E3")));

        var month = CellAt(sheet, "F2");
        Assert.Equal(new DateTime(2026, 10, 1), Date(month));
        Assert.Equal("yyyy-mm", NumberFormatOf(document, month));

        var flag = CellAt(sheet, "G2");
        Assert.Equal(CellValues.Boolean, flag.DataType?.Value);
        Assert.Equal("1", flag.CellValue!.Text);
        Assert.Equal("0", CellAt(sheet, "G3").CellValue!.Text);
    }

    [Fact]
    public void An_empty_value_is_an_empty_cell()
    {
        using var document = Open(new XlsxReportWriter().Write(Table(), Heading));

        Assert.DoesNotContain(SheetOf(document).Descendants<Cell>(), cell => cell.CellReference?.Value == "D3");
    }

    [Fact]
    public void Text_that_looks_like_a_formula_is_kept_as_text_and_nothing_is_a_formula()
    {
        using var document = Open(new XlsxReportWriter().Write(Table(), Heading));
        var sheet = SheetOf(document);

        var name = CellAt(sheet, "B2");
        Assert.Equal(CellValues.InlineString, name.DataType?.Value);
        Assert.Equal("=HYPERLINK(\"http://x\",\"คลิก\")", name.InlineString!.Text!.Text);
        Assert.Empty(sheet.Descendants<CellFormula>());
    }

    [Fact]
    public void Thai_text_survives_and_characters_xml_cannot_hold_are_dropped()
    {
        using var document = Open(new XlsxReportWriter().Write(Table(), Heading));

        Assert.Equal("น้องหมูกรอบ ผู้ใหญ่ใจดี", CellAt(SheetOf(document), "B3").InlineString!.Text!.Text);
    }

    [Fact]
    public void A_report_with_no_rows_is_still_a_valid_workbook_with_its_header()
    {
        var empty = new ReportTable([ReportColumn.Text("ชื่อ"), ReportColumn.Baht("บาท")]);

        using var document = Open(new XlsxReportWriter().Write(empty, Heading));

        Assert.Empty(new OpenXmlValidator().Validate(document, TestContext.Current.CancellationToken));
        Assert.Single(SheetOf(document).Descendants<Row>());
    }
}
