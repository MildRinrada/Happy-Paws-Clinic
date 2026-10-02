using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Patients.Dtos;

namespace HappyPaws.Application.Patients.ListPets;

/// <summary>
/// <c>GET /staff/pets</c> — "ทะเบียนคนไข้ทั้งคลินิก".
/// </summary>
public sealed record ListPetsQuery(
    string? Q = null,
    string? CustomerId = null,
    string? Species = null,
    bool IncludeDeceased = false,
    int Limit = CursorPaging.DefaultLimit,
    string? Cursor = null);

/// <summary>
/// The patient register.
/// <para>
/// Closed records are excluded unless asked for. "ทะเบียนคนไข้" is the list of animals the clinic
/// currently treats, and a register that grows forever with every patient who ever lived stops
/// being usable at a counter within a few years. They remain reachable by id and by
/// <c>?include_deceased=true</c> — nothing is hidden, only defaulted away from.
/// </para>
/// </summary>
public sealed class ListPetsHandler(IPetRepository pets)
{
    public async Task<CursorPage<PetSummaryDto>> HandleAsync(
        ListPetsQuery query,
        CancellationToken cancellationToken = default)
    {
        var filter = new PetListFilter(query.Q, query.CustomerId, query.Species, query.IncludeDeceased);

        var page = await pets.ListAsync(
            filter, new CursorPageRequest(query.Limit, query.Cursor), cancellationToken);

        return new CursorPage<PetSummaryDto>([.. page.Data.Select(PetSummaryDto.From)], page.NextCursor);
    }
}
