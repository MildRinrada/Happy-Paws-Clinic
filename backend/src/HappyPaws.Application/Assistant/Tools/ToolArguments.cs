using System.Text.Json;

namespace HappyPaws.Application.Assistant.Tools;

/// <summary>Reading a tool's arguments, which come from a model and may be any shape.</summary>
internal static class ToolArguments
{
    public static string? String(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object
        && arguments.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;
}
