using HappyPaws.Domain.Patients;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Patient states the API cannot yet reach.
/// <para>
/// Withdrawing an allergy is a real operation on the aggregate with no endpoint behind it — the
/// rest of the Tier-2 patient module is not built. Driving it here is what lets the prescription
/// check be tested against a withdrawn allergy rather than only against an active one.
/// </para>
/// </summary>
public static class PatientSeed
{
    public static async Task WithdrawAllergiesAsync(
        ApiFixture fixture,
        string petId,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var allergies = await db.Set<PetAllergy>()
            .Where(allergy => allergy.PetId == petId && allergy.WithdrawnAt == null)
            .ToListAsync(cancellationToken);

        foreach (var allergy in allergies)
        {
            allergy.Withdraw(DateTimeOffset.UtcNow, "บันทึกผิดคน");
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Reads a patient back outside the request's change tracker.</summary>
    public static async Task<Pet?> ReloadPetAsync(
        ApiFixture fixture,
        string petId,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<Pet>().AsNoTracking()
            .FirstOrDefaultAsync(pet => pet.Id == petId, cancellationToken);
    }
}
