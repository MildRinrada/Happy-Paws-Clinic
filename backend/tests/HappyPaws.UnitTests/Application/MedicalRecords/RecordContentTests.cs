using System.Text.Json;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.MedicalRecords;

namespace HappyPaws.UnitTests.Application.MedicalRecords;

/// <summary>
/// What an amendment may change, and the diff it produces — "diff รายฟิลด์".
/// </summary>
public class RecordContentTests
{
    private const string Current =
        """{"chief_complaint":"ซึม","assessment":"สงสัยติดเชื้อ","plan":"ให้ยา 7 วัน","treatments":[]}""";

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void A_change_produces_the_new_document_and_a_from_to_diff()
    {
        var (content, changes) = RecordContent.Apply(
            Current, new Dictionary<string, string?> { ["assessment"] = "ยืนยันติดเชื้อ" });

        Assert.Equal("ยืนยันติดเชื้อ", Parse(content).GetProperty("assessment").GetString());
        // Untouched fields survive verbatim — an amendment is a correction, not a rewrite.
        Assert.Equal("ให้ยา 7 วัน", Parse(content).GetProperty("plan").GetString());

        var diff = Parse(changes).GetProperty("assessment");
        Assert.Equal("สงสัยติดเชื้อ", diff.GetProperty("from").GetString());
        Assert.Equal("ยืนยันติดเชื้อ", diff.GetProperty("to").GetString());
    }

    [Fact]
    public void A_structured_field_cannot_be_amended()
    {
        // Treatments have been billed and prescriptions may have been dispensed. Editing them after
        // signing would rewrite something another part of the clinic already acted on.
        var failure = Assert.Throws<ValidationFailedException>(() =>
            RecordContent.Apply(Current, new Dictionary<string, string?> { ["treatments"] = "[]" }));

        Assert.Contains("treatments", failure.Errors["changes"][0], StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_field_cannot_be_amended()
    {
        Assert.Throws<ValidationFailedException>(() =>
            RecordContent.Apply(Current, new Dictionary<string, string?> { ["notes"] = "x" }));
    }

    [Fact]
    public void An_amendment_that_changes_nothing_is_refused()
    {
        // Copy-paste and autosave both produce no-op "changes". Recording them would fill the
        // history with entries that changed nothing.
        Assert.Throws<ValidationFailedException>(() =>
            RecordContent.Apply(Current, new Dictionary<string, string?> { ["plan"] = "ให้ยา 7 วัน" }));
    }

    [Fact]
    public void An_empty_change_set_is_refused()
    {
        Assert.Throws<ValidationFailedException>(() =>
            RecordContent.Apply(Current, new Dictionary<string, string?>()));
    }

    [Fact]
    public void A_field_can_be_cleared_and_the_diff_records_it()
    {
        var (content, changes) = RecordContent.Apply(
            Current, new Dictionary<string, string?> { ["chief_complaint"] = null });

        Assert.Equal(JsonValueKind.Null, Parse(content).GetProperty("chief_complaint").ValueKind);
        Assert.Equal("ซึม", Parse(changes).GetProperty("chief_complaint").GetProperty("from").GetString());
    }

    [Fact]
    public void Thai_text_survives_a_round_trip_unescaped()
    {
        var (content, _) = RecordContent.Apply(
            Current, new Dictionary<string, string?> { ["plan"] = "นัดติดตาม 7 วัน" });

        // Escaped, a signed record is unreadable to the person entitled to read it.
        Assert.Contains("นัดติดตาม", content, StringComparison.Ordinal);
    }
}
