namespace HappyPaws.Application.MedicalRecords.GenerateDocument;

/// <summary>
/// A document to print, as content: what it says, in what order. The renderer decides how it looks.
/// <para>
/// Use cases build this; the PDF adapter draws it. The wording of a health certificate is the
/// clinic's business and lives with the use case; the fonts and margins are the adapter's.
/// </para>
/// </summary>
public sealed record PrintableDocument(
    string Title,
    string ClinicName,
    IReadOnlyList<string> ClinicLines,
    IReadOnlyList<PrintableSection> Sections,
    PrintableSignature? Signature,
    string Reference,
    string IssuedOn);

/// <summary>A heading and what follows it: label-value pairs, a table, or a paragraph.</summary>
public sealed record PrintableSection(
    string Heading,
    IReadOnlyList<PrintableField>? Fields = null,
    PrintableTable? Table = null,
    string? Paragraph = null);

public sealed record PrintableField(string Label, string Value);

public sealed record PrintableTable(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>Who signs, printed under a line for the signature.</summary>
public sealed record PrintableSignature(string Name, string? LicenseNo, string Role);

/// <summary>Draws a <see cref="PrintableDocument"/> as a PDF.</summary>
public interface IDocumentRenderer
{
    byte[] RenderPdf(PrintableDocument document);
}
