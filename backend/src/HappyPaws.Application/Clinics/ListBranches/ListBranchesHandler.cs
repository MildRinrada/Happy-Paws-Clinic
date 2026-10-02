using HappyPaws.Application.Clinics.Dtos;

namespace HappyPaws.Application.Clinics.ListBranches;

/// <summary>
/// <c>GET /clinic/branches</c> — "สาขา ที่อยู่ พิกัด ห้องตรวจ".
/// <para>
/// Public, and returns open branches only. A closed site is not something a customer can book at
/// or drive to, and listing it would be the clinic's own website sending someone to a locked door.
/// </para>
/// </summary>
public sealed class ListBranchesHandler(IBranchRepository branches)
{
    public async Task<IReadOnlyList<BranchDto>> HandleAsync(CancellationToken cancellationToken = default) =>
        [.. (await branches.ListActiveAsync(cancellationToken)).Select(BranchDto.From)];
}
