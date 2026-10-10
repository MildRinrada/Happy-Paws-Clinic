using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.AutomationScenario;

namespace HappyPaws.IntegrationTests.Api.Automation;

/// <summary>
/// Reminders after a recorded death is undone (contract-extensions § 29): they resume from the
/// moment of the undo, and what fell due while the record was closed is not sent late in a burst,
/// even inside the scheduler's catch-up window.
/// </summary>
[Collection(nameof(JobsCollection))]
public class UndoDeceasedReminderTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_reminder_that_fell_due_while_the_record_was_closed_is_not_sent_and_later_ones_are()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var alive = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var past = DateTimeOffset.UtcNow.AddDays(-30);

        // Both pets belong to this branch through their last visit.
        await AppointmentAsync(fixture, clinic, past, cancellationToken: Token);
        await AppointmentAsync(fixture, clinic, past.AddHours(2), petId: alive.PetId, customerId: alive.CustomerId, cancellationToken: Token);

        using var counter = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Patient.Write], [clinic.BranchId]));

        (await counter.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/deceased", new { }, Token)).EnsureSuccessStatusCode();

        var undo = await counter.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/deceased/undo", new { reason = "บันทึกผิดตัว" }, Token);
        undo.EnsureSuccessStatusCode();
        var undoneAt = (await undo.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("undone_at").GetDateTimeOffset();

        // A send time two to three hours before the undo: well inside the 12-hour catch-up window,
        // so without the rule the next run would still send it.
        var target = TimeZoneInfo.ConvertTime(undoneAt, Bangkok).AddHours(-2);
        var sendDay = DateOnly.FromDateTime(target.DateTime);
        var sendAt = ClinicTime(sendDay, target.Hour);

        var stale = await VaccinationAsync(fixture, clinic.PetId, "Rabies", sendDay.AddDays(14), cancellationToken: Token);
        var control = await VaccinationAsync(fixture, alive.PetId, "Rabies", sendDay.AddDays(14), cancellationToken: Token);
        var upcoming = await VaccinationAsync(fixture, clinic.PetId, "DHPP", ClinicToday.AddDays(20), cancellationToken: Token);

        var ruleId = (await CreateRuleAsync(
            fixture,
            clinic,
            new { type = "vaccine_due", offset_days = 14, send_time = $"{target.Hour:00}:00" },
            cancellationToken: Token)).GetProperty("id").GetString()!;

        var now = undoneAt.AddMinutes(1);
        Assert.True(sendAt < undoneAt && sendAt > now.AddHours(-12));

        var due = await EvaluateAsync(fixture, ruleId, now, cancellationToken: Token);

        // The pet that was never closed is reminded; the one whose death was undone is not.
        Assert.Equal([control.Id], due.Select(occurrence => occurrence.Subject.SubjectId).ToList());
        Assert.Equal(1, (await RunRuleAsync(fixture, ruleId, now, Token)).Claimed);
        Assert.DoesNotContain(fixture.Notifications.SentTo(clinic.CustomerId), request => request.TemplateKey == NotificationTemplates.VaccineDue);

        // A reminder whose time comes after the undo goes out as usual.
        var later = ClinicTime(ClinicToday.AddDays(6), target.Hour);
        var resumed = await EvaluateAsync(fixture, ruleId, later.AddMinutes(10), cancellationToken: Token);

        Assert.Contains(resumed, occurrence => occurrence.Subject.SubjectId == upcoming.Id);
        Assert.DoesNotContain(resumed, occurrence => occurrence.Subject.SubjectId == stale.Id);
    }
}
