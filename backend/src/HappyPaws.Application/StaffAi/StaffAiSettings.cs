namespace HappyPaws.Application.StaffAi;

/// <summary>
/// How much the staff AI assistant may do, from <c>StaffAi:*</c> in configuration. Set by whoever
/// runs the deployment; nothing the clinic can change from its own screens.
/// </summary>
/// <param name="MonthlyCalls">
/// Uses allowed per calendar month (clinic time) across the whole deployment. One deployment is one
/// clinic, so this is the clinic's allowance. Zero or less switches the assistant off.
/// </param>
/// <param name="MaxToolRounds">How many times one answer of <c>/staff/ai/ask</c> may go back to the tools.</param>
/// <param name="DailyBriefHour">The clinic hour from which the morning brief is written, 0 to 23.</param>
public sealed record StaffAiSettings(int MonthlyCalls = 1500, int MaxToolRounds = 4, int DailyBriefHour = 7);

/// <summary>The features a use is counted under. The keys of <c>by_feature</c> in <c>GET /staff/ai/usage</c>.</summary>
public static class StaffAiFeatures
{
    public const string PetSummary = "pet_summary";
    public const string SoapDraft = "soap_draft";
    public const string OwnerMessage = "owner_message";
    public const string Ask = "ask";
    public const string DailyBrief = "daily_brief";

    public static IReadOnlyList<string> All { get; } = [PetSummary, SoapDraft, OwnerMessage, Ask, DailyBrief];
}
