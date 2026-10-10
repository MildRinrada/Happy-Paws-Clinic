namespace HappyPaws.Application.StaffAi.Prompts;

/// <summary>
/// <c>POST /staff/ai/consultations/{id}/soap-draft</c>: a vet's short notes rearranged into the
/// consultation form's fields, adding nothing.
/// </summary>
public static class SoapDraftPrompt
{
    /// <summary>
    /// The body systems of the consulting room's examination form when the clinic has not set its own
    /// (<c>PATCH /clinic/settings/medical-record</c>). The same names the page draws, so a drafted
    /// finding lands on an existing row.
    /// </summary>
    public static IReadOnlyList<string> DefaultBodySystems { get; } =
    [
        "ผิวหนังและขน",
        "ตา",
        "หู",
        "ช่องปากและฟัน",
        "หัวใจและหลอดเลือด",
        "ระบบหายใจ",
        "ระบบทางเดินอาหาร",
        "ระบบขับถ่ายปัสสาวะ",
        "กล้ามเนื้อและกระดูก",
        "ระบบประสาท",
        "ต่อมน้ำเหลือง",
    ];

    public static string System(IReadOnlyList<string> bodySystems) =>
        $$"""
        You rewrite a veterinarian's short notes into a draft of the clinic's consultation form. The vet will
        check the draft and save it themselves.

        Answer with ONE JSON object and nothing else, with exactly these keys:
        {
          "chief_complaint": string,
          "subjective": string,
          "objective": string,
          "assessment": string,
          "plan": string,
          "examination": [ { "body_system": string, "is_normal": boolean, "note": string } ]
        }
        - Put each piece of the notes in the field it belongs to. Use an empty string when the notes say
          nothing for a field. Do not repeat the same sentence in several fields.
        - "examination" lists only the body systems the notes mention. "body_system" must be exactly one of:
          {{string.Join(", ", bodySystems.Select(system => $"\"{system}\""))}}.
          "is_normal" is true only when the notes say that system is normal. "note" is what the notes say about it.
        - "assessment" holds only the assessment the vet wrote. "plan" holds only the plan the vet wrote.
        - NEVER add a diagnosis, a disease, a drug, a dose, a test, a measurement, a number or a finding that is
          not in the notes. Never complete a sentence the vet left unfinished with your own medical content.
        - Keep the vet's own medical terms, drug names, doses and numbers exactly as written.
        - Write the fields in Thai, except terms the vet wrote in English.

        {{StaffAiPromptRules.Common}}
        """;
}
