using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPaws.Application.Automation;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Automation;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.AutomationScenario;

namespace HappyPaws.IntegrationTests.Api.Automation;

/// <summary>
/// <c>PATCH</c> and <c>DELETE /staff/automations/{id}</c> (<c>docs/contract-extensions.md § 19</c>):
/// the create rules hold on an edit, an edit never sends a reminder twice, the branch rule is the
/// create rule, and a deleted rule's history stays.
/// </summary>
[Collection(nameof(JobsCollection))]
public class AutomationEditTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly object Visit24h = new { type = "appointment_upcoming", offset_hours = 24 };

    private static DateTimeOffset Hour(DateTimeOffset now, int hours) =>
        new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset).AddHours(hours).AddMinutes(30);

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string id, string json, uint? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/staff/automations/{id}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        if (version is { } value)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{value}\"");
        }

        return client.SendAsync(request, Token);
    }

    private async Task<AutomationRule?> StoredRuleAsync(string id)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<AutomationRule>().AsNoTracking().FirstOrDefaultAsync(rule => rule.Id == id, Token);
    }

    private async Task<IReadOnlyList<AuditEntry>> AuditAsync(string action, string id)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.Action == action && entry.ResourceId == id)
            .ToListAsync(Token);
    }

    public static TheoryData<string> Methods => new() { "PATCH", "DELETE" };

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task Editing_and_deleting_need_automation_manage(string method)
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Automation.Read, Scopes.Notify.Manage]));

        var response = await client.SendAsync(
            new HttpRequestMessage(new HttpMethod(method), "/staff/automations/atm_starter_visit_24h") { Content = JsonContent.Create(new { enabled = true }) },
            Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("insufficient_scope", body.GetProperty("error").GetProperty("code").GetString());

        using var anonymous = fixture.CreateClient();
        var refused = await anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/staff/automations/atm_starter_visit_24h"), Token);
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        Assert.False((await StoredRuleAsync("atm_starter_visit_24h"))!.Enabled);
    }

    public static TheoryData<string, string, string> BadEdits => new()
    {
        { "appointment", "name", """{"name":""}""" },
        { "appointment", "trigger.type", """{"trigger":{"type":"vaccine_due","offset_days":3}}""" },
        { "appointment", "trigger.offset_days", """{"trigger":{"offset_days":1,"offset_hours":2}}""" },
        { "appointment", "trigger.offset_hours", """{"trigger":{"offset_hours":500}}""" },
        { "appointment", "trigger.send_time", """{"trigger":{"send_time":"10:00"}}""" },
        { "appointment", "condition.vaccine_name", """{"condition":{"vaccine_name":"Rabies"}}""" },
        { "appointment", "action.channel", """{"action":{"channel":"fax"}}""" },
        { "appointment", "action.template", """{"action":{"template":"vaccine.due"}}""" },
        { "vaccine", "trigger.offset_hours", """{"trigger":{"offset_hours":24}}""" },
        { "vaccine", "trigger.offset_days", """{"trigger":{"offset_days":120}}""" },
        { "vaccine", "condition.unconfirmed_only", """{"condition":{"unconfirmed_only":true}}""" },
        { "vaccine", "condition.service_id", """{"condition":{"service_id":"svc_x"}}""" },
    };

    [Theory]
    [MemberData(nameof(BadEdits))]
    public async Task An_edit_is_held_to_the_create_rules_and_changes_nothing_when_refused(string kind, string field, string json)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var trigger = kind == "vaccine" ? new { type = "vaccine_due", offset_days = 14 } : (object)Visit24h;
        var rule = await CreateRuleAsync(fixture, clinic, trigger, enabled: false, cancellationToken: Token);
        var id = rule.GetProperty("id").GetString()!;
        using var client = fixture.Automator(clinic);

        var response = await PatchAsync(client, id, json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _), $"no error on {field}: {error}");

        var stored = await StoredRuleAsync(id);
        Assert.Equal(rule.GetProperty("name").GetString(), stored!.Name);
        Assert.Empty(await AuditAsync(AuditActions.AutomationUpdated, id));
    }

    [Fact]
    public async Task A_rule_is_switched_on_and_off_with_its_version_and_each_switch_is_audited()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var rule = await CreateRuleAsync(fixture, clinic, Visit24h, enabled: false, cancellationToken: Token);
        var id = rule.GetProperty("id").GetString()!;
        var version = rule.GetProperty("version").GetUInt32();
        using var client = fixture.Automator(clinic);

        var on = await PatchAsync(client, id, """{"enabled":true}""", version);

        Assert.Equal(HttpStatusCode.OK, on.StatusCode);
        var body = await on.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.True(body.GetProperty("enabled").GetBoolean());
        Assert.NotEqual(version, body.GetProperty("version").GetUInt32());
        Assert.Equal(24, body.GetProperty("trigger").GetProperty("offset_hours").GetInt32());
        Assert.NotNull(on.Headers.ETag);

        // The version from before the switch is stale now.
        var stale = await PatchAsync(client, id, """{"enabled":false}""", version);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("version_conflict", (await stale.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("code").GetString());

        var off = await PatchAsync(client, id, """{"enabled":false}""", body.GetProperty("version").GetUInt32());
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.False((await StoredRuleAsync(id))!.Enabled);

        var audited = await AuditAsync(AuditActions.AutomationUpdated, id);
        Assert.Equal(2, audited.Count);
        Assert.All(audited, entry => Assert.Contains("enabled", entry.DetailsJson!));
    }

    [Fact]
    public async Task Name_timing_conditions_and_channel_change_together_and_a_blank_clears_a_condition()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var rule = await CreateRuleAsync(
            fixture,
            clinic,
            new { type = "vaccine_due", offset_days = 14 },
            new { branch_id = clinic.BranchId, species = "สุนัข", vaccine_name = "Rabies" },
            enabled: false,
            cancellationToken: Token);
        var id = rule.GetProperty("id").GetString()!;
        using var client = fixture.Automator(clinic);

        var response = await PatchAsync(
            client,
            id,
            """{"name":"เตือนวัคซีน 7 วัน","trigger":{"offset_days":7,"send_time":"10:30"},"condition":{"species":""},"action":{"channel":"sms"}}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("เตือนวัคซีน 7 วัน", body.GetProperty("name").GetString());
        Assert.Equal(7, body.GetProperty("trigger").GetProperty("offset_days").GetInt32());
        Assert.Equal("10:30", body.GetProperty("trigger").GetProperty("send_time").GetString());
        Assert.Equal("sms", body.GetProperty("action").GetProperty("channel").GetString());
        Assert.False(body.GetProperty("condition").TryGetProperty("species", out _));
        Assert.Equal("Rabies", body.GetProperty("condition").GetProperty("vaccine_name").GetString());
        Assert.Equal(clinic.BranchId, body.GetProperty("condition").GetProperty("branch_id").GetString());
        Assert.False(body.GetProperty("enabled").GetBoolean());

        // Only the hour: the days stay.
        var later = await PatchAsync(client, id, """{"trigger":{"send_time":"08:00"}}""");
        var moved = await later.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(7, moved.GetProperty("trigger").GetProperty("offset_days").GetInt32());
        Assert.Equal("08:00", moved.GetProperty("trigger").GetProperty("send_time").GetString());
    }

    [Fact]
    public async Task Moving_the_timing_never_reminds_a_visit_that_was_already_reminded()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;
        var appointment = await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        var rule = await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token);
        var id = rule.GetProperty("id").GetString()!;

        Assert.Equal(1, (await RunRuleAsync(fixture, id, now, Token)).Claimed);

        using var client = fixture.Automator(clinic);
        var edit = await PatchAsync(client, id, """{"trigger":{"offset_hours":2}}""");
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

        // The new "2 hours before" point for the same visit: already sent, nothing goes.
        var atNewPoint = appointment.StartsAt.AddHours(-2).AddMinutes(1);
        var occurrences = await EvaluateAsync(fixture, id, atNewPoint, cancellationToken: Token);
        Assert.Equal(OccurrenceStatus.AlreadySent, Assert.Single(occurrences).Status);
        Assert.Equal(0, (await RunRuleAsync(fixture, id, atNewPoint, Token)).Claimed);

        Assert.Single(await DispatchesAsync(fixture, id, Token));
        Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId), request => request.Data.GetValueOrDefault("appointment_id") == appointment.Id);
    }

    [Fact]
    public async Task A_rule_switched_off_and_on_again_does_not_send_twice_either()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;
        await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        var id = (await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token)).GetProperty("id").GetString()!;
        using var client = fixture.Automator(clinic);

        Assert.Equal(1, (await RunRuleAsync(fixture, id, now, Token)).Claimed);
        (await PatchAsync(client, id, """{"enabled":false}""")).EnsureSuccessStatusCode();
        (await PatchAsync(client, id, """{"enabled":true}""")).EnsureSuccessStatusCode();

        Assert.Equal(0, (await RunRuleAsync(fixture, id, now.AddMinutes(5), Token)).Claimed);
        Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId));
    }

    [Fact]
    public async Task Another_branchs_rule_and_a_clinic_wide_rule_are_out_of_a_branch_bound_members_reach()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirId = (await CreateRuleAsync(fixture, theirs, Visit24h, enabled: false, cancellationToken: Token)).GetProperty("id").GetString()!;
        var myId = (await CreateRuleAsync(fixture, mine, Visit24h, enabled: false, cancellationToken: Token)).GetProperty("id").GetString()!;
        using var client = fixture.Automator(mine);

        Assert.Equal(HttpStatusCode.Forbidden, (await PatchAsync(client, theirId, """{"enabled":true}""")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/staff/automations/{theirId}", Token)).StatusCode);

        // The starter rules belong to the whole clinic: seen, previewed, not changed from one branch.
        Assert.Equal(HttpStatusCode.Forbidden, (await PatchAsync(client, "atm_starter_visit_24h", """{"enabled":true}""")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/staff/automations/atm_starter_visit_24h", Token)).StatusCode);

        // Nor can a rule of their own be made clinic-wide, or moved to a branch they do not work at.
        var wide = await PatchAsync(client, myId, """{"condition":{"branch_id":""}}""");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wide.StatusCode);
        var moved = await PatchAsync(client, myId, JsonSerializer.Serialize(new { condition = new { branch_id = theirs.BranchId } }));
        Assert.Equal(HttpStatusCode.Forbidden, moved.StatusCode);

        Assert.False((await StoredRuleAsync(theirId))!.Enabled);
        Assert.False((await StoredRuleAsync("atm_starter_visit_24h"))!.Enabled);
        Assert.Equal(mine.BranchId, (await StoredRuleAsync(myId))!.BranchId);
    }

    [Fact]
    public async Task A_deleted_rule_leaves_every_list_and_run_and_its_history_stays()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;
        await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        var rule = await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token);
        var id = rule.GetProperty("id").GetString()!;
        Assert.Equal(1, (await RunRuleAsync(fixture, id, now, Token)).Claimed);
        using var client = fixture.Automator(clinic);

        var stale = new HttpRequestMessage(HttpMethod.Delete, $"/staff/automations/{id}");
        stale.Headers.TryAddWithoutValidation("If-Match", $"\"{rule.GetProperty("version").GetUInt32() + 1}\"");
        Assert.Equal(HttpStatusCode.Conflict, (await client.SendAsync(stale, Token)).StatusCode);

        var response = await client.DeleteAsync($"/staff/automations/{id}", Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var list = await client.GetFromJsonAsync<JsonElement>("/staff/automations", Token);
        Assert.DoesNotContain(list.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetString() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/staff/automations/{id}/simulate", new { }, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PatchAsync(client, id, """{"enabled":true}""")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/staff/automations/{id}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/staff/reminders/upcoming?automation_id={id}", Token)).StatusCode);

        using (var scope = fixture.CreateScope())
        {
            var enabled = await scope.ServiceProvider.GetRequiredService<IAutomationRuleRepository>().EnabledAsync(Token);
            Assert.DoesNotContain(enabled, item => item.Id == id);
        }

        // The row and its "already sent" history are kept, under the name it had.
        var stored = await StoredRuleAsync(id);
        Assert.NotNull(stored!.DeletedAt);
        Assert.False(stored.Enabled);
        Assert.Equal(clinic.StaffId, stored.DeletedByStaffId);
        Assert.Equal(rule.GetProperty("name").GetString(), stored.Name);
        Assert.Single(await DispatchesAsync(fixture, id, Token));
        Assert.Single(await AuditAsync(AuditActions.AutomationDeleted, id));
    }
}
