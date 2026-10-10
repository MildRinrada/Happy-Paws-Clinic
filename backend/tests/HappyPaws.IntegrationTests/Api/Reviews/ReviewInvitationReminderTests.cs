using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Automation.RunAutomations;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Reviews.Invitations;
using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Reviews;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.AutomationScenario;

namespace HappyPaws.IntegrationTests.Api.Reviews;

/// <summary>
/// The <c>visit_completed</c> reminder (<c>docs/contract-extensions.md § 36</c>): days after a visit,
/// a review invitation goes to customers who agreed to marketing, with a portal link for account
/// holders and a one-time link for everyone else, once per visit. Care consent is not enough.
/// </summary>
[Collection(nameof(JobsCollection))]
public class ReviewInvitationReminderTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly object OneDayAfter = new { type = "visit_completed", offset_days = -1, send_time = "09:00" };

    private static DateTimeOffset Tomorrow(int hour, int minute = 0) => ClinicTime(ReviewScenario.Today.AddDays(1), hour, minute);

    [Fact]
    public async Task Invitations_go_under_marketing_consent_only_and_the_preview_matches_the_run()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var careOnly = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var silent = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        // Today's visits: one day after is tomorrow at 09:00.
        await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 0, Token);
        await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 0, Token, careOnly.CustomerId, careOnly.PetId);
        await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 0, Token, silent.CustomerId, silent.PetId);

        await NotificationScenario.ConsentAsync(fixture, clinic.CustomerId, ConsentPurpose.Marketing, granted: true, Token);
        await NotificationScenario.ConsentAsync(fixture, careOnly.CustomerId, ConsentPurpose.ServiceMessages, granted: true, Token);
        await NotificationScenario.ConsentAsync(fixture, careOnly.CustomerId, ConsentPurpose.Marketing, granted: false, Token);

        var rule = await CreateRuleAsync(fixture, clinic, OneDayAfter, cancellationToken: Token);
        var ruleId = rule.GetProperty("id").GetString()!;
        Assert.Equal("visit_completed", rule.GetProperty("trigger").GetProperty("type").GetString());
        Assert.Equal(NotificationTemplates.ReviewInvitation, rule.GetProperty("action").GetProperty("template").GetString());

        using var client = fixture.Automator(clinic);
        var simulation = await (await client.PostAsJsonAsync($"/staff/automations/{ruleId}/simulate", new { days = 7 }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        var rows = simulation.GetProperty("recipients").EnumerateArray().ToDictionary(row => row.GetProperty("customer_id").GetString()!);
        Assert.Equal(3, rows.Count);
        Assert.All(rows.Values, row => Assert.Equal("visit", row.GetProperty("subject").GetProperty("type").GetString()));
        Assert.Equal("would_send", rows[clinic.CustomerId].GetProperty("status").GetString());
        Assert.Equal("would_skip", rows[careOnly.CustomerId].GetProperty("status").GetString());
        Assert.Equal("no_consent", rows[careOnly.CustomerId].GetProperty("skip_reason").GetString());
        Assert.Equal("would_skip", rows[silent.CustomerId].GetProperty("status").GetString());
        Assert.Equal("no_consent", rows[silent.CustomerId].GetProperty("skip_reason").GetString());

        // The preview made nothing.
        Assert.Empty(await ReviewScenario.InvitationsOfAsync(fixture, clinic.CustomerId, Token));

        var summary = await RunRuleAsync(fixture, ruleId, Tomorrow(9, 30), Token);
        Assert.Equal(3, summary.Claimed);

        var dispatches = await DispatchesAsync(fixture, ruleId, Token);
        Assert.All(dispatches, dispatch => Assert.Equal(AutomationSubjectKind.Visit, dispatch.SubjectKind));
        Assert.Equal(DispatchStatus.Sent, dispatches.Single(item => item.CustomerId == clinic.CustomerId).Status);
        Assert.Equal(DispatchStatus.Skipped, dispatches.Single(item => item.CustomerId == careOnly.CustomerId).Status);
        Assert.Equal("no_consent", dispatches.Single(item => item.CustomerId == careOnly.CustomerId).ErrorCode);
        Assert.Equal(DispatchStatus.Skipped, dispatches.Single(item => item.CustomerId == silent.CustomerId).Status);
        Assert.Equal("no_consent", dispatches.Single(item => item.CustomerId == silent.CustomerId).ErrorCode);

        // Skipped customers get no message and no token.
        Assert.DoesNotContain(fixture.Notifications.SentTo(careOnly.CustomerId), request => request.TemplateKey == NotificationTemplates.ReviewInvitation);
        Assert.DoesNotContain(fixture.Notifications.SentTo(silent.CustomerId), request => request.TemplateKey == NotificationTemplates.ReviewInvitation);
        Assert.Empty(await ReviewScenario.InvitationsOfAsync(fixture, careOnly.CustomerId, Token));
        Assert.Empty(await ReviewScenario.InvitationsOfAsync(fixture, silent.CustomerId, Token));

        // The one who agreed gets a one-time link, and only its hash is kept.
        var sent = Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId), request => request.TemplateKey == NotificationTemplates.ReviewInvitation);
        var link = sent.Data[ReviewInvitationIssuer.LinkKey];
        Assert.StartsWith("http://localhost:3000/review/hprv_", link, StringComparison.Ordinal);
        var token = link["http://localhost:3000/review/".Length..];

        var invitation = Assert.Single(await ReviewScenario.InvitationsOfAsync(fixture, clinic.CustomerId, Token));
        Assert.Equal(ReviewInvitation.Hash(token), invitation.TokenHash);
        Assert.Equal(dispatches.Single(item => item.CustomerId == clinic.CustomerId).Id, invitation.DispatchId);
        Assert.Equal(ReviewScenario.Today, invitation.VisitOn);

        using var anonymous = fixture.CreateClient();
        var page = await anonymous.GetAsync($"/reviews/invitations/{token}", Token);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);

        // Once per visit: the next tick finds it already sent.
        Assert.Equal(0, (await RunRuleAsync(fixture, ruleId, Tomorrow(9, 45), Token)).Claimed);
    }

    [Fact]
    public async Task A_customer_with_a_portal_account_is_sent_the_portal_and_no_token()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await ReviewScenario.LinkAccountAsync(fixture, clinic.CustomerId, Token);
        await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 0, Token);
        await NotificationScenario.ConsentAsync(fixture, clinic.CustomerId, ConsentPurpose.Marketing, granted: true, Token);

        var rule = await CreateRuleAsync(fixture, clinic, OneDayAfter, cancellationToken: Token);
        await RunRuleAsync(fixture, rule.GetProperty("id").GetString()!, Tomorrow(9, 30), Token);

        var sent = Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId), request => request.TemplateKey == NotificationTemplates.ReviewInvitation);
        Assert.Equal("http://localhost:3000/portal/reviews", sent.Data[ReviewInvitationIssuer.LinkKey]);

        var invitation = Assert.Single(await ReviewScenario.InvitationsOfAsync(fixture, clinic.CustomerId, Token));
        Assert.Null(invitation.TokenHash);
    }

    [Fact]
    public async Task A_second_rule_on_the_same_trigger_does_not_invite_a_visit_again()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 0, Token);
        await NotificationScenario.ConsentAsync(fixture, clinic.CustomerId, ConsentPurpose.Marketing, granted: true, Token);

        var first = await CreateRuleAsync(fixture, clinic, OneDayAfter, cancellationToken: Token);
        var second = await CreateRuleAsync(fixture, clinic, new { type = "visit_completed", offset_days = -1, send_time = "09:10" }, cancellationToken: Token);

        // Both see the visit before either runs, as two instances racing would.
        var occurrence = Assert.Single(await EvaluateAsync(fixture, second.GetProperty("id").GetString()!, Tomorrow(9, 20), cancellationToken: Token));

        Assert.Equal(1, (await RunRuleAsync(fixture, first.GetProperty("id").GetString()!, Tomorrow(9, 30), Token)).Claimed);

        // The second rule now finds the visit invited and does not even claim it.
        Assert.Equal(0, (await RunRuleAsync(fixture, second.GetProperty("id").GetString()!, Tomorrow(9, 30), Token)).Claimed);

        // And had it claimed it in the same moment, the invitation's unique key stops the send.
        using var scope = fixture.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<ReviewInvitationIssuer>();
        Assert.Null(await issuer.PrepareAsync(occurrence, "ard_race", Token));

        Assert.Single(await ReviewScenario.InvitationsOfAsync(fixture, clinic.CustomerId, Token));
        Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId), request => request.TemplateKey == NotificationTemplates.ReviewInvitation);
        Assert.Equal("already_invited", RunAutomationsHandler.AlreadyInvited);
    }

    [Fact]
    public async Task A_reviewed_visit_and_a_deceased_pet_are_not_invited()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var bill = await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 0, Token);
        await NotificationScenario.ConsentAsync(fixture, clinic.CustomerId, ConsentPurpose.Marketing, granted: true, Token);

        using var portal = fixture.Portal(clinic);
        Assert.Equal(HttpStatusCode.Created, (await portal.PostAsJsonAsync("/portal/reviews", ReviewScenario.Body(visitId: bill), Token)).StatusCode);

        // A pet recorded deceased since: its owner is not asked to score that visit.
        var bereaved = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        await ReviewScenario.BillAsync(fixture, clinic, daysAgo: 0, Token, bereaved.CustomerId, bereaved.PetId);
        await NotificationScenario.ConsentAsync(fixture, bereaved.CustomerId, ConsentPurpose.Marketing, granted: true, Token);
        await ReviewScenario.MarkDeceasedAsync(fixture, bereaved.PetId, Token);

        var rule = await CreateRuleAsync(fixture, clinic, OneDayAfter, cancellationToken: Token);
        Assert.Empty(await EvaluateAsync(fixture, rule.GetProperty("id").GetString()!, Tomorrow(9, 20), cancellationToken: Token));
    }

    [Theory]
    [InlineData("trigger.offset_days", """{ "type": "visit_completed", "offset_days": 2 }""", null)]
    [InlineData("trigger.offset_days", """{ "type": "visit_completed", "offset_days": -31 }""", null)]
    [InlineData("trigger.offset_hours", """{ "type": "visit_completed", "offset_hours": 48 }""", null)]
    [InlineData("condition.service_id", """{ "type": "visit_completed", "offset_days": -2 }""", """{ "service_id": "svc_any" }""")]
    public async Task A_review_rule_counts_whole_days_after_the_visit_and_names_no_service(string field, string trigger, string? condition)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.Automator(clinic);

        var json = $$"""{ "name": "ชวนรีวิว", "trigger": {{trigger}}, "condition": {{condition ?? "null"}} }""";
        var response = await client.PostAsync("/staff/automations", new StringContent(json, System.Text.Encoding.UTF8, "application/json"), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _), $"no error on {field}: {error}");
    }
}
