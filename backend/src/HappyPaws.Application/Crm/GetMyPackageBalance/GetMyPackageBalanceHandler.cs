using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm.GetMyPackageBalance;

/// <summary>Persistence port for customers' packages.</summary>
public interface ICustomerPackageRepository
{
    void Add(CustomerPackage package);

    /// <summary>Every package a customer has bought, newest first.</summary>
    Task<IReadOnlyList<CustomerPackage>> ForCustomerAsync(string customerId, CancellationToken cancellationToken = default);
}

/// <summary>
/// One package and what is left of it. <c>status</c> is <c>active</c>, <c>used_up</c> or
/// <c>expired</c>.
/// </summary>
public sealed record PackageBalanceDto(
    string Id,
    string Name,
    string? PetId,
    IReadOnlyList<string> ServiceIds,
    int TotalUses,
    int UsedCount,
    int Remaining,
    DateTimeOffset PurchasedAt,
    DateOnly? ExpiresOn,
    string Status);

/// <summary>
/// <c>GET /portal/packages</c> — "สิทธิ์คงเหลือของลูกค้าและวันหมดอายุ".
/// <para>
/// Usable packages first, soonest to expire first, then the finished ones. The list is empty until
/// the clinic sells packages through <c>POST /staff/packages</c>, which is Tier 3 and not built.
/// </para>
/// </summary>
public sealed class GetMyPackageBalanceHandler(
    ICustomerPackageRepository packages,
    ICurrentUser currentUser,
    ClinicDays clinicDays)
{
    public async Task<IReadOnlyList<PackageBalanceDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();
        var today = clinicDays.Today;

        var owned = await packages.ForCustomerAsync(customerId, cancellationToken);

        return
        [
            .. owned
                .Select(package => Balance(package, today))
                .OrderBy(balance => balance.Status == "active" ? 0 : 1)
                .ThenBy(balance => balance.ExpiresOn ?? DateOnly.MaxValue),
        ];
    }

    private static PackageBalanceDto Balance(CustomerPackage package, DateOnly today) =>
        new(
            package.Id,
            package.Name,
            package.PetId,
            package.ServiceIds,
            package.TotalUses,
            package.UsedCount,
            Math.Max(package.Remaining, 0),
            package.PurchasedAt,
            package.ExpiresOn,
            package.IsExpiredOn(today) ? "expired" : package.Remaining <= 0 ? "used_up" : "active");
}
