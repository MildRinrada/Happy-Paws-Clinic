namespace HappyPaws.Application.Assistant;

/// <summary>
/// The language model behind the website assistant — a contract extension, see
/// <c>docs/contract-extensions.md § 16</c>.
/// <para>
/// <b>The model is a part, not the product.</b> What the clinic's assistant knows, which tools it may
/// call and what it must never say live in this module; the model only turns a conversation into
/// the next turn. So the port is the smallest thing every chat model offers — messages and tool
/// declarations in, text or tool calls out — and the adapters behind it (Gemini, or any server that
/// speaks the OpenAI chat format: Ollama, vLLM, llama.cpp, LM Studio) can be swapped by
/// configuration without touching the agent.
/// </para>
/// </summary>
public interface ILanguageModel
{
    /// <summary>False when no provider is configured; the assistant is then switched off.</summary>
    bool IsConfigured { get; }

    Task<ModelTurn> CompleteAsync(ModelRequest request, CancellationToken cancellationToken = default);
}

public enum ModelRole
{
    User,
    Assistant,
    Tool,
}

/// <summary>
/// One message in the conversation the model sees. An assistant message carries text or tool
/// calls; a tool message carries the result of one call, matched by <see cref="ToolCallId"/>.
/// </summary>
public sealed record ModelMessage(
    ModelRole Role,
    string? Text = null,
    IReadOnlyList<ModelToolCall>? ToolCalls = null,
    string? ToolCallId = null,
    string? ToolName = null)
{
    public static ModelMessage User(string text) => new(ModelRole.User, text);

    public static ModelMessage Assistant(string text) => new(ModelRole.Assistant, text);

    public static ModelMessage Calls(IReadOnlyList<ModelToolCall> calls) => new(ModelRole.Assistant, ToolCalls: calls);

    public static ModelMessage Result(ModelToolCall call, string json) => new(ModelRole.Tool, json, ToolCallId: call.Id, ToolName: call.Name);
}

/// <summary>A function the model may call. <see cref="ParametersSchema"/> is a JSON Schema object, as JSON text.</summary>
public sealed record ModelTool(string Name, string Description, string ParametersSchema);

/// <summary>
/// One call the model asked for. <see cref="ArgumentsJson"/> is a JSON object. <see cref="Signature"/>
/// is opaque provider data that must travel back with the call (Gemini's thought signature); other
/// adapters leave it empty.
/// </summary>
public sealed record ModelToolCall(string Id, string Name, string ArgumentsJson, string? Signature = null);

public sealed record ModelRequest(
    string SystemPrompt,
    IReadOnlyList<ModelMessage> Messages,
    IReadOnlyList<ModelTool> Tools);

/// <summary>What the model answered: text, or tool calls to run before it answers again.</summary>
public sealed record ModelTurn(string? Text, IReadOnlyList<ModelToolCall> ToolCalls, int InputTokens, int OutputTokens);

/// <summary>The model could not be reached or answered with something unusable. Never shown verbatim.</summary>
public sealed class LanguageModelException(string message, Exception? inner = null) : Exception(message, inner);
