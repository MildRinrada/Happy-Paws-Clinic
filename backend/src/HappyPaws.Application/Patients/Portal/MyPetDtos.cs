using HappyPaws.Application.Patients.Dtos;

namespace HappyPaws.Application.Patients.Portal;

/// <summary>
/// A pet as its owner sees it in the portal — <c>GET /portal/pets</c>, "สัตว์ทั้งหมดของบัญชีนี้".
/// <para>
/// The identity, the latest weight and the allergies are the clinic's and read-only here. The
/// nickname, photo, diet and behaviour are the owner's and changeable through
/// <c>PATCH /portal/pets/{id}</c>. <c>may_view_history</c> says whether the records page will open
/// for this caller, so a client can hide a link that would only answer <c>404</c>.
/// </para>
/// </summary>
public sealed record MyPetDto(
    string Id,
    uint Version,
    string Name,
    string? Nickname,
    string Species,
    string? Breed,
    string HospitalNumber,
    string? MicrochipNumber,
    string? PhotoUrl,
    string? Diet,
    string? Behaviour,
    bool IsDeceased,
    WeightDto? LatestWeight,
    IReadOnlyList<MyPetAllergyDto> Allergies,
    bool MayViewHistory);

/// <summary>An allergy, as the owner needs to know it: what, and how bad.</summary>
public sealed record MyPetAllergyDto(string Substance, string Severity);
