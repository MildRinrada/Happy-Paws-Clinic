using HappyPaws.Application.Staff.Dtos;

namespace HappyPaws.Application.Staff.ListStaffTypes;

/// <summary>
/// <c>GET /staff/staff-types</c> — "ประเภทพนักงานที่คลินิกนิยามเอง".
/// <para>
/// The contract calls these clinic-defined and gives no endpoint that defines one, so a staff type
/// is defined the only way left: by using it. Whatever position a clinic writes on an invitation
/// or a membership is a staff type from then on, and this lists them with how many people hold
/// each. It stays a label throughout — access is decided by scopes, never by this.
/// </para>
/// <para>
/// The four positions the contract's own role table names are always present, so the list on a
/// fresh instance is a useful set of suggestions rather than one entry reading "owner".
/// </para>
/// </summary>
public sealed class ListStaffTypesHandler(IStaffRepository staff)
{
    private static readonly string[] Suggested = ["veterinarian", "vet_assistant", "receptionist", "cashier"];

    public async Task<IReadOnlyList<StaffTypeDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var inUse = await staff.StaffTypeCountsAsync(cancellationToken);

        return
        [
            .. inUse.Keys
                .Union(Suggested, StringComparer.Ordinal)
                .OrderBy(type => type, StringComparer.Ordinal)
                .Select(type => new StaffTypeDto(type, inUse.GetValueOrDefault(type))),
        ];
    }
}
