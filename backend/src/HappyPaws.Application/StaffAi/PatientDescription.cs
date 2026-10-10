using HappyPaws.Application.Common;
using HappyPaws.Application.StaffAi.PetSummary;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.StaffAi;

/// <summary>
/// The one line about the patient that the drafting prompts carry: name, species, breed, sex and
/// age. No hospital number, microchip or owner.
/// </summary>
public static class PatientDescription
{
    public static string Of(Pet pet, DateOnly today)
    {
        var parts = new List<string?>
        {
            pet.Name,
            pet.Species,
            pet.Breed,
            $"sex {WireEnum.Of(pet.Sex)}",
            pet.BirthDate is { } born ? $"age {PetSummaryAssembler.Age(born, today)}" : "age unknown",
        };

        return "Patient: " + string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
