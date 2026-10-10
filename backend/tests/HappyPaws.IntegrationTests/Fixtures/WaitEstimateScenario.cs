using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Queueing;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Tickets and closures for the walk-in wait estimate (<c>docs/contract-extensions.md § 37</c>),
/// written straight to the database at one seeded branch so the counts are exactly what a test put
/// there.
/// </summary>
public static class WaitEstimateScenario
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    /// <summary>The clinic's today, as the API judges it.</summary>
    public static DateOnly ClinicToday =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Bangkok).DateTime);

    /// <summary>Puts <paramref name="count"/> walk-ins in today's queue, still waiting to be called.</summary>
    public static async Task<IReadOnlyList<QueueTicket>> WaitingAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        int count,
        int firstSequence = 1,
        CancellationToken cancellationToken = default)
    {
        var tickets = Enumerable.Range(firstSequence, count)
            .Select(sequence => QueueTicket.IssueWalkIn(
                clinic.BranchId,
                clinic.PetId,
                clinic.CustomerId,
                clinic.ServiceId,
                sequence,
                ClinicToday,
                DateTimeOffset.UtcNow))
            .ToList();

        await SaveAsync(fixture, db => db.Set<QueueTicket>().AddRange(tickets), cancellationToken);

        return tickets;
    }

    /// <summary>
    /// Puts served tickets in today's queue, each <paramref name="minutesInRoom"/> minutes in the
    /// room and served within the last few hours.
    /// </summary>
    public static async Task ServedAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        int count,
        int minutesInRoom,
        int firstSequence,
        CancellationToken cancellationToken = default)
    {
        var tickets = new List<QueueTicket>();

        for (var i = 0; i < count; i++)
        {
            var served = DateTimeOffset.UtcNow.AddMinutes(-10 * (i + 1));
            var entered = served.AddMinutes(-minutesInRoom);
            var ticket = QueueTicket.IssueWalkIn(
                clinic.BranchId,
                clinic.PetId,
                clinic.CustomerId,
                clinic.ServiceId,
                firstSequence + i,
                ClinicToday,
                entered.AddMinutes(-5));

            ticket.Call("stf_seed", entered.AddMinutes(-1));
            ticket.EnterRoom(entered);
            ticket.MarkServed(served);
            tickets.Add(ticket);
        }

        await SaveAsync(fixture, db => db.Set<QueueTicket>().AddRange(tickets), cancellationToken);
    }

    /// <summary>Shuts the seeded branch for today only.</summary>
    public static Task HolidayTodayAsync(ApiFixture fixture, SeededClinic clinic, CancellationToken cancellationToken = default) =>
        SaveAsync(
            fixture,
            db => db.Set<ClinicHoliday>().Add(ClinicHoliday.Declare(ClinicToday, "ปิดทำความสะอาด", clinic.BranchId)),
            cancellationToken);

    public static string Path(SeededClinic clinic) =>
        $"/queue/wait-estimate?branch={clinic.BranchId}";

    private static async Task SaveAsync(ApiFixture fixture, Action<HappyPawsDbContext> add, CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        add(db);

        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// A deployment that answers one address twice a minute on the wait estimate, in its own host: every
/// test client is the same address, and the shared fixture lifts the cap for that reason.
/// </summary>
public sealed class WaitEstimateRateLimitFixture : ApiFixture
{
    protected override IReadOnlyDictionary<string, string?> Settings =>
        new Dictionary<string, string?> { ["RateLimits:WaitEstimatePerIpPerMinute"] = "2" };
}

[CollectionDefinition(nameof(WaitEstimateRateLimitCollection))]
public sealed class WaitEstimateRateLimitCollection : ICollectionFixture<WaitEstimateRateLimitFixture>;
