using HappyPaws.Application.Appointments;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Crm;

/// <summary>The branch a CRM write belongs to, resolved the way a counter bill resolves it.</summary>
internal static class CrmBranches
{
    /// <summary>The branch asked for, or the clinic's only open branch; several branches and none named is refused.</summary>
    public static async Task<string> ResolveAsync(IBookingCatalog catalog, string? branchId, CancellationToken cancellationToken)
    {
        if (branchId is { Length: > 0 })
        {
            var branch = await catalog.FindBranchAsync(branchId, cancellationToken)
                ?? throw new NotFoundException("branch", branchId);

            return branch.Id;
        }

        var sole = await catalog.SoleActiveBranchAsync(cancellationToken);

        return sole?.Id ?? throw new ValidationFailedException(
            new Dictionary<string, string[]> { ["branch_id"] = ["คลินิกมีหลายสาขา ต้องระบุสาขา"] });
    }

    /// <summary>An optional branch: checked to exist when given.</summary>
    public static async Task<string?> ResolveOptionalAsync(IBookingCatalog catalog, string? branchId, CancellationToken cancellationToken)
    {
        if (branchId is not { Length: > 0 })
        {
            return null;
        }

        var branch = await catalog.FindBranchAsync(branchId, cancellationToken)
            ?? throw new NotFoundException("branch", branchId);

        return branch.Id;
    }
}

/// <summary>Checks that listed catalogue services exist, naming the field when one does not.</summary>
internal static class CrmServices
{
    public static async Task EnsureExistAsync(
        IBookingCatalog catalog,
        IReadOnlyList<string> serviceIds,
        string field,
        CancellationToken cancellationToken)
    {
        foreach (var serviceId in serviceIds)
        {
            if (await catalog.FindServiceAsync(serviceId, cancellationToken) is null)
            {
                throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    [field] = [$"ไม่พบบริการ {serviceId}"],
                });
            }
        }
    }
}
