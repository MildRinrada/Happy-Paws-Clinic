using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Patients.Dtos;

namespace HappyPaws.Application.Patients.GetPet;

/// <summary>
/// <c>GET /staff/pets/{id}</c> — "สายพันธุ์ น้ำหนักล่าสุด แพ้ยา ไมโครชิป".
/// </summary>
public sealed record GetPetQuery(string PetId);

/// <summary>
/// One patient, with the four things the contract names and the carer list that governs them.
/// <para>
/// The allergies are here for a reason worth stating: they are described elsewhere as
/// "แสดงเป็นแถบเตือนแดงทุกหน้าที่เกี่ยวข้อง" — a red banner on every relevant screen — and a banner
/// that needs a second request to render is a banner that sometimes renders late. It ships with the
/// patient or it is not a warning.
/// </para>
/// <para>
/// Not audited. This is the patient's identity card, not their file: species, breed, chip number,
/// current weight. Reading the clinical history <i>is</i> audited, by
/// <c>GET /staff/pets/{id}/history</c>, which is where the line falls.
/// </para>
/// </summary>
public sealed class GetPetHandler(
    IPetRepository pets,
    IPetAllergyRepository allergies,
    IPetOwnershipRepository ownerships,
    IPetTimelineReader timeline)
{
    public async Task<PetDetailDto> HandleAsync(
        GetPetQuery query,
        CancellationToken cancellationToken = default)
    {
        var pet = await pets.FindByIdAsync(query.PetId, cancellationToken)
            ?? throw new NotFoundException("pet", query.PetId);

        var recorded = await allergies.ActiveForPetAsync(pet.Id, cancellationToken);
        var carers = await ownerships.ForPetAsync(pet.Id, cancellationToken);
        var weight = await timeline.LatestWeightAsync(pet.Id, cancellationToken);

        return new PetDetailDto(
            PetSummaryDto.From(pet),
            weight,
            [.. recorded.Select(AllergyDto.From)],
            [.. carers.Select(CarerDto.From)]);
    }
}
