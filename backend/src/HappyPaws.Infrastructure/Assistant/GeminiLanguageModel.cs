using System.Text;
using System.Text.Json.Nodes;
using HappyPaws.Application.Assistant;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Assistant;

/// <summary>
/// Google's Gemini API (<c>generateContent</c> with function calling).
/// <para>
/// Use the paid tier. On the free tier Google may use what is sent to improve its products, and
/// what is sent here is a member of the public talking about their pet to a named clinic.
/// </para>
/// </summary>
internal sealed class GeminiLanguageModel(HttpClient http, IOptions<AssistantOptions> options) : ILanguageModel
{
    private const string DefaultBaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    private readonly AssistantOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey) && !string.IsNullOrWhiteSpace(_options.Model);

    public async Task<ModelTurn> CompleteAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = request.SystemPrompt }) },
            ["contents"] = Contents(request.Messages),
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = _options.Temperature,
                ["maxOutputTokens"] = _options.MaxOutputTokens,
            },
        };

        if (request.Tools.Count > 0)
        {
            body["tools"] = new JsonArray(new JsonObject
            {
                ["functionDeclarations"] = new JsonArray([.. request.Tools.Select(Declaration)]),
            });
        }

        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl) ? DefaultBaseUrl : _options.BaseUrl.TrimEnd('/');
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/models/{Uri.EscapeDataString(_options.Model)}:generateContent")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };

        // In a header, not the query string, so the key never lands in a proxy's access log.
        message.Headers.Add("x-goog-api-key", _options.ApiKey);

        var json = await ModelHttp.SendAsync(http, message, cancellationToken);
        var parts = json["candidates"]?[0]?["content"]?["parts"] as JsonArray ?? [];

        // Thinking models return their reasoning as parts marked "thought"; only the answer is kept.
        var text = string.Concat(parts
            .Where(part => part?["text"] is not null && part["thought"]?.GetValue<bool>() != true)
            .Select(part => part!["text"]!.GetValue<string>()));

        var calls = parts
            .Where(part => part?["functionCall"] is not null)
            .Select((part, index) => new ModelToolCall(
                part!["functionCall"]!["id"]?.GetValue<string>() ?? $"call_{index}",
                part["functionCall"]!["name"]!.GetValue<string>(),
                part["functionCall"]!["args"]?.ToJsonString() ?? "{}",
                part["thoughtSignature"]?.GetValue<string>()))
            .ToList();

        return new ModelTurn(
            text.Length == 0 ? null : text,
            calls,
            json["usageMetadata"]?["promptTokenCount"]?.GetValue<int>() ?? 0,
            json["usageMetadata"]?["candidatesTokenCount"]?.GetValue<int>() ?? 0);
    }

    private static JsonArray Contents(IReadOnlyList<ModelMessage> messages)
    {
        var contents = new JsonArray();
        JsonArray? pendingResults = null;

        foreach (var message in messages)
        {
            if (message.Role == ModelRole.Tool)
            {
                // Every result of one round goes back in one turn, matching the calls one-for-one.
                if (pendingResults is null)
                {
                    pendingResults = [];
                    contents.Add(new JsonObject { ["role"] = "user", ["parts"] = pendingResults });
                }

                pendingResults.Add(new JsonObject
                {
                    ["functionResponse"] = new JsonObject { ["name"] = message.ToolName, ["response"] = AsObject(message.Text) },
                });
                continue;
            }

            pendingResults = null;

            if (message.ToolCalls is { Count: > 0 } calls)
            {
                contents.Add(new JsonObject
                {
                    ["role"] = "model",
                    ["parts"] = new JsonArray([.. calls.Select(call =>
                    {
                        var part = new JsonObject
                        {
                            ["functionCall"] = new JsonObject { ["name"] = call.Name, ["args"] = AsObject(call.ArgumentsJson) },
                        };
                        if (call.Signature is not null) part["thoughtSignature"] = call.Signature;
                        return (JsonNode)part;
                    })]),
                });
                continue;
            }

            contents.Add(new JsonObject
            {
                ["role"] = message.Role == ModelRole.User ? "user" : "model",
                ["parts"] = new JsonArray(new JsonObject { ["text"] = message.Text }),
            });
        }

        return contents;
    }

    /// <summary>
    /// A declaration in Gemini's schema dialect: upper-case type names, and no <c>parameters</c> at
    /// all for a function that takes none (an object with no properties is refused).
    /// </summary>
    private static JsonNode Declaration(ModelTool tool)
    {
        var declaration = new JsonObject { ["name"] = tool.Name, ["description"] = tool.Description };
        var schema = JsonNode.Parse(tool.ParametersSchema) as JsonObject;

        if (schema?["properties"] is JsonObject { Count: > 0 })
        {
            declaration["parameters"] = UpperCaseTypes(schema);
        }

        return declaration;
    }

    private static JsonNode UpperCaseTypes(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var (key, value) in obj.ToList())
            {
                if (key == "type" && value is JsonValue type && type.TryGetValue<string>(out var name))
                {
                    obj[key] = name.ToUpperInvariant();
                }
                else if (value is not null)
                {
                    UpperCaseTypes(value);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array.Where(item => item is not null)) UpperCaseTypes(item!);
        }

        return node;
    }

    /// <summary>Gemini wants an object here; a tool's JSON is one, and anything else is wrapped.</summary>
    private static JsonObject AsObject(string? json)
    {
        try
        {
            if (JsonNode.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json) is JsonObject parsed)
            {
                return parsed;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Not JSON: passed on as text below.
        }

        return new JsonObject { ["result"] = json };
    }
}
