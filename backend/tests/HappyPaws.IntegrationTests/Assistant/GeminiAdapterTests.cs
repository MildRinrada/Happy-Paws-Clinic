using System.Text.Json.Nodes;
using HappyPaws.Application.Assistant;
using HappyPaws.Infrastructure.Assistant;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.Options;

namespace HappyPaws.IntegrationTests.Assistant;

/// <summary>
/// The Gemini adapter's half of the port, without a model: what it sends, and what it makes of
/// what comes back. The integration tests run the assistant through the OpenAI-format adapter; this
/// keeps the second adapter honest.
/// </summary>
public class GeminiAdapterTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static (GeminiLanguageModel Model, ModelScript Script) Build()
    {
        var script = new ModelScript();
        var http = new HttpClient(new ScriptedModelHandler(script));
        var options = Options.Create(new AssistantOptions { Provider = "gemini", Model = "gemini-test", ApiKey = "g-key" });

        return (new GeminiLanguageModel(http, options), script);
    }

    private static readonly ModelTool[] Tools =
    [
        new("get_clinic_info", "info", """{"type":"object","properties":{}}"""),
        new("find_available_times", "times", """{"type":"object","properties":{"date":{"type":"string"}},"required":["date"]}"""),
    ];

    [Fact]
    public async Task A_request_is_in_Gemini_shape_with_the_key_in_a_header()
    {
        var (model, script) = Build();
        script.Answer("""{"candidates":[{"content":{"role":"model","parts":[{"text":"สวัสดีค่ะ"}]}}],"usageMetadata":{"promptTokenCount":50,"candidatesTokenCount":5}}""");

        var turn = await model.CompleteAsync(new ModelRequest("rules", [ModelMessage.User("สวัสดี")], Tools), Token);

        var sent = script.Requests[0];
        Assert.Equal("/v1beta/models/gemini-test:generateContent", sent["_path"]!.GetValue<string>());
        Assert.Equal("g-key", sent["_auth"]!.GetValue<string>());
        Assert.Equal("rules", sent["systemInstruction"]!["parts"]![0]!["text"]!.GetValue<string>());
        Assert.Equal("user", sent["contents"]![0]!["role"]!.GetValue<string>());

        var declarations = sent["tools"]![0]!["functionDeclarations"]!.AsArray();

        // A function with no arguments has no schema at all; the other's types are upper-cased.
        Assert.Null(declarations[0]!["parameters"]);
        Assert.Equal("OBJECT", declarations[1]!["parameters"]!["type"]!.GetValue<string>());
        Assert.Equal("STRING", declarations[1]!["parameters"]!["properties"]!["date"]!["type"]!.GetValue<string>());

        Assert.Equal("สวัสดีค่ะ", turn.Text);
        Assert.Equal(50, turn.InputTokens);
    }

    [Fact]
    public async Task A_function_call_comes_back_with_its_signature_and_goes_out_again_with_it()
    {
        var (model, script) = Build();
        script.Answer("""
            {"candidates":[{"content":{"role":"model","parts":[
              {"text":"thinking...","thought":true},
              {"functionCall":{"name":"find_available_times","args":{"date":"2026-10-05"}},"thoughtSignature":"sig-1"}
            ]}}]}
            """);

        var turn = await model.CompleteAsync(new ModelRequest("rules", [ModelMessage.User("ว่างไหม")], Tools), Token);

        var call = Assert.Single(turn.ToolCalls);
        Assert.Equal("find_available_times", call.Name);
        Assert.Equal("2026-10-05", JsonNode.Parse(call.ArgumentsJson)!["date"]!.GetValue<string>());
        Assert.Equal("sig-1", call.Signature);
        Assert.Null(turn.Text);

        script.Answer("""{"candidates":[{"content":{"parts":[{"text":"มีค่ะ"}]}}]}""");
        await model.CompleteAsync(
            new ModelRequest("rules", [ModelMessage.User("ว่างไหม"), ModelMessage.Calls([call]), ModelMessage.Result(call, """{"free_times":["10:00"]}""")], Tools),
            Token);

        var contents = script.Requests[1]["contents"]!.AsArray();
        Assert.Equal("sig-1", contents[1]!["parts"]![0]!["thoughtSignature"]!.GetValue<string>());
        Assert.Equal("10:00", contents[2]!["parts"]![0]!["functionResponse"]!["response"]!["free_times"]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task A_refused_request_is_a_model_failure()
    {
        var (model, script) = Build();
        script.Answer("""{"error":{"code":400,"message":"API key not valid"}}""", System.Net.HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<LanguageModelException>(() =>
            model.CompleteAsync(new ModelRequest("rules", [ModelMessage.User("hi")], Tools), Token));
    }
}
