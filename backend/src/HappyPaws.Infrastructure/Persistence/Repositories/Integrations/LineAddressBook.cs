using HappyPaws.Application.Integrations;
using HappyPaws.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Integrations;

/// <summary>Finds a live customer by the LINE address on their record. Unique by index, so at most one.</summary>
internal sealed class LineAddressBook(HappyPawsDbContext db) : ILineAddressBook
{
    public Task<Customer?> FindTrackedAsync(string lineUserId, CancellationToken cancellationToken = default) =>
        db.Set<Customer>()
            .FirstOrDefaultAsync(
                customer => customer.LineUserId == lineUserId && customer.MergedIntoCustomerId == null,
                cancellationToken);
}
