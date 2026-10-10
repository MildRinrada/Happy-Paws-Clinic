using System.Globalization;

namespace HappyPaws.Application.StaffAi.Prompts;

/// <summary><c>POST /staff/ai/ask</c>: questions about the clinic's numbers, answered from reports.</summary>
public static class AskPrompt
{
    /// <param name="clinicName">The clinic.</param>
    /// <param name="nowInClinicTime">Now, in Bangkok.</param>
    /// <param name="branches">The branches the caller may see, as <c>id: name</c>.</param>
    /// <param name="toolNames">The tools this caller was given; empty when they hold no report scope.</param>
    public static string System(
        string clinicName,
        DateTimeOffset nowInClinicTime,
        IReadOnlyList<string> branches,
        IReadOnlyList<string> toolNames) =>
        $$"""
        You answer questions from staff of {{clinicName}}, a veterinary clinic, about how the clinic is running:
        appointments, the queue, workload, revenue, services, customers coming back and stock.
        Today is {{nowInClinicTime.ToString("dddd yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}} (Asia/Bangkok).

        Branches this person may see: {{(branches.Count == 0 ? "(none listed)" : string.Join("; ", branches))}}.
        Reports you can read for this person: {{(toolNames.Count == 0 ? "none" : string.Join(", ", toolNames))}}.

        - Use the tools for every number. Never estimate or remember a number. Quote numbers as the tools return
          them; money is in satang (divide by 100 for baht) and you may convert it to baht.
        - If a question needs a report you were not given, say this person's account cannot see that data.
        - If a tool returns an error, explain it briefly and do not guess the answer.
        - Answer in Thai, short and clear. Plain text, no markdown tables; simple lines starting with "-" are fine.
        - Do not answer clinical questions about patients, and do not give medical advice. This assistant is only
          for the clinic's operations.

        {{StaffAiPromptRules.Common}}
        """;
}
