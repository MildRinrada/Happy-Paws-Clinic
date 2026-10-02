using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.Scheduling;
using HappyPaws.Domain.ServiceCatalog;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// One self-contained clinic: a branch, a bookable service, a vet on shift, a customer, and a pet
/// the customer cares for.
/// </summary>
public sealed record SeededClinic(
    string BranchId,
    string ServiceId,
    string StaffId,
    string StaffUserId,
    string CustomerId,
    string CustomerUserId,
    string PetId,
    DateTimeOffset ShiftStartsAt,
    DateTimeOffset ShiftEndsAt);

/// <summary>
/// Builds the minimum clinic a booking test needs, through the production factories.
/// <para>
/// Each call creates its own branch, service and patient. The suite shares one PostgreSQL
/// container, so tests that shared a branch would collide on the queue's per-branch daily number
/// and on each other's calendars — and would pass or fail depending on execution order.
/// </para>
/// <para>
/// The shift deliberately spans from an hour ago to nearly a day ahead. That leaves room both for
/// slots the booking lead time must withhold and for slots it must offer, without a test having to
/// reason about what time of day it happens to be running.
/// </para>
/// </summary>
public static class ClinicSeed
{
    public static async Task<SeededClinic> ClinicAsync(
        ApiFixture fixture,
        int serviceDurationMinutes = 30,
        bool onlineBookingEnabled = true,
        CancellationToken cancellationToken = default)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];

        var branch = Branch.Create($"br-{suffix}", "สาขาทดสอบ", examRooms: ["room_1", "room_2"]);
        var service = ClinicService.Create(
            $"svc-{suffix}",
            "ตรวจสุขภาพ",
            "checkup",
            serviceDurationMinutes,
            priceSatang: 50_000,
            onlineBookingEnabled);

        var customer = Customer.Create("คุณลูกค้า ทดสอบ", $"08{Random.Shared.Next(10_000_000, 99_999_999)}");
        var pet = Pet.Create($"HN-{suffix}", "ลูน่า", "สุนัข", customer.Id, "ชิห์สุ");
        var ownership = PetOwnership.Create(pet.Id, customer.Id, isPrimary: true, mayViewHistory: true);

        // Seeded through the identity path so the staff membership, role and account are the same
        // objects a real sign-in would produce.
        var staff = await IdentitySeed.StaffAsync(
            fixture,
            [Domain.Access.Scopes.Appointment.Read, Domain.Access.Scopes.Appointment.Write],
            withTwoFactor: false,
            branches: [branch.Id],
            cancellationToken: cancellationToken);

        // Floored to the hour on purpose. The availability grid is anchored at the shift start, so
        // a shift beginning at 06:37:12.345 would offer slots at 06:37:12.345, 07:07:12.345 … and
        // no whole-hour time a test asked for would ever appear in the feed.
        var shiftStart = FlooredHour(DateTimeOffset.UtcNow.AddHours(-1));
        var shiftEnd = shiftStart.AddHours(24);
        var shift = StaffShift.Create(staff.StaffId, branch.Id, shiftStart, shiftEnd);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        db.Set<Branch>().Add(branch);
        db.Set<ClinicService>().Add(service);
        db.Set<Customer>().Add(customer);
        db.Set<Pet>().Add(pet);
        db.Set<PetOwnership>().Add(ownership);
        db.Set<StaffShift>().Add(shift);

        await db.SaveChangesAsync(cancellationToken);

        return new SeededClinic(
            branch.Id,
            service.Id,
            staff.StaffId,
            staff.UserId,
            customer.Id,
            $"usr_portal_{suffix}",
            pet.Id,
            shiftStart,
            shiftEnd);
    }

    /// <summary>Adds a second pet under a different customer, for the "not your pet" cases.</summary>
    public static async Task<(string CustomerId, string PetId)> OtherOwnerAsync(
        ApiFixture fixture,
        CancellationToken cancellationToken = default)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];

        var customer = Customer.Create("คุณคนอื่น", $"09{Random.Shared.Next(10_000_000, 99_999_999)}");
        var pet = Pet.Create($"HN-{suffix}", "มิโล", "แมว", customer.Id);
        var ownership = PetOwnership.Create(pet.Id, customer.Id, isPrimary: true);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        db.Set<Customer>().Add(customer);
        db.Set<Pet>().Add(pet);
        db.Set<PetOwnership>().Add(ownership);

        await db.SaveChangesAsync(cancellationToken);

        return (customer.Id, pet.Id);
    }

    /// <summary>Every appointment at a branch, for asserting on what actually landed.</summary>
    public static async Task<IReadOnlyList<Appointment>> AppointmentsAtAsync(
        ApiFixture fixture,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<Appointment>().AsNoTracking()
            .Where(appointment => appointment.BranchId == branchId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Reads an appointment back outside the request's change tracker.</summary>
    public static async Task<Appointment?> ReloadAppointmentAsync(
        ApiFixture fixture,
        string appointmentId,
        CancellationToken cancellationToken = default)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<Appointment>().AsNoTracking()
            .FirstOrDefaultAsync(appointment => appointment.Id == appointmentId, cancellationToken);
    }

    /// <summary>
    /// A whole-hour start time inside the seeded shift and beyond the two-hour booking lead time.
    /// Callers pass different offsets so that two bookings in one test do not land on each other.
    /// </summary>
    public static DateTimeOffset SlotAt(int hoursFromNow) =>
        FlooredHour(DateTimeOffset.UtcNow.AddHours(hoursFromNow));

    /// <summary>
    /// The clinic-local calendar day an instant falls on, formatted for <c>?date=</c>.
    /// <para>
    /// Tests must ask for the day the clinic thinks it is, not the day UTC thinks it is. Bangkok
    /// runs seven hours ahead, so for seven hours out of every twenty-four the two disagree — and
    /// a test that used the UTC date would pass all morning and fail every evening.
    /// </para>
    /// </summary>
    public static string ClinicDate(DateTimeOffset instant) =>
        TimeZoneInfo.ConvertTime(instant, ClinicTimeZone).ToString("yyyy-MM-dd");

    /// <summary>The same zone the API is configured with, looked up the same way.</summary>
    private static readonly TimeZoneInfo ClinicTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    private static DateTimeOffset FlooredHour(DateTimeOffset instant) =>
        new(instant.Year, instant.Month, instant.Day, instant.Hour, 0, 0, instant.Offset);
}
