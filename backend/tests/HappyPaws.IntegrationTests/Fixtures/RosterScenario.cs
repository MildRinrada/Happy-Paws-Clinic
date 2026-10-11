using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Scheduling;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// The request-shaping the roster edit tests share (<c>docs/contract-extensions.md § 47</c> to
/// <c>§ 49</c>): clinic-local times, Mondays, shifts written through the API, and the stored rows
/// read back behind it.
/// </summary>
public static class RosterScenario
{
    public static readonly TimeSpan Bangkok = TimeSpan.FromHours(7);

    /// <summary>A scheduler at the clinic's branch, able to read and write the roster and book.</summary>
    public static HttpClient Scheduler(this ApiFixture fixture, SeededClinic clinic) =>
        fixture.StaffOf(
            clinic,
            Scopes.Schedule.Read,
            Scopes.Schedule.Write,
            Scopes.Appointment.Read,
            Scopes.Appointment.Write);

    /// <summary>Today's clinic day.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(Bangkok).DateTime);

    /// <summary>The Monday of this week, plus whole weeks.</summary>
    public static DateOnly MondayWeeksAhead(int weeks)
    {
        var today = Today;

        return today.AddDays(-(((int)today.DayOfWeek + 6) % 7) + (7 * weeks));
    }

    /// <summary>A whole hour on a clinic day, Bangkok time.</summary>
    public static DateTimeOffset At(DateOnly day, int hour, int minute = 0) =>
        new(day.Year, day.Month, day.Day, hour, minute, 0, Bangkok);

    public static string Day(DateOnly day) => day.ToString("yyyy-MM-dd");

    public static async Task<JsonElement> BodyAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

    /// <summary>Creates shifts through <c>POST /staff/schedules/shifts</c> and answers the blocks made.</summary>
    public static async Task<List<JsonElement>> CreateShiftsAsync(
        HttpClient client,
        object body,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync("/staff/schedules/shifts", body, cancellationToken);
        response.EnsureSuccessStatusCode();

        return [.. (await BodyAsync(response, cancellationToken)).GetProperty("data").EnumerateArray()];
    }

    /// <summary>A <c>PATCH</c> or <c>DELETE</c> on one shift, with <c>If-Match</c> when a version is given.</summary>
    public static Task<HttpResponseMessage> SendShiftAsync(
        HttpClient client,
        HttpMethod method,
        string shiftId,
        CancellationToken cancellationToken,
        object? body = null,
        uint? version = null)
    {
        var request = new HttpRequestMessage(method, $"/staff/schedules/shifts/{shiftId}");

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (version is { } value)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{value}\"");
        }

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>One person's shifts at the branch on the calendar for a span of days.</summary>
    public static async Task<List<JsonElement>> ShiftsOnCalendarAsync(
        HttpClient client,
        SeededClinic clinic,
        string staffId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var calendar = await client.GetFromJsonAsync<JsonElement>(
            $"/staff/schedules?from={Day(from)}&to={Day(to)}&branch={clinic.BranchId}&staff_id={staffId}", cancellationToken);

        return
        [
            .. calendar.GetProperty("staff").EnumerateArray()
                .Where(person => person.GetProperty("staff_id").GetString() == staffId)
                .SelectMany(person => person.GetProperty("shifts").EnumerateArray()),
        ];
    }

    /// <summary>Writes a shift straight to the table, for times the API would not take, such as one already over.</summary>
    public static async Task<StaffShift> StoreShiftAsync(
        ApiFixture fixture,
        string staffId,
        string branchId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CancellationToken cancellationToken)
    {
        var shift = StaffShift.Create(staffId, branchId, startsAt, endsAt);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<StaffShift>().Add(shift);
        await db.SaveChangesAsync(cancellationToken);

        return shift;
    }

    public static async Task<StaffShift?> StoredShiftAsync(ApiFixture fixture, string shiftId, CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<StaffShift>().AsNoTracking().FirstOrDefaultAsync(shift => shift.Id == shiftId, cancellationToken);
    }

    public static async Task<IReadOnlyList<AuditEntry>> AuditAsync(
        ApiFixture fixture,
        string action,
        string resourceId,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.Action == action && entry.ResourceId == resourceId)
            .ToListAsync(cancellationToken);
    }

    public static string ErrorCode(JsonElement body) => body.GetProperty("error").GetProperty("code").GetString()!;
}
