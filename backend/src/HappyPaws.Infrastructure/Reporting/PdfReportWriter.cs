using System.Globalization;
using HappyPaws.Application.Reporting.Files;
using HappyPaws.Application.Reporting.Tables;
using HappyPaws.Domain.Reporting;
using HappyPaws.Infrastructure.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HappyPaws.Infrastructure.Reporting;

/// <summary>
/// Writes a report as a printable PDF with QuestPDF, the same engine and embedded Sarabun typeface
/// as the medical documents (<see cref="QuestPdfSetup"/>; Community licence, see
/// <c>docs/features/clinical-records.md</c>).
/// <para>
/// A4 landscape, so the widest report (nine columns) fits without shrinking the type. Every page
/// carries the clinic, the report's name, the branches and the span at the top, and when it was
/// built and "หน้า n / m" at the bottom, so a single page that has left the binder still says what
/// it is. A long table runs onto as many pages as it needs, and its header row is repeated on each.
/// </para>
/// <para>
/// Dates are Thai with the Buddhist year, as Thai paperwork is dated ("7 ต.ค. 2569"); numbers sit on
/// the right, baht with two decimals and a thousands separator. A PDF has nothing a reader could
/// run, so text is printed as typed.
/// </para>
/// </summary>
internal sealed class PdfReportWriter : IReportFileWriter
{
    private static readonly CultureInfo Thai = CultureInfo.GetCultureInfo("th-TH");

    public ReportFormat Format => ReportFormat.Pdf;

    public string ContentType => "application/pdf";

    public string Extension => "pdf";

    public byte[] Write(ReportTable table, ReportFileHeading heading)
    {
        QuestPdfSetup.EnsureReady();

        return Document.Create(container => container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontFamily(QuestPdfSetup.Font).FontSize(9));

                page.Header().Element(header => Header(header, heading));
                page.Content().PaddingVertical(8).Element(content => Table(content, table));
                page.Footer().Element(footer => Footer(footer, heading));
            }))
            .WithMetadata(new DocumentMetadata
            {
                Title = heading.Title,
                Author = heading.ClinicName,
                Subject = $"{heading.Branches} {ThaiDate(heading.From)} ถึง {ThaiDate(heading.To)}",
                CreationDate = heading.GeneratedAt,
                ModifiedDate = heading.GeneratedAt,
            })
            .GeneratePdf();
    }

    private static void Header(IContainer container, ReportFileHeading heading) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(6).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(heading.ClinicName).FontSize(10).FontColor(Colors.Grey.Darken2);
                column.Item().Text(heading.Title).FontSize(15).Bold();
            });

            row.RelativeItem().AlignRight().AlignBottom().Column(column =>
            {
                column.Item().AlignRight().Text(BranchLine(heading.Branches));
                column.Item().AlignRight().Text($"ช่วงวันที่ {ThaiDate(heading.From)} ถึง {ThaiDate(heading.To)}");
            });
        });

    private static void Table(IContainer container, ReportTable data)
    {
        if (data.RowCount == 0)
        {
            container.PaddingTop(12).Text("ไม่มีข้อมูลในช่วงนี้").FontColor(Colors.Grey.Darken1);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var column in data.Columns)
                {
                    columns.RelativeColumn(column.Kind == ReportColumnKind.Text ? 2 : 1);
                }
            });

            // Repeated at the top of every page the table runs onto.
            table.Header(header =>
            {
                foreach (var column in data.Columns)
                {
                    var cell = header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(6);
                    (IsNumber(column.Kind) ? cell.AlignRight() : cell).Text(column.Header).Bold();
                }
            });

            for (var r = 0; r < data.Rows.Count; r++)
            {
                var background = r % 2 == 1 ? Colors.Grey.Lighten5 : Colors.White;

                for (var i = 0; i < data.Columns.Count; i++)
                {
                    var kind = data.Columns[i].Kind;
                    var cell = table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(6);
                    (IsNumber(kind) ? cell.AlignRight() : cell).Text(Shown(kind, data.Rows[r][i]));
                }
            }
        });
    }

    private static void Footer(IContainer container, ReportFileHeading heading) =>
        container.Row(row =>
        {
            row.RelativeItem().Text($"สร้างเมื่อ {ThaiDateTime(heading.GeneratedAt)}").FontSize(8).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                text.Span("หน้า ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });

    private static bool IsNumber(ReportColumnKind kind) =>
        kind is ReportColumnKind.Count or ReportColumnKind.Minutes or ReportColumnKind.Baht;

    /// <summary>A value as printed. Empty is a dash, so a blank cell reads as "none" rather than "missing".</summary>
    private static string Shown(ReportColumnKind kind, object? value) => value switch
    {
        null => "-",
        DateOnly month when kind == ReportColumnKind.Month => month.ToString("MMM yyyy", Thai),
        DateOnly day => ThaiDate(day),
        bool flag => flag ? "ใช่" : "ไม่",
        decimal baht when kind == ReportColumnKind.Baht => baht.ToString("#,##0.00", CultureInfo.InvariantCulture),
        decimal minutes => minutes.ToString("#,##0.0", CultureInfo.InvariantCulture),
        double minutes => minutes.ToString("#,##0.0", CultureInfo.InvariantCulture),
        IFormattable number => number.ToString("#,##0", CultureInfo.InvariantCulture),
        string text => Printable(text),
        _ => Printable(value.ToString() ?? string.Empty),
    };

    /// <summary>Text without control characters, which a font has no glyph for and would print as a box.</summary>
    private static string Printable(string text) =>
        text.Any(char.IsControl) ? string.Concat(text.Where(character => !char.IsControl(character))) : text;

    /// <summary>"สาขาสุขุมวิท" or "ทุกสาขา" already say what they are; a bare name gets the word in front.</summary>
    private static string BranchLine(string branches) =>
        branches.StartsWith("สาขา", StringComparison.Ordinal) || branches == "ทุกสาขา" ? branches : $"สาขา {branches}";

    /// <summary>"7 ต.ค. 2569": the Thai culture's calendar counts years from the Buddha.</summary>
    private static string ThaiDate(DateOnly day) => day.ToString("d MMM yyyy", Thai);

    private static string ThaiDateTime(DateTimeOffset instant) => instant.ToString("d MMM yyyy HH:mm", Thai) + " น.";
}
