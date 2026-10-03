using System.Text.Json;

namespace HappyPaws.Application.Assistant;

/// <summary>
/// Something the assistant can look up, declared to the model as a function.
/// <para>
/// <b>Every tool is a public read.</b> They call the same use cases the public website does, so the
/// assistant can tell a visitor nothing the clinic's own pages would not. That is the line that keeps
/// a chat window on the internet from becoming a way into the clinic's records.
/// </para>
/// </summary>
public interface IAssistantTool
{
    string Name { get; }

    /// <summary>What the tool does and when to use it, written for the model.</summary>
    string Description { get; }

    /// <summary>JSON Schema of the arguments, as JSON text.</summary>
    string ParametersSchema { get; }

    Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// What a tool found, as JSON for the model, and the buttons it suggests the visitor be shown —
/// "จองนัด" after the assistant has looked at free times. Buttons come from tools, not from the
/// model's text, so the widget never renders a link a model made up.
/// </summary>
public sealed record ToolResult(object Data, IReadOnlyList<AssistantAction>? Actions = null);

/// <summary>A button under the assistant's reply. <see cref="Href"/> is always a path on this site.</summary>
public sealed record AssistantAction(string Label, string Href);
