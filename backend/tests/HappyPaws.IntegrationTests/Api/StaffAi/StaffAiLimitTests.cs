using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.StaffAi;

/// <summary>
/// The monthly allowance on a deployment allowed three uses (<see cref="StaffAiLimitFixture"/>):
/// requests racing for the last uses cannot pass the limit, a spent month answers
/// <c>429 ai_quota_exceeded</c>, and a cached summary is still served because it costs nothing.
/// </summary>
[Collection(nameof(StaffAiLimitCollection))]
public class StaffAiLimitTests(StaffAiLimitFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Concurrent_requests_never_pass_the_limit_and_the_rest_are_refused_with_429()
    {
        var open = await ConsultationScenario.OpenCaseAsync(fixture, Token);
        using var vet = fixture.AiVet(open.Clinic);

        fixture.Model.Reset();
        for (var i = 0; i < 12; i++) fixture.Model.Answer(ModelScript.Text("""{"chief_complaint":"อาเจียน","examination":[]}"""));

        // One use on a summary first, so the cache can be read back once the month is spent.
        var summary = await vet.PostAsync($"/staff/ai/pets/{open.Clinic.PetId}/summary", null, Token);
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);

        // Ten drafts at once for the two uses left.
        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            vet.PostAsJsonAsync($"/staff/ai/consultations/{open.ConsultationId}/soap-draft", new { notes = "อาเจียน" }, Token)));

        Assert.Equal(2, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(8, responses.Count(response => response.StatusCode == HttpStatusCode.TooManyRequests));

        var refusal = (await responses.First(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("ai_quota_exceeded", refusal.GetProperty("code").GetString());
        Assert.Contains("เดือน", refusal.GetProperty("message").GetString());
        Assert.Equal(StaffAiLimitFixture.MonthlyCalls, refusal.GetProperty("details").GetProperty("limit").GetInt32());

        // The model was called exactly as often as the allowance let it be.
        Assert.Equal(StaffAiLimitFixture.MonthlyCalls, fixture.Model.Requests.Count);

        var usage = await StaffAiScenario.UsageAsync(vet, Token);
        Assert.Equal(StaffAiLimitFixture.MonthlyCalls, usage.GetProperty("used").GetInt32());
        Assert.Equal(StaffAiLimitFixture.MonthlyCalls, usage.GetProperty("limit").GetInt32());
        Assert.Equal(2, usage.GetProperty("by_feature").GetProperty("soap_draft").GetInt32());
        Assert.Equal(1, usage.GetProperty("by_feature").GetProperty("pet_summary").GetInt32());

        // A summary of unchanged data is a cache hit: served with the month spent, and not counted.
        var cached = await vet.PostAsync($"/staff/ai/pets/{open.Clinic.PetId}/summary", null, Token);
        Assert.Equal(HttpStatusCode.OK, cached.StatusCode);
        Assert.True((await cached.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("cached").GetBoolean());

        // Anything that needs the model is refused, and a refused request reaches no model.
        var ask = await vet.PostAsJsonAsync("/staff/ai/ask", new { messages = new[] { new { role = "user", text = "สวัสดี" } } }, Token);
        Assert.Equal(HttpStatusCode.TooManyRequests, ask.StatusCode);
        Assert.Equal(StaffAiLimitFixture.MonthlyCalls, fixture.Model.Requests.Count);
        Assert.Equal(StaffAiLimitFixture.MonthlyCalls, (await StaffAiScenario.UsageAsync(vet, Token)).GetProperty("used").GetInt32());
    }
}
