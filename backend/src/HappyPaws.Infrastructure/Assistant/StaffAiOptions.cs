namespace HappyPaws.Infrastructure.Assistant;

/// <summary>
/// <c>StaffAi:*</c>: the staff AI assistant (<c>docs/contract-extensions.md § 34</c>). Set by whoever
/// runs the deployment, from the environment (<c>StaffAi__ApiKey</c>); the clinic has no screen for it.
/// <code>
/// "StaffAi": {
///   "Provider": "",            // gemini | openai | off; empty uses the Assistant section's provider
///   "Model": "", "ApiKey": "", "BaseUrl": "",
///   "MonthlyCalls": 1500, "TimeoutSeconds": 60, "MaxOutputTokens": 2048,
///   "MaxToolRounds": 4, "DailyBriefHour": 7
/// }
/// </code>
/// <para>
/// <b>One key for both.</b> When <see cref="Provider"/> is empty, the four provider settings
/// (provider, model, key, base URL) are all taken from <c>Assistant:*</c>, so a deployment with the
/// website assistant set up has the staff assistant too. When it is set, all four come from here;
/// the two blocks are never mixed, so a Gemini key is never sent to an Ollama URL. <c>off</c>
/// switches the staff assistant off even when the website assistant is on. With no provider in
/// either block the staff assistant is off and its routes answer <c>503 ai_unavailable</c>.
/// </para>
/// </summary>
public sealed class StaffAiOptions
{
    public const string SectionName = "StaffAi";

    /// <summary>The value that switches the staff assistant off whatever <c>Assistant:*</c> says.</summary>
    public const string Off = "off";

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Uses per clinic month for the whole deployment. Zero or less switches the assistant off.</summary>
    public int MonthlyCalls { get; set; } = 1500;

    /// <summary>A summary of a long file takes longer than a chat reply.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Room for a thinking model's reasoning plus a paragraph or a JSON draft.</summary>
    public int MaxOutputTokens { get; set; } = 2048;

    public int MaxToolRounds { get; set; } = 4;

    /// <summary>The clinic hour from which the morning brief is written.</summary>
    public int DailyBriefHour { get; set; } = 7;

    /// <summary>Lower than the website's: drafts should follow the notes, not vary.</summary>
    public double Temperature { get; set; } = 0.2;
}
