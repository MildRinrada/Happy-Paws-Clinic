using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Patients.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.ListVaccinations;

/// <summary><c>GET /staff/pets/{id}/vaccinations</c> — "รอบที่ฉีดแล้วและรอบถัดไปที่ครบกำหนด".</summary>
public sealed record ListVaccinationsQuery(string PetId);

/// <summary>
/// The doses a patient has had, and what is due next.
/// <para>
/// <b>The due list is one entry per vaccine, from its latest dose.</b> A dog vaccinated against
/// rabies five years running has five doses and one due date — the one the newest dose set. The
/// older doses' due dates have been met, and listing them would show four "overdue" entries for a
/// patient who is fully covered.
/// </para>
/// <para>
/// A patient marked deceased has no due list. "หยุดแจ้งเตือนทุกชนิด" means nothing about this
/// patient should prompt anyone to call the owner.
/// </para>
/// </summary>
public sealed class ListVaccinationsHandler(
    IPetRepository pets,
    IVaccinationRepository vaccinations,
    ClinicDays clinicDays)
{
    /// <summary>How close a due date has to be before the clinic should start reminding.</summary>
    public const int DueSoonDays = 30;

    public async Task<PetVaccinationsDto> HandleAsync(
        ListVaccinationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pet = await pets.FindByIdAsync(query.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", query.PetId);

        var doses = await vaccinations.ForPetAsync(pet.Id, cancellationToken);

        var due = pet.IsDeceased ? [] : DueFrom(doses, clinicDays.Today);

        return new PetVaccinationsDto(pet.Id, [.. doses.Select(VaccinationDto.From)], due);
    }

    /// <summary>The latest dose of each vaccine that still has a next dose, soonest first.</summary>
    public static IReadOnlyList<VaccinationDueDto> DueFrom(IEnumerable<Vaccination> doses, DateOnly today) =>
    [
        .. doses
            .GroupBy(dose => dose.VaccineName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.MaxBy(dose => dose.AdministeredAt)!)
            .Where(latest => latest.NextDueOn is not null)
            .OrderBy(latest => latest.NextDueOn)
            .Select(latest => new VaccinationDueDto(
                latest.VaccineName,
                latest.NextDueOn!.Value,
                StatusOf(latest.NextDueOn.Value, today),
                latest.Id,
                latest.AdministeredAt)),
    ];

    private static string StatusOf(DateOnly dueOn, DateOnly today) =>
        dueOn < today ? "overdue"
        : dueOn <= today.AddDays(DueSoonDays) ? "due_soon"
        : "upcoming";
}
