using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// A stand-in model server: answers each request with the next scripted response and keeps what it
/// was sent. Behind the real adapter, so the request the adapter builds and the parsing of the answer
/// are what the tests exercise; only the model itself is not real.
/// </summary>
public sealed class ModelScript
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _answers = new();
    private readonly List<JsonNode> _requests = [];
    private readonly object _gate = new();

    public IReadOnlyList<JsonNode> Requests
    {
        get { lock (_gate) return [.. _requests]; }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _answers.Clear();
            _requests.Clear();
        }
    }

    public void Answer(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        lock (_gate) _answers.Enqueue((status, body));
    }

    internal (HttpStatusCode, string) Next(JsonNode request)
    {
        lock (_gate)
        {
            _requests.Add(request);
            return _answers.Count > 0 ? _answers.Dequeue() : (HttpStatusCode.InternalServerError, """{"error":"script ran out"}""");
        }
    }

    /// <summary>An OpenAI-format answer in words.</summary>
    public static string Text(string text) =>
        new JsonObject
        {
            ["choices"] = new JsonArray(new JsonObject { ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = text } }),
            ["usage"] = new JsonObject { ["prompt_tokens"] = 100, ["completion_tokens"] = 20 },
        }.ToJsonString();

    /// <summary>An OpenAI-format answer that calls one tool.</summary>
    public static string Call(string tool, string argumentsJson = "{}") =>
        new JsonObject
        {
            ["choices"] = new JsonArray(new JsonObject
            {
                ["message"] = new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = null,
                    ["tool_calls"] = new JsonArray(new JsonObject
                    {
                        ["id"] = "call_1",
                        ["type"] = "function",
                        ["function"] = new JsonObject { ["name"] = tool, ["arguments"] = argumentsJson },
                    }),
                },
            }),
            ["usage"] = new JsonObject { ["prompt_tokens"] = 100, ["completion_tokens"] = 10 },
        }.ToJsonString();
}

/// <summary>The handler the assistant's HttpClient sends through in tests.</summary>
public sealed class ScriptedModelHandler(ModelScript script) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
        body["_path"] = request.RequestUri!.AbsolutePath;
        body["_auth"] = request.Headers.Authorization?.ToString()
            ?? (request.Headers.TryGetValues("x-goog-api-key", out var keys) ? keys.First() : null);

        var (status, answer) = script.Next(body);

        return new HttpResponseMessage(status) { Content = new StringContent(answer, Encoding.UTF8, "application/json") };
    }
}
