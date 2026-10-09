using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HappyPaws.Application.Reporting.Files;
using HappyPaws.Application.Reporting.Tables;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.Infrastructure.Reporting;

/// <summary>
/// Writes a report as an Excel workbook (<c>.xlsx</c>) with the Open XML SDK (MIT, Microsoft).
/// <para>
/// One sheet named after the report. Row 1 is the header, bold and frozen, so a pivot table or a
/// filter finds it where it expects; the heading (clinic, branches, span) goes in the file's
/// properties instead of above the table. Cells are typed: counts and baht are numbers (baht with
/// two decimals and a thousands separator), days and months are real dates, flags are booleans,
/// so a sum or a date filter works without converting anything.
/// </para>
/// <para>
/// <b>No formulas, ever.</b> Every cell is written as a value; text is an inline string, which a
/// spreadsheet shows and never runs, so a name typed as <c>=HYPERLINK(...)</c> stays those
/// characters. Unlike CSV there is nothing to defuse and the text is kept exactly. Characters XML
/// cannot carry are dropped, and text is cut at Excel's 32,767-character cell limit.
/// </para>
/// </summary>
internal sealed class XlsxReportWriter : IReportFileWriter
{
    private const int MaxCellText = 32_767;

    // Cell formats, by index into the stylesheet's cellXfs below.
    private const uint TextStyle = 0;
    private const uint HeaderStyle = 1;
    private const uint CountStyle = 2;
    private const uint MinutesStyle = 3;
    private const uint BahtStyle = 4;
    private const uint DayStyle = 5;
    private const uint MonthStyle = 6;

    public ReportFormat Format => ReportFormat.Xlsx;

    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public string Extension => "xlsx";

    public byte[] Write(ReportTable table, ReportFileHeading heading)
    {
        using var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            document.PackageProperties.Title = heading.Title;
            document.PackageProperties.Subject = Subject(heading);
            document.PackageProperties.Creator = heading.ClinicName;
            document.PackageProperties.Created = heading.GeneratedAt.UtcDateTime;

            var workbook = document.AddWorkbookPart();
            workbook.Workbook = new Workbook();

            var styles = workbook.AddNewPart<WorkbookStylesPart>();
            styles.Stylesheet = Stylesheet();

            var sheet = workbook.AddNewPart<WorksheetPart>();
            sheet.Worksheet = Worksheet(table);

            workbook.Workbook.AppendChild(new Sheets(new Sheet
            {
                Id = workbook.GetIdOfPart(sheet),
                SheetId = 1,
                Name = SheetName(heading.Title),
            }));
        }

