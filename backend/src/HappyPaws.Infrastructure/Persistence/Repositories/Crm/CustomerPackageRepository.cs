using HappyPaws.Application.Crm;
using HappyPaws.Domain.Crm;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Crm;

internal sealed class CustomerPackageRepository(HappyPawsDbContext db) : ICustomerPackageRepository
{
    public void Add(CustomerPackage package) => db.Set<CustomerPackage>().Add(package);

    public void AddUse(PackageUse use) => db.Set<PackageUse>().Add(use);

    public async Task<IReadOnlyList<CustomerPackage>> ForCustomerAsync(string customerId, CancellationToken cancellationToken = default) =>
        await db.Set<CustomerPackage>()
            .AsNoTracking()
            .Where(package => package.CustomerId == customerId)
            .OrderByDescending(package => package.PurchasedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerPackage>> SpendableForCustomerTrackedAsync(
        string customerId,
        CancellationToken cancellationToken = default) =>
        await db.Set<CustomerPackage>()
            .Where(package => package.CustomerId == customerId
                && package.ActivatedAt != null
                && package.CancelledAt == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerPackage>> SoldOnInvoiceTrackedAsync(
        string invoiceId,
        CancellationToken cancellationToken = default) =>
        await db.Set<CustomerPackage>()
            .Where(package => package.InvoiceId == invoiceId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PackageUse>> OpenUsesForInvoiceTrackedAsync(
        string invoiceId,
        CancellationToken cancellationToken = default) =>
        await db.Set<PackageUse>()
            .Where(use => use.InvoiceId == invoiceId && use.ReleasedAt == null)
            .ToListAsync(cancellationToken);

    public Task<CustomerPackage?> FindTrackedAsync(string packageId, CancellationToken cancellationToken = default) =>
        db.Set<CustomerPackage>().FirstOrDefaultAsync(package => package.Id == packageId, cancellationToken);
}
