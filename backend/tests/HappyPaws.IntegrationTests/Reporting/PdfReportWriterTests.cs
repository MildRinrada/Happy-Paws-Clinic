using System.Text;
using System.Text.RegularExpressions;
using HappyPaws.Application.Reporting.Tables;
using HappyPaws.Domain.Reporting;
using HappyPaws.Infrastructure.Reporting;
using static HappyPaws.IntegrationTests.Reporting.ReportFileSamples;

namespace HappyPaws.IntegrationTests.Reporting;

/// <summary>
/// The printable PDF an export writes. Its text is drawn as shaped Thai glyphs inside compressed
/// streams, so these tests read what a PDF keeps in the clear: the header, the page objects, and
/// the document's title.
/// </summary>
public partial class PdfReportWriterTests
{
    private static string Latin1(byte[] bytes) => Encoding.Latin1.GetString(bytes);

    /// <summary>Page objects, not the page tree (<c>/Type /Pages</c>).</summary>
    private static int PageCount(byte[] pdf) => PageObject().Matches(Latin1(pdf)).Count;

    [GeneratedRegex(@"/Type\s*/Page(?![a-zA-Z])")]
    private static partial Regex PageObject();

    [Fact]
    public void It_is_the_pdf_writer()
    {
        var writer = new PdfReportWriter();

        Assert.Equal(ReportFormat.Pdf, writer.Format);
        Assert.Equal("pdf", writer.Extension);
        Assert.Equal("application/pdf", writer.ContentType);
    }

    [Fact]
    public void A_short_report_is_one_page_of_pdf()
    {
        var pdf = new PdfReportWriter().Write(Table(), Heading);

        Assert.StartsWith("%PDF-", Latin1(pdf[..8]));
        Assert.Equal(1, PageCount(pdf));
    }

    [Fact]
    public void A_long_table_runs_onto_more_pages()
    {
        var pdf = new PdfReportWriter().Write(Table(rows: 300), Heading);

        Assert.InRange(PageCount(pdf), 5, 40);
    }

    [Fact]
    public void The_document_carries_the_reports_thai_title()
    {
        var pdf = Latin1(new PdfReportWriter().Write(Table(), Heading));

        // PDF text strings outside Latin-1 are UTF-16BE with a byte-order mark, written as hex.
        var utf16 = "FEFF" + Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(Heading.Title));
        Assert.Contains(utf16, pdf, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_report_with_no_rows_still_prints_its_page()
    {
        var pdf = new PdfReportWriter().Write(new ReportTable([ReportColumn.Text("ชื่อ")]), Heading);

        Assert.Equal(1, PageCount(pdf));
    }
}
