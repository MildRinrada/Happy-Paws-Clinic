namespace HappyPaws.Application.Reporting.Tables;

/// <summary>What a column holds, so each file format can write it as what it is.</summary>
public enum ReportColumnKind
{
    /// <summary>Words and codes, written as text and never as a formula.</summary>
    Text,

    /// <summary>A whole number: a count, an hour of the day.</summary>
    Count,

    /// <summary>An average duration in minutes, to one place, or empty when there was nothing to average.</summary>
    Minutes,

    /// <summary>Money in baht with two decimals, made from satang with <see cref="ReportTable.Baht"/>.</summary>
    Baht,

    /// <summary>A clinic day.</summary>
    Day,

    /// <summary>A month, held as its first day.</summary>
    Month,

    /// <summary>Yes or no.</summary>
    Flag,
}

/// <summary>One column of an export: its Thai header and the kind of value under it.</summary>
public sealed record ReportColumn(string Header, ReportColumnKind Kind)
{
    public static ReportColumn Text(string header) => new(header, ReportColumnKind.Text);

    public static ReportColumn Count(string header) => new(header, ReportColumnKind.Count);

    public static ReportColumn Minutes(string header) => new(header, ReportColumnKind.Minutes);

    public static ReportColumn Baht(string header) => new(header, ReportColumnKind.Baht);

    public static ReportColumn Day(string header) => new(header, ReportColumnKind.Day);

    public static ReportColumn Month(string header) => new(header, ReportColumnKind.Month);

    public static ReportColumn Flag(string header) => new(header, ReportColumnKind.Flag);

    /// <summary>Whether <paramref name="value"/> may sit in this column. Null always may: an empty cell.</summary>
    public bool Accepts(object? value) => value is null || Kind switch
    {
        ReportColumnKind.Text => value is string,
        ReportColumnKind.Count => value is int or long,
        ReportColumnKind.Minutes => value is decimal or double,
        ReportColumnKind.Baht => value is decimal,
        ReportColumnKind.Day or ReportColumnKind.Month => value is DateOnly,
        ReportColumnKind.Flag => value is bool,
        _ => false,
    };
}