        return stream.ToArray();
    }

    private static Worksheet Worksheet(ReportTable table)
    {
        var data = new SheetData();
        var header = new Row { RowIndex = 1 };

        for (var i = 0; i < table.Columns.Count; i++)
        {
            header.AppendChild(TextCell(Reference(i, 1), table.Columns[i].Header, HeaderStyle));
        }

        data.AppendChild(header);

        for (var r = 0; r < table.Rows.Count; r++)
        {
            var rowIndex = (uint)(r + 2);
            var row = new Row { RowIndex = rowIndex };
            var values = table.Rows[r];

            for (var i = 0; i < table.Columns.Count; i++)
            {
                if (values[i] is { } value)
                {
                    row.AppendChild(Cell(Reference(i, rowIndex), table.Columns[i].Kind, value));
                }
            }

            data.AppendChild(row);
        }

        // Element order matters to Excel: sheetViews, then cols, then sheetData.
        return new Worksheet(FrozenHeader(), Columns(table), data);
    }

    private static Cell Cell(string reference, ReportColumnKind kind, object value) => kind switch
    {
        ReportColumnKind.Count => NumberCell(reference, Convert.ToDecimal(value, CultureInfo.InvariantCulture), CountStyle),
        ReportColumnKind.Minutes => NumberCell(reference, Convert.ToDecimal(value, CultureInfo.InvariantCulture), MinutesStyle),
        ReportColumnKind.Baht => NumberCell(reference, (decimal)value, BahtStyle),
        ReportColumnKind.Day => DateCell(reference, (DateOnly)value, DayStyle),
        ReportColumnKind.Month => DateCell(reference, (DateOnly)value, MonthStyle),
        ReportColumnKind.Flag => new Cell
        {
            CellReference = reference,
            DataType = CellValues.Boolean,
            CellValue = new CellValue((bool)value ? "1" : "0"),
        },
        _ => TextCell(reference, (string)value, TextStyle),
    };

    private static Cell NumberCell(string reference, decimal value, uint style) => new()
    {
        CellReference = reference,
        DataType = CellValues.Number,
        CellValue = new CellValue(value),
        StyleIndex = style,
    };

    /// <summary>A date is a number of days since 1899-12-30 with a date format; that is what Excel sorts and filters as a date.</summary>
    private static Cell DateCell(string reference, DateOnly day, uint style) => new()
    {
        CellReference = reference,
        CellValue = new CellValue(day.ToDateTime(TimeOnly.MinValue).ToOADate()),
        StyleIndex = style,
    };

    private static Cell TextCell(string reference, string text, uint style) => new()
    {
        CellReference = reference,
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(XmlSafe(text)) { Space = SpaceProcessingModeValues.Preserve }),
        StyleIndex = style == TextStyle ? null : style,
    };

    private static SheetViews FrozenHeader() => new(new SheetView(
        new Pane
        {
            VerticalSplit = 1,
            TopLeftCell = "A2",
            ActivePane = PaneValues.BottomLeft,
            State = PaneStateValues.Frozen,
        },
        new Selection { Pane = PaneValues.BottomLeft, ActiveCell = "A2", SequenceOfReferences = new ListValue<StringValue> { InnerText = "A2" } })
    {
        TabSelected = true,
        WorkbookViewId = 0,
    });

    /// <summary>Each column as wide as its longest value, within reason, so nothing opens as <c>####</c>.</summary>
    private static Columns Columns(ReportTable table)
    {
        var columns = new Columns();

        for (var i = 0; i < table.Columns.Count; i++)
        {
            var column = table.Columns[i];
            var widest = Width(column.Header) + 2;

            foreach (var row in table.Rows)
            {
                widest = Math.Max(widest, Width(Shown(column.Kind, row[i])));
            }

            columns.AppendChild(new Column
            {
                Min = (uint)(i + 1),
                Max = (uint)(i + 1),
                Width = Math.Clamp(widest + 2, 8, 60),
                CustomWidth = true,
            });
        }

        return columns;
    }

    /// <summary>Roughly what a cell shows, for its width.</summary>
    private static string Shown(ReportColumnKind kind, object? value) => value switch
    {
        null => string.Empty,
        decimal number when kind == ReportColumnKind.Baht => number.ToString("#,##0.00", CultureInfo.InvariantCulture),
        DateOnly when kind == ReportColumnKind.Month => "2026-10",
        DateOnly => "2026-10-01",
        bool => "FALSE",
        IFormattable formattable => formattable.ToString("#,##0.#", CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary>Characters that take up room: Thai vowel and tone marks sit above or below a consonant and add none.</summary>
    private static int Width(string text)
    {
        var width = 0;

        foreach (var rune in text.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);

            if (category is not UnicodeCategory.NonSpacingMark and not UnicodeCategory.EnclosingMark)
            {
                width++;
            }
        }

        return width;
    }

    private static Stylesheet Stylesheet() => new(
        new NumberingFormats(
            new NumberingFormat { NumberFormatId = 164, FormatCode = "0.0" },
            new NumberingFormat { NumberFormatId = 165, FormatCode = "yyyy-mm-dd" },
            new NumberingFormat { NumberFormatId = 166, FormatCode = "yyyy-mm" })
        { Count = 3 },
        new Fonts(
            new Font(new FontSize { Val = 11 }, new FontName { Val = "Calibri" }),
            new Font(new Bold(), new FontSize { Val = 11 }, new FontName { Val = "Calibri" }))
        { Count = 2 },
        new Fills(
            new Fill(new PatternFill { PatternType = PatternValues.None }),
            new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
            new Fill(new PatternFill(new ForegroundColor { Rgb = "FFE8EEEC" }) { PatternType = PatternValues.Solid }))
        { Count = 3 },
        new Borders(new Border(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder()))
        { Count = 1 },
        new CellStyleFormats(new CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 })
        { Count = 1 },
        new CellFormats(
            new CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 },
            new CellFormat { NumberFormatId = 0, FontId = 1, FillId = 2, BorderId = 0, ApplyFont = true, ApplyFill = true },
            new CellFormat { NumberFormatId = 3, ApplyNumberFormat = true },
            new CellFormat { NumberFormatId = 164, ApplyNumberFormat = true },
            new CellFormat { NumberFormatId = 4, ApplyNumberFormat = true },
            new CellFormat { NumberFormatId = 165, ApplyNumberFormat = true },
            new CellFormat { NumberFormatId = 166, ApplyNumberFormat = true })
        { Count = 7 },
        new CellStyles(new CellStyle { Name = "Normal", FormatId = 0, BuiltinId = 0 })
        { Count = 1 });

    /// <summary><c>A1</c>-style reference: column 0 is <c>A</c>, 26 is <c>AA</c>.</summary>
    private static string Reference(int column, uint row)
    {
        var letters = string.Empty;

        for (var n = column + 1; n > 0; n = (n - 1) / 26)
        {
            letters = (char)('A' + ((n - 1) % 26)) + letters;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{letters}{row}");
    }

    /// <summary>A sheet name: at most 31 characters, none of <c>[ ] : * ? / \</c>.</summary>
    private static string SheetName(string title)
    {
        var name = string.Concat(title.Where(character => "[]:*?/\\".IndexOf(character) < 0)).Trim();

        if (name.Length == 0)
        {
            return "Report";
        }

        return name.Length <= 31 ? name : name[..31];
    }

    private static string Subject(ReportFileHeading heading) => string.Create(
        CultureInfo.InvariantCulture,
        $"{heading.Branches} {heading.From:yyyy-MM-dd} ถึง {heading.To:yyyy-MM-dd}");

    /// <summary>Drops characters XML 1.0 cannot hold (control characters, lone surrogates) and caps the length.</summary>
    private static string XmlSafe(string text)
    {
        var safe = new StringBuilder(Math.Min(text.Length, MaxCellText));

        foreach (var rune in text.EnumerateRunes())
        {
            var value = rune.Value;
            var allowed = value is 0x9 or 0xA or 0xD
                || value is >= 0x20 and <= 0xD7FF
                || value is >= 0xE000 and <= 0xFFFD
                || value >= 0x10000;

            if (!allowed)
            {
                continue;
            }

            if (safe.Length + rune.Utf16SequenceLength > MaxCellText)
            {
                break;
            }

            safe.Append(rune.ToString());
        }

        return safe.ToString();
    }
}
