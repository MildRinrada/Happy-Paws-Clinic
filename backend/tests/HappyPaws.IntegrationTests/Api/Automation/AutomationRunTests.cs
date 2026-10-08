using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Automation;
using HappyPaws.Application.Automation.RunAutomations;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Notifications;
using HappyPaws.Domain.Patients;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.AutomationScenario;

namespace HappyPaws.IntegrationTests.Api.Automation;

/// <summary>
/// The scheduler's real run against the simulation: the same evaluator, the same answer; each
/// reminder sent once, also when two workers race for it; and the consent gate in front of all of it.
/// </summary>
[Collection(nameof(JobsCollection))]
public class AutomationRunTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly object Visit24h = new { type = "appointment_upcoming", offset_hours = 24 };

    [Fact]
    public async Task What_simulate_says_is_due_now_is_exactly_what_the_real_run_sends()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var declined = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        await NotificationScenario.ConsentAsync(fixture, declined.CustomerId, ConsentPurpose.ServiceMessages, granted: false, Token);

        var now = DateTimeOffset.UtcNow;
        var due = await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        var dueToo = await AppointmentAsync(fixture, clinic, Hour(now, 22), petId: other.PetId, customerId: other.CustomerId, cancellationToken: Token);
        var dueDeclined = await AppointmentAsync(fixture, clinic, Hour(now, 21), petId: declined.PetId, customerId: declined.CustomerId, cancellationToken: Token);
        var later = await AppointmentAsync(fixture, clinic, Hour(now, 50), cancellationToken: Token);

        var rule = await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token);
        var ruleId = rule.GetProperty("id").GetString()!;

        using var client = fixture.Automator(clinic);
        var simulation = await (await client.PostAsJsonAsync($"/staff/automations/{ruleId}/simulate", new { days = 7 }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        var rows = simulation.GetProperty("recipients").EnumerateArray().ToList();
        var dueNow = rows.Where(row => row.GetProperty("due_now").GetBoolean()).ToList();

        Assert.Equal(
            new[] { due.Id, dueToo.Id, dueDeclined.Id }.Order(),
            dueNow.Select(row => row.GetProperty("subject").GetProperty("id").GetString()!).Order());
        Assert.Contains(rows, row => row.GetProperty("subject").GetProperty("id").GetString() == later.Id
            && !row.GetProperty("due_now").GetBoolean());
        Assert.Equal("would_skip", dueNow.Single(row => row.GetProperty("customer_id").GetString() == declined.CustomerId).GetProperty("status").GetString());
        Assert.Equal(3, simulation.GetProperty("due_now").GetInt32());

        var summary = await RunRuleAsync(fixture, ruleId, DateTimeOffset.UtcNow, Token);

        var dispatches = await DispatchesAsync(fixture, ruleId, Token);
        Assert.Equal(3, summary.Claimed);
        Assert.Equal(
            dueNow.Select(row => row.GetProperty("subject").GetProperty("id").GetString()!).Order(),
            dispatches.Select(item => item.SubjectId).Order());

        // Each row's predicted status is the outcome the real run recorded.
        foreach (var row in dueNow)
        {
            var dispatch = dispatches.Single(item => item.SubjectId == row.GetProperty("subject").GetProperty("id").GetString());
            var expected = row.GetProperty("status").GetString() == "would_send" ? DispatchStatus.Sent : DispatchStatus.Skipped;
            Assert.Equal(expected, dispatch.Status);
        }

        // And the words the preview showed are the words that went out.
        var sent = Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId), request => request.Data.GetValueOrDefault("appointment_id") == due.Id);
        var previewed = dueNow.Single(row => row.GetProperty("subject").GetProperty("id").GetString() == due.Id).GetProperty("message");
        Assert.Equal(NotificationTemplates.AppointmentReminder, sent.TemplateKey);
        Assert.Equal(DeliverySource.Automation, sent.Source);
        Assert.Equal(previewed.GetProperty("body").GetString(), NotificationText.Render(sent).Body);
        Assert.Equal(clinic.BranchId, sent.BranchId);
    }

    [Fact]
    public async Task The_consent_gate_stops_a_reminder_and_the_log_says_why()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await NotificationScenario.ConsentAsync(fixture, clinic.CustomerId, ConsentPurpose.ServiceMessages, granted: false, Token);

        var now = DateTimeOffset.UtcNow;
        await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        var ruleId = (await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token)).GetProperty("id").GetString()!;

        await RunRuleAsync(fixture, ruleId, now, Token);

        Assert.Empty(fixture.Notifications.SentTo(clinic.CustomerId));

        var dispatch = Assert.Single(await DispatchesAsync(fixture, ruleId, Token));
        Assert.Equal(DispatchStatus.Skipped, dispatch.Status);
        Assert.Equal("no_consent", dispatch.ErrorCode);

        var logged = Assert.Single(await NotificationScenario.DeliveriesAsync(fixture, clinic.CustomerId, Token));
        Assert.Equal(DeliveryStatus.Skipped, logged.Status);
        Assert.Equal("no_consent", logged.ErrorCode);
        Assert.Equal(DeliverySource.Automation, logged.Source);
        Assert.Equal(dispatch.Id, logged.BatchId);
    }

    [Fact]
    public async Task A_reminder_is_sent_once_however_many_times_the_rule_runs()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;
        var appointment = await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        var ruleId = (await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token)).GetProperty("id").GetString()!;

        var first = await RunRuleAsync(fixture, ruleId, now, Token);
        var second = await RunRuleAsync(fixture, ruleId, now.AddMinutes(1), Token);
        var third = await RunRuleAsync(fixture, ruleId, now.AddHours(2), Token);

        Assert.Equal(1, first.Claimed);
        Assert.Equal(0, second.Claimed + third.Claimed);
        Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId), request => request.Data.GetValueOrDefault("appointment_id") == appointment.Id);
        Assert.Single(await DispatchesAsync(fixture, ruleId, Token));

        // The preview agrees: nothing left to send now, and the row says it went.
        var after = await EvaluateAsync(fixture, ruleId, now.AddMinutes(2), cancellationToken: Token);
        Assert.Equal(OccurrenceStatus.AlreadySent, Assert.Single(after).Status);

        // The rule list counts it.
        using var client = fixture.Automator(clinic);
        var listed = (await client.GetFromJsonAsync<JsonElement>("/staff/automations", Token))
            .GetProperty("data").EnumerateArray().Single(item => item.GetProperty("id").GetString() == ruleId);
        Assert.Equal(1, listed.GetProperty("stats").GetProperty("sent").GetInt32());
        Assert.Equal(1, listed.GetProperty("stats").GetProperty("last_30_days").GetInt32());
    }

    [Fact]
    public async Task Workers_racing_for_the_same_reminders_send_each_one_once()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;
        var customers = new List<string> { clinic.CustomerId };

        await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);

        for (var i = 0; i < 3; i++)
        {
            var owner = await ClinicSeed.OtherOwnerAsync(fixture, Token);
            customers.Add(owner.CustomerId);
            await AppointmentAsync(fixture, clinic, Hour(now, 20 - i), petId: owner.PetId, customerId: owner.CustomerId, cancellationToken: Token);
        }

        var ruleId = (await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token)).GetProperty("id").GetString()!;

        // Five workers, each in its own scope and DbContext, as five API instances would be.
        var runs = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Task.Run(() => RunRuleAsync(fixture, ruleId, now, Token), Token)));

        Assert.Equal(4, runs.Sum(run => run.Claimed));
        Assert.Equal(4, (await DispatchesAsync(fixture, ruleId, Token)).Count);
        Assert.All(customers, customer => Assert.Single(fixture.Notifications.SentTo(customer)));
    }

    [Fact]
    public async Task A_moved_appointment_is_reminded_again_for_its_new_time()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;
        var appointment = await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        var ruleId = (await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token)).GetProperty("id").GetString()!;

        await RunRuleAsync(fixture, ruleId, now, Token);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var tracked = await db.Set<Appointment>().FirstAsync(item => item.Id == appointment.Id, Token);
            tracked.Reschedule(Hour(now, 22), Hour(now, 22).AddMinutes(30), staffId: null);
            await db.SaveChangesAsync(Token);
        }

        var again = await RunRuleAsync(fixture, ruleId, now.AddMinutes(1), Token);

        Assert.Equal(1, again.Claimed);
        Assert.Equal(2, fixture.Notifications.SentTo(clinic.CustomerId).Count(request => request.Data.GetValueOrDefault("appointment_id") == appointment.Id));
    }

    [Fact]
    public async Task Cancelled_walk_in_and_confirmed_visits_are_left_out_when_the_rule_says_so()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;
        var booked = await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        await AppointmentAsync(fixture, clinic, Hour(now, 22), appointment => appointment.Confirm(now), cancellationToken: Token);
        await AppointmentAsync(fixture, clinic, Hour(now, 21), appointment => appointment.Cancel(now, "usr_test", "ไม่ว่าง"), cancellationToken: Token);

        var ruleId = (await CreateRuleAsync(
            fixture,
            clinic,
            Visit24h,
            new { branch_id = clinic.BranchId, unconfirmed_only = true },
            cancellationToken: Token)).GetProperty("id").GetString()!;

        var occurrences = await EvaluateAsync(fixture, ruleId, now, cancellationToken: Token);

        Assert.Equal(booked.Id, Assert.Single(occurrences).Subject.SubjectId);
    }

    [Fact]
    public async Task A_vaccine_falling_due_is_reminded_from_the_latest_dose_only_and_never_for_a_deceased_patient()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var replaced = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var deceased = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var past = DateTimeOffset.UtcNow.AddDays(-30);

        // The pets belong to this branch through their last visit.
        await AppointmentAsync(fixture, clinic, Hour(past, 0), cancellationToken: Token);
        await AppointmentAsync(fixture, clinic, Hour(past, 2), petId: replaced.PetId, customerId: replaced.CustomerId, cancellationToken: Token);
        await AppointmentAsync(fixture, clinic, Hour(past, 4), petId: deceased.PetId, customerId: deceased.CustomerId, cancellationToken: Token);

        var dueOn = ClinicToday.AddDays(20);
        var dose = await VaccinationAsync(fixture, clinic.PetId, "Rabies", dueOn, cancellationToken: Token);
        await VaccinationAsync(fixture, replaced.PetId, "Rabies", dueOn, givenDaysAgo: 300, cancellationToken: Token);
        await VaccinationAsync(fixture, replaced.PetId, "rabies", dueOn.AddYears(1), givenDaysAgo: 2, cancellationToken: Token);
        await VaccinationAsync(fixture, deceased.PetId, "Rabies", dueOn, cancellationToken: Token);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            (await db.Set<Pet>().FirstAsync(pet => pet.Id == deceased.PetId, Token)).MarkDeceased(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(Token);
        }

        var ruleId = (await CreateRuleAsync(
            fixture,
            clinic,
            new { type = "vaccine_due", offset_days = 14, send_time = "10:00" },
            new { branch_id = clinic.BranchId, vaccine_name = "RABIES" },
            cancellationToken: Token)).GetProperty("id").GetString()!;

        // Ten past ten on the clinic day fourteen days before the due date.
        var sendAt = ClinicTime(dueOn.AddDays(-14), 10);
        var occurrences = await EvaluateAsync(fixture, ruleId, sendAt.AddMinutes(10), cancellationToken: Token);

        var occurrence = Assert.Single(occurrences);
        Assert.Equal(dose.Id, occurrence.Subject.SubjectId);
        Assert.Equal(sendAt, occurrence.SendAt);
        Assert.Equal("Rabies", occurrence.Request.Data["vaccine_name"]);

        // A minute before ten nothing is due yet; the same evaluator, so the run agrees.
        Assert.Equal(0, (await RunRuleAsync(fixture, ruleId, sendAt.AddMinutes(-1), Token)).Claimed);
        Assert.Equal(1, (await RunRuleAsync(fixture, ruleId, sendAt.AddMinutes(10), Token)).Claimed);

        var sent = Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId), request => request.TemplateKey == NotificationTemplates.VaccineDue);
        Assert.Equal(clinic.BranchId, sent.BranchId);
        Assert.Empty(fixture.Notifications.SentTo(replaced.CustomerId));
        Assert.Empty(fixture.Notifications.SentTo(deceased.CustomerId));
    }

    [Fact]
    public async Task A_treatment_plan_session_is_reminded_the_day_before_with_its_number()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await AppointmentAsync(fixture, clinic, Hour(DateTimeOffset.UtcNow.AddDays(-10), 0), cancellationToken: Token);

        var firstDay = ClinicToday.AddDays(3);
        var plan = await PlanAsync(fixture, clinic, "คอร์สกายภาพ", [ClinicTime(firstDay, 10), ClinicTime(firstDay.AddDays(7), 10)], Token);

        var ruleId = (await CreateRuleAsync(
            fixture,
            clinic,
            new { type = "treatment_plan_session", offset_days = 1, send_time = "09:00" },
            cancellationToken: Token)).GetProperty("id").GetString()!;

        var now = ClinicTime(firstDay.AddDays(-1), 9, 5);
        var occurrence = Assert.Single(await EvaluateAsync(fixture, ruleId, now, cancellationToken: Token));

        Assert.Equal($"{plan.Id}:1", occurrence.Subject.SubjectId);
        Assert.Equal("1", occurrence.Request.Data["session_number"]);
        Assert.Equal("10:00", occurrence.Request.Data["time"]);
        Assert.Equal("คอร์สกายภาพ", occurrence.Request.Data["plan_title"]);

        Assert.Equal(1, (await RunRuleAsync(fixture, ruleId, now, Token)).Claimed);
        Assert.Single(fixture.Notifications.SentTo(clinic.CustomerId), request => request.TemplateKey == NotificationTemplates.TreatmentPlanSession);
    }

    [Fact]
    public async Task The_upcoming_list_holds_what_enabled_rules_will_send_and_nothing_already_sent()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;

        var sentAlready = await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        var tomorrow = await AppointmentAsync(fixture, clinic, Hour(now, 48), cancellationToken: Token);

        var enabledId = (await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token)).GetProperty("id").GetString()!;
        var disabledId = (await CreateRuleAsync(fixture, clinic, new { type = "appointment_upcoming", offset_hours = 2 }, enabled: false, cancellationToken: Token))
            .GetProperty("id").GetString()!;

        await RunRuleAsync(fixture, enabledId, now, Token);

        using var client = fixture.Automator(clinic);
        var upcoming = await client.GetFromJsonAsync<JsonElement>("/staff/reminders/upcoming", Token);
        var mine = upcoming.GetProperty("data").EnumerateArray()
            .Where(row => BranchOf(row) == clinic.BranchId)
            .ToList();

        var row = Assert.Single(mine);
        Assert.Equal(tomorrow.Id, row.GetProperty("subject").GetProperty("id").GetString());
        Assert.Equal(enabledId, row.GetProperty("automation_id").GetString());
        Assert.False(row.GetProperty("due_now").GetBoolean());
        Assert.DoesNotContain(mine, item => item.GetProperty("subject").GetProperty("id").GetString() == sentAlready.Id);
        Assert.DoesNotContain(mine, item => item.GetProperty("automation_id").GetString() == disabledId);

        // Another branch's reader does not see this branch's reminders.
        using var outsider = fixture.Automator(elsewhere);
        var theirs = await outsider.GetFromJsonAsync<JsonElement>("/staff/reminders/upcoming", Token);
        Assert.DoesNotContain(theirs.GetProperty("data").EnumerateArray(), item => BranchOf(item) == clinic.BranchId);
    }

    [Fact]
    public async Task A_claim_left_by_a_stopped_worker_is_marked_lost_and_never_sent()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var now = DateTimeOffset.UtcNow;
        var appointment = await AppointmentAsync(fixture, clinic, Hour(now, 23), cancellationToken: Token);
        var ruleId = (await CreateRuleAsync(fixture, clinic, Visit24h, cancellationToken: Token)).GetProperty("id").GetString()!;

        var occurrence = Assert.Single(await EvaluateAsync(fixture, ruleId, now, cancellationToken: Token));
        using (var scope = fixture.CreateScope())
        {
            var claim = AutomationDispatch.Claim(
                ruleId, occurrence.Subject.Kind, occurrence.Subject.SubjectId, occurrence.DueKey, clinic.CustomerId,
                clinic.PetId, clinic.BranchId, occurrence.SendAt, now.AddMinutes(-30), "dead-worker:1");
            Assert.True(await scope.ServiceProvider.GetRequiredService<IAutomationDispatchRepository>().TryClaimAsync(claim, Token));
        }

        using (var scope = fixture.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<RunAutomationsHandler>().RunAsync(now, Token);
        }

        var dispatch = Assert.Single(await DispatchesAsync(fixture, ruleId, Token));
        Assert.Equal(DispatchStatus.Lost, dispatch.Status);
        Assert.DoesNotContain(fixture.Notifications.SentTo(clinic.CustomerId), request => request.Data.GetValueOrDefault("appointment_id") == appointment.Id);
    }

    /// <summary>A reminder's branch; absent from the JSON when it has none.</summary>
    private static string? BranchOf(JsonElement row) =>
        row.TryGetProperty("branch_id", out var branch) ? branch.GetString() : null;

    /// <summary>A whole hour some hours from an instant, so two appointments in a test never overlap.</summary>
    private static DateTimeOffset Hour(DateTimeOffset from, int hours)
    {
        var at = from.AddHours(hours);
        return new DateTimeOffset(at.Year, at.Month, at.Day, at.Hour, 0, 0, at.Offset).AddMinutes(30);
    }
}
