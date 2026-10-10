using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.StaffAi.DailyBrief;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.StaffAi;

/// <summary>
/// The morning brief (<c>docs/contract-extensions.md § 34</c>): written by the job once per branch
/// per clinic day from 07:00, numbers by code and words by the model, read by anyone with
/// <c>dashboard:read</c> without spending a use. The job is driven by hand at chosen instants.
/// </summary>
[Collection(nameof(StaffAiBriefCollection))]
public class DailyBriefTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>Today in Bangkok at the given hour.</summary>
    private static DateTimeOffset At(int hour)
    {
        var today = DateOnly.ParseExact(ClinicSeed.ClinicDate(DateTimeOffset.UtcNow), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new DateTimeOffset(today.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.FromHours(7));
    }

    private async Task<int> RunAsync(DateTimeOffset at)
    {
        using var scope = fixture.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<GenerateDailyBriefsHandler>().RunAsync(at, Token);
    }

    /// <summary>Writes today's brief for every branch that lacks one, so a test starts from a known state.</summary>
    private async Task DrainAsync()
    {
        using (var scope = fixture.CreateScope())
        {
            var branches = await scope.ServiceProvider.GetRequiredService<IBranchRepository>().ListActiveAsync(Token);
            fixture.Model.Reset();
            foreach (var _ in branches) fixture.Model.Answer(ModelScript.Text("สรุปวันนี้"));
        }

        await RunAsync(At(8));
        fixture.Model.Reset();
    }

    private HttpClient Staff(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(fixture, clinic.StaffUserId, clinic.StaffId, scopes, [clinic.BranchId]));

    private async Task<JsonElement> BriefAsync(HttpClient client, string branchId) =>
        await client.GetFromJsonAsync<JsonElement>($"/staff/ai/daily-brief?branch_id={branchId}", Token);

    [Fact]
    public async Task The_brief_is_written_once_per_branch_per_day_from_seven_and_reading_it_costs_nothing()
    {
        await DrainAsync();
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var front = Staff(clinic, Scopes.Dashboard.Read);
        using var manager = Staff(clinic, Scopes.Dashboard.Read, Scopes.Analytics.Read, Scopes.Ai.Use);

        var pending = await BriefAsync(front, clinic.BranchId);
        Assert.Equal("pending", pending.GetProperty("status").GetString());
        Assert.Equal("07:00", pending.GetProperty("generates_at").GetString());

        fixture.Model.Reset();
        fixture.Model.Answer(ModelScript.Text("วันนี้นัดยังว่าง สต๊อกเรียบร้อย ขอให้เป็นวันที่ดีค่ะ"));

        Assert.Equal(0, await RunAsync(At(6)));
        Assert.Empty(fixture.Model.Requests);

        Assert.Equal(1, await RunAsync(At(8)));
        Assert.Equal(0, await RunAsync(At(9)));

        var request = Assert.Single(fixture.Model.Requests);
        var data = StaffAiScenario.UserMessage(request);
        Assert.Contains("Branch: สาขาทดสอบ", data);
        Assert.Contains("Appointments today: total", data);
        Assert.DoesNotContain("satang", data, StringComparison.OrdinalIgnoreCase);

        var usageBefore = (await StaffAiScenario.UsageAsync(manager, Token)).GetProperty("by_feature").GetProperty("daily_brief").GetInt32();

        var brief = await BriefAsync(front, clinic.BranchId);
        Assert.Equal("ready", brief.GetProperty("status").GetString());
        Assert.Equal("วันนี้นัดยังว่าง สต๊อกเรียบร้อย ขอให้เป็นวันที่ดีค่ะ", brief.GetProperty("text").GetString());
        Assert.Equal(0, brief.GetProperty("numbers").GetProperty("appointments").GetProperty("total").GetInt32());

        // Money only for a caller who may read money, the dashboard's own rule.
        Assert.False(brief.GetProperty("numbers").TryGetProperty("yesterday_revenue", out _));
        var withMoney = await BriefAsync(manager, clinic.BranchId);
        Assert.Equal(0, withMoney.GetProperty("numbers").GetProperty("yesterday_revenue").GetProperty("net_satang").GetInt64());

        Assert.Equal(usageBefore, (await StaffAiScenario.UsageAsync(manager, Token)).GetProperty("by_feature").GetProperty("daily_brief").GetInt32());
        Assert.True(usageBefore >= 1);
    }

    [Fact]
    public async Task Two_workers_racing_for_a_branch_write_its_brief_once()
    {
        await DrainAsync();
        await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        fixture.Model.Reset();
        fixture.Model.Answer(ModelScript.Text("สรุป"));
        fixture.Model.Answer(ModelScript.Text("สรุปซ้ำ"));

        var written = await Task.WhenAll(RunAsync(At(8)), RunAsync(At(8)), RunAsync(At(8).AddMinutes(5)));

        Assert.Equal(1, written.Sum());
        Assert.Single(fixture.Model.Requests);
    }

    [Fact]
    public async Task A_failed_model_keeps_the_numbers_and_the_next_run_tries_again()
    {
        await DrainAsync();
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var front = Staff(clinic, Scopes.Dashboard.Read);

        fixture.Model.Reset();
        fixture.Model.Answer("""{"error":"overloaded"}""", HttpStatusCode.ServiceUnavailable);

        Assert.Equal(1, await RunAsync(At(8)));

        var failed = await BriefAsync(front, clinic.BranchId);
        Assert.Equal("failed", failed.GetProperty("status").GetString());
        Assert.Equal("model_unavailable", failed.GetProperty("reason").GetString());
        Assert.True(failed.TryGetProperty("numbers", out var numbers) && numbers.ValueKind == JsonValueKind.Object);
        Assert.False(failed.TryGetProperty("text", out _));

        fixture.Model.Answer(ModelScript.Text("สรุปรอบสอง"));
        Assert.Equal(1, await RunAsync(At(8)));

        var ready = await BriefAsync(front, clinic.BranchId);
        Assert.Equal("ready", ready.GetProperty("status").GetString());
        Assert.Equal("สรุปรอบสอง", ready.GetProperty("text").GetString());
    }

    [Fact]
    public async Task The_brief_is_read_within_the_caller_s_branches()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var front = Staff(mine, Scopes.Dashboard.Read);
        using var owner = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Wildcard]));

        Assert.Equal(HttpStatusCode.Forbidden, (await front.GetAsync($"/staff/ai/daily-brief?branch_id={theirs.BranchId}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await front.GetAsync("/staff/ai/daily-brief", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync("/staff/ai/daily-brief?branch_id=brn_nowhere", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/staff/ai/daily-brief?branch_id={theirs.BranchId}", Token)).StatusCode);
    }
}
