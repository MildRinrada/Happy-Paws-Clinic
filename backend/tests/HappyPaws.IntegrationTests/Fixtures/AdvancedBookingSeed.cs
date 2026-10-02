using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Scheduling;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>Rows the advanced booking tests need that no endpoint makes on demand.</summary>
public static class AdvancedBookingSeed
{
    /// <summary>A booked visit that has already started — the only kind that can be marked a no-show.</summary>
    public static async Task<string> PastAppointmentAsync(ApiFixture fixture, SeededClinic clinic, CancellationToken cancellationToken)
    {
        var startsAt = DateTimeOffset.UtcNow.AddHours(-2);
        var appointment = Appointment.Book(
            clinic.BranchId, clinic.CustomerId, clinic.PetId, clinic.ServiceId, clinic.StaffId, startsAt, startsAt.AddMinutes(30));

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        db.Set<Appointment>().Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        return appointment.Id;
    }

    /// <summary>The seeded vet's shift again a day later, so a course can run into tomorrow.</summary>
    public static async Task NextDayShiftAsync(ApiFixture fixture, SeededClinic clinic, CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        db.Set<StaffShift>().Add(StaffShift.Create(
            clinic.StaffId, clinic.BranchId, clinic.ShiftStartsAt.AddHours(24), clinic.ShiftEndsAt.AddHours(24)));

        await db.SaveChangesAsync(cancellationToken);
    }
}
