using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.StaffAi.DailyBrief;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.StaffAi;

/// <summary>
/// A deployment with <c>StaffAi:Provider=off</c>: every route that needs the model answers
/// <c>503 ai_unavailable</c> with nothing about the provider in it, usage says <c>enabled: false</c>
/// so the pages hide the buttons, and the website assistant is untouched.
/// </summary>
[Collection(nameof(StaffAiOffCollection))]
public class StaffAiOffTests(StaffAiOffFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Every_model_route_answers_503_and_the_model_hears_nothing()
    {
        var open = await ConsultationScenario.OpenCaseAsync(fixture, Token);
        using var vet = fixture.AiVet(open.Clinic);
        fixture.Model.Reset();

        var responses = new[]
        {
            await vet.PostAsync($"/staff/ai/pets/{open.Clinic.PetId}/summary", null, Token),
            await vet.PostAsJsonAsync($"/staff/ai/consultations/{open.ConsultationId}/soap-draft", new { notes = "อาเจียน" }, Token),
            await vet.PostAsJsonAsync("/staff/ai/drafts/owner-message", new { kind = "aftercare", consultation_id = open.ConsultationId }, Token),
            await vet.PostAsJsonAsync("/staff/ai/ask", new { messages = new[] { new { role = "user", text = "สวัสดี" } } }, Token),
        };

        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync(Token);
            Assert.Contains("ai_unavailable", body);
            Assert.DoesNotContain("test-key", body);
            Assert.DoesNotContain("model.test", body);
        }

        Assert.Empty(fixture.Model.Requests);
    }

    [Fact]
    public async Task Usage_says_off_and_the_brief_says_disabled_and_the_website_assistant_is_still_on()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.AiVet(clinic);

        var usage = await StaffAiScenario.UsageAsync(vet, Token);
        Assert.False(usage.GetProperty("enabled").GetBoolean());
        Assert.Equal(0, usage.GetProperty("used").GetInt32());

        var brief = await vet.GetFromJsonAsync<JsonElement>($"/staff/ai/daily-brief?branch_id={clinic.BranchId}", Token);
        Assert.Equal("disabled", brief.GetProperty("status").GetString());

        using var anyone = fixture.CreateClient();
        Assert.True((await anyone.GetFromJsonAsync<JsonElement>("/assistant", Token)).GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task The_brief_job_writes_nothing_while_the_assistant_is_off()
    {
        await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        fixture.Model.Reset();

        using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GenerateDailyBriefsHandler>();

        Assert.Equal(0, await handler.RunAsync(DateTimeOffset.UtcNow.AddHours(12), Token));
        Assert.Empty(fixture.Model.Requests);
    }
}
