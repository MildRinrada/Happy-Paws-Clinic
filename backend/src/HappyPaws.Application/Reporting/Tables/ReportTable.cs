namespace HappyPaws.Application.Reporting.Tables;

/// <summary>
/// The rows of one export, before they become a file: typed columns and the values under them.
/// <para>
/// Every format writes the same table. Each column says what it holds (<see cref="ReportColumnKind"/>),
/// so a spreadsheet gets numbers as numbers and days as dates, and a printed page can line money
/// up on the right, without any format having to guess from the header.
/// </para>
/// </summary>
public sealed class ReportTable(IReadOnlyList<ReportColumn> columns)
{
    private readonly List<IReadOnlyList<object?>> _rows = [];

    public IReadOnlyList<ReportColumn> Columns { get; } = columns;

    public IReadOnlyList<IReadOnlyList<object?>> Rows => _rows;

    public int RowCount => _rows.Count;

    /// <summary>One row, a value per column, each of the kind its column holds or null.</summary>
    public ReportTable Add(params object?[] cells)
    {
        if (cells.Length != Columns.Count)
        {
            throw new ArgumentException($"row has {cells.Length} cells, the table {Columns.Count} columns", nameof(cells));
        }

        for (var i = 0; i < cells.Length; i++)
        {
            if (!Columns[i].Accepts(cells[i]))
            {
                throw new ArgumentException(
                    $"column {i} ({Columns[i].Kind}) cannot hold a {cells[i]!.GetType().Name}", nameof(cells));
            }
        }

        _rows.Add(cells);
        return this;
    }

    /// <summary>Satang as baht with two decimals: 125050 → 1250.50.</summary>
    public static decimal Baht(long satang) => satang / 100m;
}
