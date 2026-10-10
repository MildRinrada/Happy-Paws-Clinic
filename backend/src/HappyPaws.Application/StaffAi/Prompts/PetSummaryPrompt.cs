namespace HappyPaws.Application.StaffAi.Prompts;

/// <summary>
/// <c>POST /staff/ai/pets/{id}/summary</c>: one paragraph a vet reads before walking into the room.
/// </summary>
public static class PetSummaryPrompt
{
    /// <summary>
    /// Part of every cache fingerprint. Change it whenever the prompt or the data layout changes, so
    /// summaries written under the old wording are written again rather than served.
    /// </summary>
    public const string Version = "pet-summary-v1";

    public static string System(string clinicName) =>
        $$"""
        You prepare a short briefing for a veterinarian at {{clinicName}} who is about to examine a patient.

        Write ONE paragraph in Thai, at most about 120 words, from the patient's records given as data.
        Cover these topics in this order, skipping none:
        1. allergies, 2. chronic or recurring diagnoses, 3. current medications (only those marked active),
        4. the weight trend, 5. abnormal lab results, 6. the last visits.
        If there are no records for a topic, say so in a few words.

        Cite every fact: right after it, write the reference of the record it came from in square brackets,
        for example [R3], or [R1][R4] for several. Use only references that appear in the data. Do not cite
        anything else and do not invent references.

        Do not add advice, a diagnosis, a cause, a prognosis or a treatment plan. Do not use headings,
        bullet points or markdown: plain sentences only.

        {{StaffAiPromptRules.Common}}
        """;
}
