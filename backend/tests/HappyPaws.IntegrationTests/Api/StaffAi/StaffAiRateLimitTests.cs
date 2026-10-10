using System.Net;
using System.Net.Http.Json;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.StaffAi;

/// <summary>
/// The per-person rate limit on the routes that call the model (<c>StaffAi:CallsPerMinutePerUser</c>,
/// two in <see cref="StaffAiRateLimitFixture"/>): a third call in the minute is refused with
/// <c>429</c> before it reaches the model, and another person's window is their own.
/// </summary>
[Collection(nameof(StaffAiRateLimitCollection))]
public class StaffAiRateLimitTests(StaffAiRateLimitFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_third_call_in_a_minute_is_refused_and_another_person_still_gets_through()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var first = fixture.AiVet(clinic);
        using var second = fixture.Authenticated(TestTokens.Staff(
            fixture, "usr_second_ai", clinic.StaffId, StaffAiScenario.AiVetScopes, [clinic.BranchId]));

        fixture.Model.Reset();
        for (var i = 0; i < 3; i++) fixture.Model.Answer(ModelScript.Text("สวัสดีครับ"));
        var ask = new { messages = new[] { new { role = "user", text = "สวัสดี" } } };

        Assert.Equal(HttpStatusCode.OK, (await first.PostAsJsonAsync("/staff/ai/ask", ask, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await first.PostAsJsonAsync("/staff/ai/ask", ask, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await first.PostAsJsonAsync("/staff/ai/ask", ask, Token)).StatusCode);
        Assert.Equal(2, fixture.Model.Requests.Count);

        Assert.Equal(HttpStatusCode.OK, (await second.PostAsJsonAsync("/staff/ai/ask", ask, Token)).StatusCode);
        Assert.Equal(3, fixture.Model.Requests.Count);
    }
}
