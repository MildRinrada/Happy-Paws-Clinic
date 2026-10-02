using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Patients.Documents;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Portal;

/// <summary>Builds the owner's view of a pet, for the list, the update and the portal home.</summary>
public sealed class MyPetReader(
    IPetTimelineReader timeline,
    IPetAllergyRepository allergies,
    IPetDocumentRepository documents,
    IPetAccessPolicy access,
    IFileStorage storage)
{
    public async Task<MyPetDto> ReadAsync(Pet pet, string customerId, CancellationToken cancellationToken = default)
    {
        var weight = await timeline.LatestWeightAsync(pet.Id, cancellationToken);
        var known = await allergies.ActiveForPetAsync(pet.Id, cancellationToken);
        var mayViewHistory = await access.MayViewHistoryAsync(pet.Id, customerId, cancellationToken);

        string? photoUrl = null;

        if (pet.PhotoDocumentId is { } photoId
            && await documents.FindByIdAsync(photoId, cancellationToken) is { IsUploaded: true } photo)
        {
            photoUrl = storage.CreateDownloadUrl(photo.StorageKey, PetDocumentDto.DownloadLinkLifetime);
        }

        return new MyPetDto(
            pet.Id,
            pet.Version,
            pet.Name,
            pet.Nickname,
            pet.Species,
            pet.Breed,
            pet.HospitalNumber,
            pet.MicrochipNumber,
            photoUrl,
            pet.Diet,
            pet.Behaviour,
            pet.IsDeceased,
            weight,
            [.. known.Select(allergy => new MyPetAllergyDto(allergy.Substance, allergy.Severity.ToString().ToLowerInvariant()))],
            mayViewHistory);
    }
}
