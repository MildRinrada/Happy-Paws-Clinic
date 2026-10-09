using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Crm.GetMyPackageBalance;

/// <summary>
/// <c>GET /portal/packages</c> — "สิทธิ์คงเหลือของลูกค้าและวันหมดอายุ".
/// <para>
/// The same <see cref="PackageDto"/> the staff see on <c>GET /staff/loyalty/{customer_id}</c>, so
/// the owner and the counter can never read a different balance. Usable packages first, then those
/// waiting for payment, then the finished ones; soonest to expire first within each.
/// </para>
/// </summary>
public sealed class GetMyPackageBalanceHandler(
    ICustomerPackageRepository packages,
    ICurrentUser currentUser,
    ClinicDays clinicDays)
{
    public async Task<IReadOnlyList<PackageDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.RequireCustomerId();

        var owned = await packages.ForCustomerAsync(customerId, cancellationToken);

        return PackageDto.Ordered(owned, clinicDays.Today);
    }
}
