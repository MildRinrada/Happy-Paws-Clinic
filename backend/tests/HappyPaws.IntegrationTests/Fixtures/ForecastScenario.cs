using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Inventory;
using HappyPaws.Domain.Queueing;
using HappyPaws.Domain.Scheduling;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// The history the forecasts of <c>docs/contract-extensions.md § 33</c> read, written straight to
/// the tables with past dates: no endpoint records last month's ledger, tickets or visits.
/// <para>
/// Every helper works at the branch of the <see cref="SeededClinic"/> it is given, and every
/// forecast test reads that branch only, so the shared database's other rows never reach an
/// assertion.
/// </para>
/// </summary>
public static class ForecastScenario
{
    /// <summary>An instant at a clinic-local day and time.</summary>
    public static DateTimeOffset At(DateOnly day, int hour, int minute = 0) => AutomationScenario.ClinicTime(day, hour, minute);

    public static DateOnly Today => AutomationScenario.ClinicToday;

    /// <summary>
    /// An item stocked at the branch <paramref name="receivedDaysAgo"/> days ago, then the movements
    /// given, in order, each a (days ago, kind, quantity) with quantity counted for an adjustment.
    /// </summary>
    public static async Task<string> StockHistoryAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        string name,
        int received,
        int receivedDaysAgo,
        IReadOnlyList<(int DaysAgo, StockMovementKind Kind, int Quantity)> movements,
        CancellationToken cancellationToken)
    {
        var item = InventoryItem.Create(name, InventoryItemKind.Medicine, "เม็ด", 1_000, 0, clinic.StaffId);
        var receivedAt = At(Today.AddDays(-receivedDaysAgo), 9);
        var lot = StockLot.Open(item.Id, clinic.BranchId, $"L-{Guid.NewGuid():N}"[..10], null, receivedAt);

        var rows = new List<StockMovement> { StockMovement.Receive(lot, received, clinic.StaffId, receivedAt) };

        foreach (var (daysAgo, kind, quantity) in movements)
        {
            var at = daysAgo == 0 ? DateTimeOffset.UtcNow : At(Today.AddDays(-daysAgo), 11);

            rows.Add(kind switch
            {
                StockMovementKind.Issue => StockMovement.Issue(lot, quantity, clinic.StaffId, at),
                StockMovementKind.Adjustment => StockMovement.Adjust(lot, quantity, "นับสต๊อก", clinic.StaffId, at),
                _ => throw new ArgumentOutOfRangeException(nameof(movements), kind, "seed only issues and counts"),
            });
        }

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        db.Set<InventoryItem>().Add(item);
        db.Set<StockLot>().Add(lot);
        db.Set<StockMovement>().AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);

        return item.Id;
    }

    /// <summary>A vet's shift at the branch on a clinic day, from one hour to another.</summary>
    public static async Task ShiftAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        DateOnly day,
        int fromHour,
        int toHour,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        db.Set<StaffShift>().Add(StaffShift.Create(clinic.StaffId, clinic.BranchId, At(day, fromHour), At(day, toHour)));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Walk-in tickets issued at the branch on a past day, at the hour given.</summary>
    public static async Task WalkInsAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        DateOnly day,
        int hour,
        int count,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        for (var i = 0; i < count; i++)
        {
            // Numbered past anything the day already holds: the number is unique per branch and day.
            var sequence = (hour * 100) + i + 1;

            db.Set<QueueTicket>().Add(QueueTicket.IssueWalkIn(
                clinic.BranchId, clinic.PetId, clinic.CustomerId, clinic.ServiceId, sequence, day, At(day, hour, 10 + i)));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>A booked appointment at the branch, changed by <paramref name="change"/> before it is saved.</summary>
    public static async Task<Appointment> AppointmentAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        DateTimeOffset startsAt,
        Action<Appointment>? change = null,
        string? customerId = null,
        string? petId = null,
        bool walkIn = false,
        CancellationToken cancellationToken = default)
    {
        var appointment = Appointment.Book(
            clinic.BranchId,
            customerId ?? clinic.CustomerId,
            petId ?? clinic.PetId,
            clinic.ServiceId,
            clinic.StaffId,
            startsAt,
            startsAt.AddMinutes(30),
            isWalkIn: walkIn);

        change?.Invoke(appointment);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<Appointment>().Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        return appointment;
    }

    /// <summary>
    /// Visits at the branch, each a consultation opened that many days ago. With
    /// <paramref name="billSameDay"/> a bill is raised on each day too, which must not count twice.
    /// </summary>
    public static async Task VisitsAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        string customerId,
        string petId,
        IReadOnlyList<int> daysAgo,
        CancellationToken cancellationToken,
        bool billSameDay = false)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        foreach (var ago in daysAgo)
        {
            var at = At(Today.AddDays(-ago), 10);
            db.Set<Consultation>().Add(Consultation.Open(clinic.BranchId, petId, customerId, clinic.StaffId, at));

            if (billSameDay)
            {
                db.Set<Invoice>().Add(Invoice.Raise($"T-{Guid.NewGuid():N}"[..20], clinic.BranchId, customerId, clinic.StaffId, at.AddHours(1), petId));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
