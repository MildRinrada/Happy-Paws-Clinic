using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Jobs;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static HappyPaws.IntegrationTests.Fixtures.AutomationScenario;

namespace HappyPaws.IntegrationTests.Api.Automation;

/// <summary>
/// Who may use the automation endpoints, what they refuse, and the branch rule. The rules made
/// here are bound to each test's own branch.
/// </summary>
[Collection(nameof(JobsCollection))]
public class AutomationEndpointTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public static TheoryData<string, string, string> Endpoints => new()
    {
        { "GET", "/staff/automations", Scopes.Automation.Read },
        { "POST", "/staff/automations", Scopes.Automation.Manage },
        { "POST", "/staff/automations/atm_starter_visit_24h/simulate", Scopes.Automation.Manage },
        { "GET", "/staff/reminders/upcoming", Scopes.Automation.Read },
    };

    private static HttpRequestMessage Request(string method, string path) =>
        new(new HttpMethod(method), path) { Content = method == "GET" ? null : JsonContent.Create(new { }) };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Each_endpoint_needs_its_own_scope(string method, string path, string scope)
    {
        string[] all = [Scopes.Automation.Read, Scopes.Automation.Manage, Scopes.Report.Export, Scopes.Notify.Send];
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [.. all.Where(other => other != scope)]));

        var response = await client.SendAsync(Request(method, path), Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("insufficient_scope", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Each_endpoint_refuses_an_anonymous_caller(string method, string path, string scope)
    {
        _ = scope;
        using var client = fixture.CreateClient();

        var response = await client.SendAsync(Request(method, path), Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, string> BadRules => new()
    {
        { "name", """{"trigger":{"type":"vaccine_due","offset_days":14}}""" },
        { "trigger", """{"name":"x"}""" },
        { "trigger.type", """{"name":"x","trigger":{"type":"birthday","offset_days":1}}""" },
        { "trigger.offset_days", """{"name":"x","trigger":{"type":"vaccine_due"}}""" },
        { "trigger.offset_days", """{"name":"x","trigger":{"type":"appointment_upcoming","offset_days":1,"offset_hours":2}}""" },
        { "trigger.offset_hours", """{"name":"x","trigger":{"type":"vaccine_due","offset_hours":24}}""" },
        { "trigger.offset_hours", """{"name":"x","trigger":{"type":"appointment_upcoming","offset_hours":500}}""" },
        { "trigger.offset_days", """{"name":"x","trigger":{"type":"vaccine_due","offset_days":120}}""" },
        { "trigger.send_time", """{"name":"x","trigger":{"type":"vaccine_due","offset_days":14,"send_time":"9am"}}""" },
        { "condition.vaccine_name", """{"name":"x","trigger":{"type":"appointment_upcoming","offset_hours":2},"condition":{"vaccine_name":"Rabies"}}""" },
        { "condition.unconfirmed_only", """{"name":"x","trigger":{"type":"vaccine_due","offset_days":2},"condition":{"unconfirmed_only":true}}""" },
        { "action.channel", """{"name":"x","trigger":{"type":"vaccine_due","offset_days":14},"action":{"channel":"fax"}}""" },
        { "action.type", """{"name":"x","trigger":{"type":"vaccine_due","offset_days":14},"action":{"type":"webhook"}}""" },
        { "action.template", """{"name":"x","trigger":{"type":"vaccine_due","offset_days":14},"action":{"template":"appointment.reminder"}}""" },
    };

    [Theory]
    [MemberData(nameof(BadRules))]
    public async Task A_rule_that_cannot_be_run_is_refused_with_the_field_named(string field, string json)
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Automation.Manage]));

        var response = await client.PostAsync(
            "/staff/automations", new StringContent(json, System.Text.Encoding.UTF8, "application/json"), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _), $"no error on {field}: {error}");
    }

    [Fact]
    public async Task A_rule_is_created_off_by_default_listed_with_empty_stats_and_audited()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Automator(clinic);

        var response = await client.PostAsJsonAsync(
            "/staff/automations",
            new
            {
                name = "ครบกำหนดวัคซีน 14 วัน",
                trigger = new { type = "vaccine_due", offset_days = 14 },
                condition = new { species = "สุนัข", vaccine_name = "Rabies" },
                action = new { type = "send_notification", channel = "line" },
            },
            Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var rule = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var id = rule.GetProperty("id").GetString()!;

        Assert.StartsWith("atm_", id);
        Assert.False(rule.GetProperty("enabled").GetBoolean());
        Assert.Equal("vaccine_due", rule.GetProperty("trigger").GetProperty("type").GetString());
        Assert.Equal("09:00", rule.GetProperty("trigger").GetProperty("send_time").GetString());
        Assert.Equal(clinic.BranchId, rule.GetProperty("condition").GetProperty("branch_id").GetString());
        Assert.Equal("line", rule.GetProperty("action").GetProperty("channel").GetString());
        Assert.Equal("vaccine.due", rule.GetProperty("action").GetProperty("template").GetString());

        var list = await client.GetFromJsonAsync<JsonElement>("/staff/automations", Token);
        var listed = list.GetProperty("data").EnumerateArray().Single(item => item.GetProperty("id").GetString() == id);
        Assert.Equal(0, listed.GetProperty("stats").GetProperty("total").GetInt32());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var audited = await db.Set<AuditEntry>().AsNoTracking()
            .AnyAsync(entry => entry.Action == AuditActions.AutomationCreated && entry.ResourceId == id, Token);
        Assert.True(audited);
    }

    [Fact]
    public async Task The_starter_rules_are_there_and_switched_off()
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Automation.Read]));

        var rules = (await client.GetFromJsonAsync<JsonElement>("/staff/automations", Token))
            .GetProperty("data").EnumerateArray()
            .Where(item => item.GetProperty("id").GetString()!.StartsWith("atm_starter_", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(4, rules.Count);
        Assert.All(rules, rule => Assert.False(rule.GetProperty("enabled").GetBoolean()));
        Assert.Contains(rules, rule => rule.GetProperty("trigger").GetProperty("offset_hours").ValueKind == JsonValueKind.Number
            && rule.GetProperty("trigger").GetProperty("offset_hours").GetInt32() == 24);
    }

    [Fact]
    public async Task A_branch_bound_member_cannot_make_or_see_another_branchs_rules()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirRule = await CreateRuleAsync(fixture, theirs, new { type = "appointment_upcoming", offset_hours = 24 }, enabled: false, cancellationToken: Token);
        var theirId = theirRule.GetProperty("id").GetString()!;

        using var client = fixture.Automator(mine);

        var create = await client.PostAsJsonAsync(
            "/staff/automations",
            new { name = "ของสาขาอื่น", trigger = new { type = "appointment_upcoming", offset_hours = 2 }, condition = new { branch_id = theirs.BranchId } },
            Token);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var list = await client.GetFromJsonAsync<JsonElement>("/staff/automations", Token);
        Assert.DoesNotContain(list.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetString() == theirId);
        Assert.Contains(list.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetString() == "atm_starter_visit_24h");

        var simulate = await client.PostAsJsonAsync($"/staff/automations/{theirId}/simulate", new { }, Token);
        Assert.Equal(HttpStatusCode.Forbidden, simulate.StatusCode);

        var upcoming = await client.GetAsync($"/staff/reminders/upcoming?branch={theirs.BranchId}", Token);
        Assert.Equal(HttpStatusCode.Forbidden, upcoming.StatusCode);
    }

    [Fact]
    public async Task A_member_with_several_branches_must_say_which_and_only_an_unrestricted_one_makes_a_clinic_wide_rule()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var body = new { name = "ไม่ระบุสาขา", trigger = new { type = "appointment_upcoming", offset_hours = 2 } };

        using var twoBranches = fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: [Scopes.Automation.Manage], branches: [clinic.BranchId, other.BranchId]));
        var refused = await twoBranches.PostAsJsonAsync("/staff/automations", body, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);

        using var unrestricted = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Automation.Manage]));
        var created = await unrestricted.PostAsJsonAsync("/staff/automations", body, Token);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var rule = await created.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.False(rule.GetProperty("condition").TryGetProperty("branch_id", out _));
    }

    [Fact]
    public async Task Unknown_branches_services_and_rules_are_told_apart()
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: AutomationScopes));

        var branch = await client.PostAsJsonAsync(
            "/staff/automations",
            new { name = "x", trigger = new { type = "appointment_upcoming", offset_hours = 2 }, condition = new { branch_id = "brn_nope" } },
            Token);
        Assert.Equal(HttpStatusCode.NotFound, branch.StatusCode);

        var service = await client.PostAsJsonAsync(
            "/staff/automations",
            new { name = "x", trigger = new { type = "appointment_upcoming", offset_hours = 2 }, condition = new { service_id = "svc_nope" } },
            Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, service.StatusCode);

        var simulate = await client.PostAsJsonAsync("/staff/automations/atm_nope/simulate", new { }, Token);
        Assert.Equal(HttpStatusCode.NotFound, simulate.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public async Task A_look_ahead_outside_one_to_thirty_days_is_refused(int days)
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: AutomationScopes));

        var simulate = await client.PostAsJsonAsync("/staff/automations/atm_starter_visit_24h/simulate", new { days }, Token);
        var upcoming = await client.GetAsync($"/staff/reminders/upcoming?days={days}", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, simulate.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, upcoming.StatusCode);
    }

    [Fact]
    public async Task Simulate_takes_an_empty_body()
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: AutomationScopes));

        var response = await client.PostAsync("/staff/automations/atm_starter_visit_24h/simulate", content: null, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.False(body.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public void The_scheduler_is_off_in_the_test_host()
    {
        var options = fixture.Services.GetRequiredService<IOptions<JobsOptions>>().Value;

        Assert.False(options.Enabled);
    }
}
