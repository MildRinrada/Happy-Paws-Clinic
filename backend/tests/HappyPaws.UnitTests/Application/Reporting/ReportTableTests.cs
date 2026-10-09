using HappyPaws.Application.Reporting.Tables;

namespace HappyPaws.UnitTests.Application.Reporting;

/// <summary>The table every format writes: a value per column, of the kind the column says.</summary>
public class ReportTableTests
{
    [Fact]
    public void A_row_must_fill_every_column()
    {
        Assert.Throws<ArgumentException>(() => new ReportTable([ReportColumn.Text("a"), ReportColumn.Text("b")]).Add("only one"));
    }

    [Fact]
    public void A_value_of_the_wrong_kind_is_refused_so_no_format_has_to_guess()
    {
        Assert.Throws<ArgumentException>(() => new ReportTable([ReportColumn.Baht("บาท")]).Add(125_050L));
        Assert.Throws<ArgumentException>(() => new ReportTable([ReportColumn.Day("วันที่")]).Add("2026-10-07"));
        Assert.Throws<ArgumentException>(() => new ReportTable([ReportColumn.Text("ชื่อ")]).Add(3));
    }

    [Fact]
    public void Every_kind_takes_its_own_values_and_null()
    {
        var table = new ReportTable(
        [
            ReportColumn.Text("t"), ReportColumn.Count("c"), ReportColumn.Minutes("m"), ReportColumn.Baht("b"),
            ReportColumn.Day("d"), ReportColumn.Month("mo"), ReportColumn.Flag("f"),
        ]);

        table.Add("x", 3, 1.5m, ReportTable.Baht(100), new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 1), true);
        table.Add(null, 4L, 2.5d, null, null, null, null);

        Assert.Equal(2, table.RowCount);
    }

    [Fact]
    public void Satang_become_baht_with_two_places()
    {
        Assert.Equal(1250.50m, ReportTable.Baht(125_050));
        Assert.Equal(-0.01m, ReportTable.Baht(-1));
    }
}
