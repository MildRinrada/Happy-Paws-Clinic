using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using HappyPaws.Application.Assistant;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.StaffAi.Ask;

/// <summary>
/// A report the staff assistant can read for the person asking, declared to the model as a function.
/// <para>
/// <b>Every tool is an existing read use case, run as the caller.</b> It is offered to the model
/// only when the caller holds <see cref="RequiredScope"/>, the same scope the report's own endpoint
/// requires, and the use case behind it applies the caller's branches exactly as it does for that
/// endpoint. So the assistant can never show anyone more than the screens would.
/// </para>
/// </summary>
public interface IStaffAiTool
{
    string Name { get; }

    /// <summary>What the tool does and when to use it, written for the model.</summary>
    string Description { get; }

    /// <summary>JSON Schema of the arguments, as JSON text.</summary>
    string ParametersSchema { get; }

    /// <summary>The scope of the endpoint whose use case this wraps.</summary>
    string RequiredScope { get; }

    /// <summary>The result as data for the model, and a button to the screen that shows it.</summary>
    Task<ToolResult> InvokeAsync(JsonElement arguments, CancellationToken cancellationToken = default);
}

/// <summary>Reading a tool's arguments, which come from a model and may be any shape.</summary>
public static class StaffToolArguments
{
    public static string? String(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object
        && arguments.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;

    /// <summary>A <c>YYYY-MM-DD</c> argument. Absent is null; present but malformed is a refusal the model can read.</summary>
    public static DateOnly? Date(JsonElement arguments, string name)
    {
        var text = String(arguments, name);

        if (text is null)
        {
            return null;
        }

        return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? day
            : throw new ValidationFailedException(new Dictionary<string, string[]> { [name] = [$"{name} ต้องเป็นวันที่รูปแบบ YYYY-MM-DD"] });
    }

    public static int? Int(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object
        && arguments.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var number)
            ? number
            : null;
}

/// <summary>Tool results go to the model in the API's own wire format, snake_case, Thai unescaped.</summary>
public static class StaffToolJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
}
