using System.Text.Json;

namespace HappyPaws.Application.StaffAi.SoapDraft;

/// <summary>One drafted line of the examination form.</summary>
public sealed record DraftFindingDto(string BodySystem, bool IsNormal, string? Note);

/// <summary>
/// The consultation form's own fields, drafted. The names are those of <c>ConsultationDto</c> and
/// of <c>PATCH /staff/consultations/{id}</c> and <c>POST .../examination</c>, so the page can put
/// each one into the field of the same name.
/// </summary>
public sealed record SoapDraftFieldsDto(
    string ChiefComplaint,
    string Subjective,
    string Objective,
    string Assessment,
    string Plan,
    IReadOnlyList<DraftFindingDto> Examination);

/// <summary>
/// Reads the model's JSON. Models wrap JSON in code fences or a sentence of their own, so the
/// object is taken from the first <c>{</c> to the last <c>}</c>. Anything not usable is null, and
/// the handler answers <c>503 ai_unavailable</c>.
/// <para>
/// The draft is held to the form: each text field is cut to the length the form accepts, a body
/// system not on the clinic's form is dropped, and a system named twice keeps its first line.
/// </para>
/// </summary>
public static class SoapDraftParser
{
    public const int ChiefComplaintMax = 2000;
    public const int NoteFieldMax = 8000;
    public const int FindingNoteMax = 500;

    public static SoapDraftFieldsDto? Parse(string answer, IReadOnlyList<string> bodySystems)
    {
        var start = answer.IndexOf('{');
        var end = answer.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(answer[start..(end + 1)]);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var findings = new List<DraftFindingDto>();

            if (root.TryGetProperty("examination", out var examination) && examination.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in examination.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object))
                {
                    var named = Text(item, "body_system", 200);
                    var system = bodySystems.FirstOrDefault(known => string.Equals(known, named, StringComparison.OrdinalIgnoreCase));

                    if (system is null || findings.Any(finding => finding.BodySystem == system))
                    {
                        continue;
                    }

                    var normal = item.TryGetProperty("is_normal", out var flag) && flag.ValueKind == JsonValueKind.True;
                    var note = Text(item, "note", FindingNoteMax);

                    findings.Add(new DraftFindingDto(system, normal, note.Length == 0 ? null : note));
                }
            }

            return new SoapDraftFieldsDto(
                Text(root, "chief_complaint", ChiefComplaintMax),
                Text(root, "subjective", NoteFieldMax),
                Text(root, "objective", NoteFieldMax),
                Text(root, "assessment", NoteFieldMax),
                Text(root, "plan", NoteFieldMax),
                findings);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Text(JsonElement element, string name, int max)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return string.Empty;
        }

        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
            JsonValueKind.Array => string.Join("\n", value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString())),
            _ => string.Empty,
        };

        text = text.Trim();

        return text.Length <= max ? text : text[..max];
    }
}
