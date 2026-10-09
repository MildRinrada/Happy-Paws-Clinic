using System.Globalization;
using System.Text;
using HappyPaws.Application.Reporting.Files;
using HappyPaws.Application.Reporting.Tables;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.Application.Reporting.Csv;

/// <summary>
/// Writes a report as CSV.
/// <para>
/// RFC 4180 with CRLF line ends, in UTF-8 with a byte-order mark. The mark is what makes Excel read
/// Thai as Thai instead of guessing a legacy code page; Numbers and Google Sheets ignore it. The
/// heading is not written: the first line is the header row, which is what a spreadsheet's import
/// expects.
/// </para>
/// <para>
/// <b>Text cells are defused.</b> A customer or staff name is typed by a person, and a cell that
/// starts with <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, a tab or a carriage return is run as a
/// formula by a spreadsheet ("CSV injection"). Such text is written with a leading apostrophe.
/// Numbers are never touched, so a negative amount stays a number.
/// </para>
/// </summary>
public sealed class CsvReportWriter : IReportFileWriter
{
    private static readonly char[] FormulaStarts = ['=', '+', '-', '@', '\t', '\r'];
    private static readonly char[] NeedsQuotes = [',', '"', '\r', '\n'];

    public ReportFormat Format => ReportFormat.Csv;

    public string ContentType => "text/csv; charset=utf-8";

    public string Extension => "csv";

    public byte[] Write(ReportTable table, ReportFileHeading heading) => ToBytes(table);

    /// <summary>The CSV of <paramref name="table"/>; the heading plays no part.</summary>
    public static byte[] ToBytes(ReportTable table)
    {
        var text = new StringBuilder();

        WriteLine(text, table.Columns.Select(column => (object?)column.Header).ToList());

        foreach (var row in table.Rows)
        {
            WriteLine(text, row);
        }

        var body = Encoding.UTF8.GetBytes(text.ToString());
        var preamble = Encoding.UTF8.GetPreamble();
        var bytes = new byte[preamble.Length + body.Length];

        preamble.CopyTo(bytes, 0);
        body.CopyTo(bytes, preamble.Length);

        return bytes;
    }

    private static void WriteLine(StringBuilder text, IReadOnlyList<object?> cells)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0)
            {
                text.Append(',');
            }

            text.Append(Cell(cells[i]));
        }

        text.Append("\r\n");
    }

    private static string Cell(object? value) => value switch
    {
        null => string.Empty,
        string text => Quote(Defuse(text)),
        bool flag => flag ? "1" : "0",
        decimal number => number.ToString("0.00##", CultureInfo.InvariantCulture),
        double number => number.ToString("0.##", CultureInfo.InvariantCulture),
        DateOnly day => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeOffset instant => instant.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => Quote(Defuse(value.ToString() ?? string.Empty)),
    };

    private static string Defuse(string text) =>
        text.Length > 0 && FormulaStarts.Contains(text[0]) ? "'" + text : text;

    private static string Quote(string text) =>
        text.IndexOfAny(NeedsQuotes) >= 0 ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : text;
}
