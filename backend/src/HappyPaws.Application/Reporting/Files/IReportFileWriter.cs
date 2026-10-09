using HappyPaws.Application.Reporting.Tables;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.Application.Reporting.Files;

/// <summary>
/// What a file says about itself besides its rows: what it is, whose it is, and what it covers.
/// A printed report shows all of it on every page; a spreadsheet keeps it in the file's properties
/// so its first row stays the header a pivot table expects.
/// </summary>
/// <param name="Title">The report's Thai name, such as "รายรับ".</param>
/// <param name="ClinicName">The clinic, from its profile.</param>
/// <param name="Branches">The branches covered, by name, or "ทุกสาขา".</param>
/// <param name="From">First clinic day covered.</param>
/// <param name="To">Last clinic day covered.</param>
/// <param name="GeneratedAt">When the file was built, in clinic time.</param>
public sealed record ReportFileHeading(
    string Title,
    string ClinicName,
    string Branches,
    DateOnly From,
    DateOnly To,
    DateTimeOffset GeneratedAt);

/// <summary>
/// Writes a <see cref="ReportTable"/> as one file format. One writer per <see cref="ReportFormat"/>;
/// the export worker picks by the format the job stored.
/// <para>
/// <b>No writer may produce a formula.</b> Names in a report were typed by people, and a cell that
/// a spreadsheet runs is the attack ("formula injection"). CSV has no cell types, so it defuses
/// text; XLSX writes every cell as a typed value; PDF has nothing to run.
/// </para>
/// </summary>
public interface IReportFileWriter
{
    ReportFormat Format { get; }

    /// <summary>The media type the download is served with.</summary>
    string ContentType { get; }

    /// <summary>The file name's extension, without the dot.</summary>
    string Extension { get; }

    byte[] Write(ReportTable table, ReportFileHeading heading);
}
