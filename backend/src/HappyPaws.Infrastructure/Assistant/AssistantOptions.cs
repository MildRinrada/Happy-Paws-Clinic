namespace HappyPaws.Infrastructure.Assistant;

/// <summary>
/// <c>Assistant:*</c> — which model the website assistant talks to.
/// <para>
/// <c>Provider</c> picks the adapter: <c>gemini</c> for Google's API, <c>openai</c> for any server
/// that speaks the OpenAI chat-completions format — which is how the open-source runtimes are
/// served (Ollama at <c>http://host:11434/v1</c>, vLLM, llama.cpp, LM Studio). Empty switches the
/// assistant off and the website hides the chat button. The key is a secret: set it from the
/// environment (<c>Assistant__ApiKey</c>), never in a committed file.
/// </para>
/// </summary>
public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    public string Provider { get; set; } = string.Empty;

    /// <summary>e.g. <c>gemini-2.5-flash</c>, or an Ollama tag such as <c>qwen2.5:7b</c>.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Required for Gemini; optional for a self-hosted server that does not check one.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Where the model is. Defaults to Google's endpoint for Gemini; required for <c>openai</c>,
    /// e.g. <c>http://localhost:11434/v1</c>.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Generous for a few sentences on purpose: a "thinking" model (Gemini 2.5) spends part of this
    /// on its reasoning, and a budget sized for the answer alone comes back empty.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 1024;

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>See <c>AssistantSettings.DailyConversationTurns</c>.</summary>
    public int DailyConversationTurns { get; set; } = 1000;

    /// <summary>See <c>AssistantSettings.MaxToolRounds</c>.</summary>
    public int MaxToolRounds { get; set; } = 4;

    /// <summary>Per visitor (by IP, or by account when signed in), per minute.</summary>
    public int MessagesPerMinute { get; set; } = 10;
}
