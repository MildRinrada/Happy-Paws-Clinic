using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Customers;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.AutomationScenario;

namespace HappyPaws.IntegrationTests.Api.Forecasting;

/// <summary>
/// The <c>customer_lapsed</c> reminder (<c>docs/contract-extensions.md § 33</c>): the due-back rule's
/// customers, one message per absence, and only to customers who agreed to marketing. Care consent,
/// on by default, is not enough.
/// </summary>
[Collection(nameof(JobsCollection))]
public class LapsedReminderTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly object SevenDaysLate = new { type = "customer_lapsed", offset_days = -7, send_time = "09:00" };

    [Fact]
    public async Task Lapsed_customers_are_written_to_under_marketing_consent_only_and_the_preview_matches_the_run()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var careOnly = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var silent = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var today = ForecastScenario.Today;

        // Monthly visits, the last 66 days ago: due back 6 days ago, so seven days late tomorrow.
        int[] visits = [126, 96, 66];
        await ForecastScenario.VisitsAsync(fixture, clinic, clinic.CustomerId, clinic.PetId, visits, Token);
        await ForecastScenario.VisitsAsync(fixture, clinic, careOnly.CustomerId, careOnly.PetId, visits, Token);
        await ForecastScenario.VisitsAsync(fixture, clinic, silent.CustomerId, silent.PetId, visits, Token);

        await NotificationScenario.ConsentAsync(fixture, clinic.CustomerId, ConsentPurpose.Marketing, granted: true, Token);
        await NotificationScenario.ConsentAsync(fixture, careOnly.CustomerId, ConsentPurpose.ServiceMessages, granted: true, Token);
        await NotificationScenario.ConsentAsync(fixture, careOnly.CustomerId, ConsentPurpose.Marketing, granted: false, Token);

        var rule = await CreateRuleAsync(fixture, clinic, SevenDaysLate, cancellationToken: Token);
        var ruleId = rule.GetProperty("id").GetString()!;
        Assert.Equal("customer_lapsed", rule.GetProperty("trigger").GetProperty("type").GetString());
        Assert.Equal(NotificationTemplates.CustomerLapsed, rule.GetProperty("action").GetProperty("template").GetString());

        using var client = fixture.Automator(clinic);
        var simulation = await (await client.PostAsJsonAsync($"/staff/automations/{ruleId}/simulate", new { days = 7 }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        var rows = simulation.GetProperty("recipients").EnumerateArray().ToDictionary(row => row.GetProperty("customer_id").GetString()!);
        Assert.Equal(3, rows.Count);
        Assert.All(rows.Values, row => Assert.Equal("customer", row.GetProperty("subject").GetProperty("type").GetString()));
        Assert.All(rows.Values, row => Assert.Equal(today.AddDays(-6).ToString("yyyy-MM-dd"), row.GetProperty("subject").GetProperty("due_on").GetString()));
        Assert.Equal("would_send", rows[clinic.CustomerId].GetProperty("status").GetString());
        Assert.Equal("would_skip", rows[careOnly.CustomerId].GetProperty("status").GetString());
        Assert.Equal("no_consent", rows[careOnly.CustomerId].GetProperty("skip_reason").GetString());
        Assert.Equal("would_skip", rows[silent.CustomerId].GetProperty("status").GetString());
        Assert.Contains("ลูน่า", rows[clinic.CustomerId].GetProperty("message").GetProperty("title").GetString());

        var summary = await RunRuleAsync(fixture, ruleId, ClinicTime(today.AddDays(1), 9, 30), Token);
        Assert.Equal(3, summary.Claimed);

        var dispatches = await DispatchesAsync(fixture, ruleId, Token);
        Assert.All(dispatches, dispatch => Assert.Equal(AutomationSubjectKind.Customer, dispatch.SubjectKind));
        Assert.Equal(DispatchStatus.Sent, dispatches.Single(item => item.CustomerId == clinic.CustomerId).Status);
        Assert.Equal(DispatchStatus.Skipped, dispatches.Single(item => item.CustomerId == careOnly.CustomerId).Status);
        Assert.Equal(DispatchStatus.Skipped, dispatches.Single(item => item.CustomerId == silent.CustomerId).Status);

        Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId), request => request.TemplateKey == NotificationTemplates.CustomerLapsed);
        Assert.DoesNotContain(fixture.Notifications.SentTo(careOnly.CustomerId), request => request.TemplateKey == NotificationTemplates.CustomerLapsed);
        Assert.DoesNotContain(fixture.Notifications.SentTo(silent.CustomerId), request => request.TemplateKey == NotificationTemplates.CustomerLapsed);

        // Once per absence: the next tick finds it already sent.
        var again = await RunRuleAsync(fixture, ruleId, ClinicTime(today.AddDays(1), 9, 45), Token);
        Assert.Equal(0, again.Claimed);
    }

    [Theory]
    [InlineData("trigger.offset_days", """{ "type": "customer_lapsed", "offset_days": 0 }""", null)]
    [InlineData("trigger.offset_hours", """{ "type": "customer_lapsed", "offset_hours": 24 }""", null)]
    [InlineData("condition.service_id", """{ "type": "customer_lapsed", "offset_days": -7 }""", """{ "service_id": "svc_any" }""")]
    public async Task A_lapsed_rule_counts_whole_days_after_the_due_back_day_and_names_no_service(string field, string trigger, string? condition)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Automator(clinic);

        var json = $$"""{ "name": "คิดถึงน้อง", "trigger": {{trigger}}, "condition": {{condition ?? "null"}} }""";
        var response = await client.PostAsync("/staff/automations", new StringContent(json, System.Text.Encoding.UTF8, "application/json"), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _), $"no error on {field}: {error}");
    }
}
