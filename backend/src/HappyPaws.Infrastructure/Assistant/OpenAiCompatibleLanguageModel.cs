using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HappyPaws.Application.Assistant;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Assistant;

/// <summary>
/// Any server that speaks the OpenAI chat-completions format with tool calls. This is the adapter
/// for self-hosted open-source models: Ollama (<c>http://host:11434/v1</c>), vLLM, llama.cpp's
/// server and LM Studio all serve this format, so a clinic moving off a paid API changes the base
/// URL and model name and nothing else.
/// </summary>
internal sealed class OpenAiCompatibleLanguageModel(HttpClient http, IOptions<AssistantOptions> options) : ILanguageModel
{
    private readonly AssistantOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.BaseUrl) && !string.IsNullOrWhiteSpace(_options.Model);

    public async Task<ModelTurn> CompleteAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["model"] = _options.Model,
            ["temperature"] = _options.Temperature,
            ["max_tokens"] = _options.MaxOutputTokens,
            ["messages"] = Messages(request),
        };

        if (request.Tools.Count > 0)
        {
            body["tools"] = new JsonArray([.. request.Tools.Select(tool => (JsonNode)new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = JsonNode.Parse(tool.ParametersSchema),
                },
            })]);
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        var json = await ModelHttp.SendAsync(http, message, cancellationToken);

        try
        {
            var reply = json["choices"]![0]!["message"]!;
            var calls = (reply["tool_calls"] as JsonArray ?? [])
                .Select((call, index) => new ModelToolCall(
                    call!["id"]?.GetValue<string>() ?? $"call_{index}",
                    call["function"]!["name"]!.GetValue<string>(),
                    Arguments(call["function"]!["arguments"])))
                .ToList();

            return new ModelTurn(
                reply["content"]?.GetValue<string>(),
                calls,
                json["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 0,
                json["usage"]?["completion_tokens"]?.GetValue<int>() ?? 0);
        }
        catch (Exception unexpected) when (unexpected is NullReferenceException or InvalidOperationException or FormatException or ArgumentOutOfRangeException)
        {
            throw new LanguageModelException("The model's answer was not in the chat-completions shape.", unexpected);
        }
    }

    private static JsonArray Messages(ModelRequest request)
    {
        var messages = new JsonArray { new JsonObject { ["role"] = "system", ["content"] = request.SystemPrompt } };

        foreach (var message in request.Messages)
        {
            messages.Add(message.Role switch
            {
                ModelRole.User => new JsonObject { ["role"] = "user", ["content"] = message.Text },
                ModelRole.Tool => new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = message.ToolCallId,
                    ["name"] = message.ToolName,
                    ["content"] = message.Text,
                },
                _ when message.ToolCalls is { Count: > 0 } calls => new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = "",
                    ["tool_calls"] = new JsonArray([.. calls.Select(call => (JsonNode)new JsonObject
                    {
                        ["id"] = call.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.ArgumentsJson },
                    })]),
                },
                _ => new JsonObject { ["role"] = "assistant", ["content"] = message.Text },
            });
        }

        return messages;
    }

    /// <summary>OpenAI sends the arguments as a JSON string; Ollama sends an object. Both become JSON text.</summary>
    private static string Arguments(JsonNode? arguments) => arguments switch
    {
        null => "{}",
        JsonValue value when value.TryGetValue<string>(out var text) => text,
        _ => arguments.ToJsonString(),
    };
}

/// <summary>The HTTP half both adapters share: send, and turn every failure into one exception type.</summary>
internal static class ModelHttp
{
    public static async Task<JsonNode> SendAsync(HttpClient http, HttpRequestMessage message, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, cancellationToken);
        }
        catch (Exception failure) when (failure is HttpRequestException || (failure is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new LanguageModelException("The model could not be reached or did not answer in time.", failure);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // The status and the start of the body are enough to tell a wrong key from a full
                // quota in the log. The request is never logged: it holds the visitor's words.
                throw new LanguageModelException(
                    $"The model answered {(int)response.StatusCode}: {text[..Math.Min(300, text.Length)]}");
            }

            try
            {
                return JsonNode.Parse(text) ?? throw new LanguageModelException("The model answered with an empty body.");
            }
            catch (JsonException invalid)
            {
                throw new LanguageModelException("The model's answer was not JSON.", invalid);
            }
        }
    }
}
