using System.Text.Json;
using System.Text.Json.Nodes;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Consultations.Dtos;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.MedicalRecords;

/// <summary>
/// Builds the document a medical record holds, and applies amendments to it.
/// <para>
/// The snapshot is taken here rather than in the domain so the entity stays free of serialisation
/// concerns and stores what it is given as opaque text — which is also what lets the stored shape
/// evolve without a migration of signed documents.
/// </para>
/// </summary>
public static class RecordContent
{
    /// <summary>
    /// The fields an amendment may touch.
    /// <para>
    /// Narrative only, and deliberately so. A treatment line has been billed, a prescription may
    /// have been dispensed, and vitals were a measurement taken at a moment — editing any of them
    /// after signing would rewrite something another part of the clinic already acted on. Those are
    /// corrected by a credit note or by a new consultation, not by amending history. An amendment
    /// is for what the vet <i>wrote</i>, not for what the clinic <i>did</i>.
    /// </para>
    /// </summary>
    public static readonly IReadOnlySet<string> AmendableFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "chief_complaint",
        "subjective",
        "objective",
        "assessment",
        "plan",
    };

    /// <summary>Serialises a closed case into the document that will be signed.</summary>
    public static string Build(Consultation consultation) =>
        JsonSerializer.Serialize(ConsultationDto.From(consultation), Options);

    /// <summary>Reads a signed document back into its parts, for projections such as the owner's view.</summary>
    public static ConsultationDto? Read(string contentJson) =>
        JsonSerializer.Deserialize<ConsultationDto>(contentJson, Options);

    /// <summary>
    /// Applies field changes to the current document and returns the new document alongside a
    /// field-level diff — "diff รายฟิลด์", which is what the record's audit view shows.
    /// </summary>
    public static (string Content, string Changes) Apply(
        string currentJson,
        IReadOnlyDictionary<string, string?> changes)
    {
        if (changes.Count == 0)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["changes"] = ["ต้องระบุฟิลด์ที่แก้ไขอย่างน้อยหนึ่งรายการ"] });
        }

        var unknown = changes.Keys.Where(key => !AmendableFields.Contains(key)).ToArray();
        if (unknown.Length > 0)
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["changes"] = [$"แก้ไขฟิลด์เหล่านี้ไม่ได้: {string.Join(", ", unknown)}"],
            });
        }

        var document = JsonNode.Parse(currentJson)?.AsObject()
            ?? throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["record"] = ["เนื้อหาเวชระเบียนเสียหาย"] });

        var diff = new JsonObject();

        foreach (var (field, value) in changes)
        {
            var before = document[field]?.GetValue<string?>();

            if (before == value)
            {
                // Autosave and copy-paste both produce no-op "changes". Recording them would fill
                // the amendment history with entries that changed nothing.
                continue;
            }

            diff[field] = new JsonObject
            {
                ["from"] = before,
                ["to"] = value,
            };

            document[field] = value;
        }

        if (diff.Count == 0)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["changes"] = ["ค่าที่ส่งมาไม่ต่างจากฉบับปัจจุบัน"] });
        }

        return (document.ToJsonString(Options), diff.ToJsonString(Options));
    }

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        // Thai clinical notes are the normal case here, not an edge case. Escaped, a signed record
        // is unreadable to the person entitled to read it.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
