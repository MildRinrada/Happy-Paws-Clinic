using HappyPaws.Application.Crm.GetMyPackageBalance;
using HappyPaws.Domain.Crm;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Crm;

internal sealed class CustomerPackageRepository(HappyPawsDbContext db) : ICustomerPackageRepository
{
    public void Add(CustomerPackage package) => db.Set<CustomerPackage>().Add(package);

    public async Task<IReadOnlyList<CustomerPackage>> ForCustomerAsync(string customerId, CancellationToken cancellationToken = default) =>
        await db.Set<CustomerPackage>()
            .AsNoTracking()
            .Where(package => package.CustomerId == customerId)
            .OrderByDescending(package => package.PurchasedAt)
            .ToListAsync(cancellationToken);
}
