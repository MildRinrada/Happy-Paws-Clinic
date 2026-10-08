using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Automation;
using HappyPaws.Application.Automation.RunAutomations;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Patients;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// A clinic of its own for the scheduler's work: reminder rules and report exports.
/// <para>
/// Separate from <see cref="ApiCollection"/> because a real run evaluates every enabled rule over
/// every record, and an export worker takes every queued job. On the shared database that would
/// send reminders to other tests' customers and build other tests' files. Here every test still
/// makes its own branch, and its rules are bound to that branch.
/// </para>
/// </summary>
[CollectionDefinition(nameof(JobsCollection))]
public sealed class JobsCollection : ICollectionFixture<ApiFixture>;

/// <summary>Rules, the records they watch, and driving the evaluator and the run by hand.</summary>
public static class AutomationScenario
{
    public static readonly string[] AutomationScopes = [Scopes.Automation.Read, Scopes.Automation.Manage];

    /// <summary>A member of staff bound to the clinic's branch, holding the automation scopes (or those given).</summary>
    public static HttpClient Automator(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? AutomationScopes : scopes,
            [clinic.BranchId]));

    /// <summary>Creates a rule through the API, enabled unless told otherwise, at the clinic's branch.</summary>
    public static async Task<JsonElement> CreateRuleAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        object trigger,
        object? condition = null,
        bool enabled = true,
        string channel = "auto",
        CancellationToken cancellationToken = default)
    {
        using var client = fixture.Automator(clinic);

        var response = await client.PostAsJsonAsync(
            "/staff/automations",
            new
            {
                name = $"กติกาทดสอบ {Guid.NewGuid():N}"[..30],
                trigger,
                condition = condition ?? new { branch_id = clinic.BranchId },
                action = new { type = "send_notification", channel },
                enabled,
            },
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"create rule answered {(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>A booked appointment for the clinic's pet, written straight to the table.</summary>
    public static async Task<Appointment> AppointmentAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        DateTimeOffset startsAt,
        Action<Appointment>? change = null,
        string? petId = null,
        string? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var appointment = Appointment.Book(
            clinic.BranchId,
            customerId ?? clinic.CustomerId,
            petId ?? clinic.PetId,
            clinic.ServiceId,
            clinic.StaffId,
            startsAt,
            startsAt.AddMinutes(30));

        change?.Invoke(appointment);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<Appointment>().Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        return appointment;
    }

    /// <summary>A dose given some days ago, due again on <paramref name="nextDueOn"/>.</summary>
    public static async Task<Vaccination> VaccinationAsync(
        ApiFixture fixture,
        string petId,
        string vaccineName,
        DateOnly nextDueOn,
        int givenDaysAgo = 300,
        CancellationToken cancellationToken = default)
    {
        var givenAt = DateTimeOffset.UtcNow.AddDays(-givenDaysAgo);
        var dose = Vaccination.Record(
            petId,
            vaccineName,
            $"LOT-{Guid.NewGuid():N}"[..12],
            DateOnly.FromDateTime(givenAt.UtcDateTime).AddYears(2),
            givenAt,
            DateOnly.FromDateTime(givenAt.UtcDateTime),
            "stf_test",
            nextDueOn: nextDueOn);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<Vaccination>().Add(dose);
        await db.SaveChangesAsync(cancellationToken);

        return dose;
    }

    /// <summary>A plan of unbooked sessions at the given times.</summary>
    public static async Task<TreatmentPlan> PlanAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        string title,
        IReadOnlyList<DateTimeOffset> sessions,
        CancellationToken cancellationToken = default)
    {
        var plan = TreatmentPlan.Create(clinic.PetId, title, clinic.ServiceId, sessions, clinic.StaffId);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<TreatmentPlan>().Add(plan);
        await db.SaveChangesAsync(cancellationToken);

        return plan;
    }

    /// <summary>The evaluator's answer at an instant of the test's choosing.</summary>
    public static async Task<IReadOnlyList<AutomationOccurrence>> EvaluateAsync(
        ApiFixture fixture,
        string ruleId,
        DateTimeOffset now,
        DateTimeOffset? horizon = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var rule = await RuleAsync(scope, ruleId, cancellationToken);

        return await scope.ServiceProvider.GetRequiredService<AutomationEvaluator>()
            .EvaluateAsync(rule, now, horizon ?? now, cancellationToken);
    }

    /// <summary>A real run of one rule at an instant of the test's choosing, in a scope of its own.</summary>
    public static async Task<AutomationRunSummary> RunRuleAsync(
        ApiFixture fixture,
        string ruleId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var rule = await RuleAsync(scope, ruleId, cancellationToken);

        return await scope.ServiceProvider.GetRequiredService<RunAutomationsHandler>()
            .RunRuleAsync(rule, now, cancellationToken);
    }

    /// <summary>Every "already sent" row of one rule.</summary>
    public static async Task<IReadOnlyList<AutomationDispatch>> DispatchesAsync(
        ApiFixture fixture,
        string ruleId,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<AutomationDispatch>().AsNoTracking()
            .Where(item => item.RuleId == ruleId)
            .OrderBy(item => item.ClaimedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>The clinic's zone, looked up as the API does.</summary>
    public static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    /// <summary>An instant at a clinic-local day and time.</summary>
    public static DateTimeOffset ClinicTime(DateOnly day, int hour, int minute = 0)
    {
        var local = day.ToDateTime(new TimeOnly(hour, minute));
        return new DateTimeOffset(local, Bangkok.GetUtcOffset(local));
    }

    /// <summary>The clinic's today.</summary>
    public static DateOnly ClinicToday => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Bangkok).DateTime);

    private static async Task<AutomationRule> RuleAsync(IServiceScope scope, string ruleId, CancellationToken cancellationToken) =>
        await scope.ServiceProvider.GetRequiredService<IAutomationRuleRepository>().FindAsync(ruleId, cancellationToken)
            ?? throw new InvalidOperationException($"no rule {ruleId}");
}
