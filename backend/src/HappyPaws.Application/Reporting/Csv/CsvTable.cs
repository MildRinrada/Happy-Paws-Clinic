using System.Globalization;
using System.Text;

namespace HappyPaws.Application.Reporting.Csv;

/// <summary>
/// A table on its way to a CSV file: a header row and data rows of text and numbers.
/// <para>
/// Written as RFC 4180 with CRLF line ends, in UTF-8 with a byte-order mark. The mark is what makes
/// Excel read Thai as Thai instead of guessing a legacy code page; Numbers and Google Sheets ignore
/// it.
/// </para>
/// <para>
/// <b>Text cells are defused.</b> A customer or staff name is typed by a person, and a cell that
/// starts with <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, a tab or a carriage return is run as a
/// formula by a spreadsheet ("CSV injection"). Such text is written with a leading apostrophe.
/// Numbers are never touched, so a negative amount stays a number.
/// </para>
/// </summary>
public sealed class CsvTable(IReadOnlyList<string> headers)
{
    private static readonly char[] FormulaStarts = ['=', '+', '-', '@', '\t', '\r'];
    private static readonly char[] NeedsQuotes = [',', '"', '\r', '\n'];

    private readonly List<IReadOnlyList<object?>> _rows = [];

    public IReadOnlyList<string> Headers { get; } = headers;

    public int RowCount => _rows.Count;

    /// <summary>One row: strings, whole numbers, decimals, dates, booleans or null.</summary>
    public CsvTable Add(params object?[] cells)
    {
        if (cells.Length != Headers.Count)
        {
            throw new ArgumentException($"row has {cells.Length} cells, the table {Headers.Count} columns", nameof(cells));
        }

        _rows.Add(cells);
        return this;
    }

    public byte[] ToBytes()
    {
        var text = new StringBuilder();

        WriteLine(text, Headers.Cast<object?>().ToList());

        foreach (var row in _rows)
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

    /// <summary>Satang as baht with two decimals: 125050 → 1250.50.</summary>
    public static decimal Baht(long satang) => satang / 100m;

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
