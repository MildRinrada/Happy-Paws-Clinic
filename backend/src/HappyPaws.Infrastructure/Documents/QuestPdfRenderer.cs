using HappyPaws.Application.MedicalRecords.GenerateDocument;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HappyPaws.Infrastructure.Documents;

/// <summary>
/// Draws a <see cref="PrintableDocument"/> as an A4 PDF.
/// <para>
/// QuestPDF, because Thai needs real text shaping: vowels and tone marks stack above and below the
/// consonant, and a renderer that places glyphs one after another prints them beside it instead.
/// The Sarabun typeface is embedded from this assembly, so the document reads the same on a server
/// with no Thai fonts installed.
/// </para>
/// <para>
/// Used under QuestPDF's Community licence, which covers organisations under its revenue threshold.
/// A clinic above it needs a commercial licence; see <c>docs/features/clinical-records.md</c>. The setup is
/// shared with the report exports (<see cref="QuestPdfSetup"/>).
/// </para>
/// </summary>
internal sealed class QuestPdfRenderer : IDocumentRenderer
{
    private const string Font = QuestPdfSetup.Font;

    public byte[] RenderPdf(PrintableDocument document)
    {
        QuestPdfSetup.EnsureReady();

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.8f, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontFamily(Font).FontSize(11));

            page.Header().Element(header => Header(header, document));
            page.Content().PaddingVertical(12).Element(content => Content(content, document));
            page.Footer().Element(footer => Footer(footer, document));
        })).GeneratePdf();
    }

    private static void Header(IContainer container, PrintableDocument document) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(8).Column(column =>
        {
            column.Item().Text(document.ClinicName).FontSize(16).Bold();

            foreach (var line in document.ClinicLines)
            {
                column.Item().Text(line).FontSize(9).FontColor(Colors.Grey.Darken1);
            }

            column.Item().PaddingTop(8).AlignCenter().Text(document.Title).FontSize(15).Bold();
        });

    private static void Content(IContainer container, PrintableDocument document) =>
        container.Column(column =>
        {
            column.Spacing(10);

            foreach (var section in document.Sections)
            {
                column.Item().Element(item => Section(item, section));
            }

            if (document.Signature is { } signature)
            {
                column.Item().PaddingTop(24).AlignRight().Width(220).Column(sign =>
                {
                    sign.Item().PaddingBottom(4).BorderBottom(0.5f).Height(28);
                    sign.Item().AlignCenter().Text($"({signature.Name})");
                    sign.Item().AlignCenter().Text(signature.Role).FontSize(9);

                    if (signature.LicenseNo is { } license)
                    {
                        sign.Item().AlignCenter().Text($"เลขที่ใบอนุญาต {license}").FontSize(9);
                    }
                });
            }
        });

    private static void Section(IContainer container, PrintableSection section) =>
        container.Column(column =>
        {
            column.Item().Text(section.Heading).Bold().FontSize(12);

            if (section.Fields is { Count: > 0 } fields)
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(130);
                        columns.RelativeColumn();
                    });

                    foreach (var field in fields)
                    {
                        table.Cell().PaddingVertical(1).Text(field.Label).FontColor(Colors.Grey.Darken2);
                        table.Cell().PaddingVertical(1).Text(field.Value);
                    }
                });
            }

            if (section.Table is { } data)
            {
                column.Item().Element(item => Table(item, data));
            }

            if (section.Paragraph is { } paragraph)
            {
                column.Item().Text(paragraph);
            }
        });

    private static void Table(IContainer container, PrintableTable data)
    {
        if (data.Rows.Count == 0)
        {
            container.Text("ไม่มีรายการ").FontColor(Colors.Grey.Darken1);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in data.Columns)
                {
                    columns.RelativeColumn();
                }
            });

            table.Header(header =>
            {
                foreach (var title in data.Columns)
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(title).Bold().FontSize(10);
                }
            });

            foreach (var row in data.Rows)
            {
                foreach (var value in row)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(value).FontSize(10);
                }
            }
        });
    }

    private static void Footer(IContainer container, PrintableDocument document) =>
        container.Row(row =>
        {
            row.RelativeItem().Text($"เลขที่เอกสาร {document.Reference}").FontSize(8).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().AlignRight().Text($"ออกเมื่อ {document.IssuedOn}").FontSize(8).FontColor(Colors.Grey.Darken1);
        });
}
