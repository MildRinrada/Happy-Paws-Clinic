using HappyPaws.Application.Customers;
using HappyPaws.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Customers;

/// <summary>
/// EF Core implementation of the consent port.
/// <para>
/// "Current" is the newest row per purpose, computed in the database rather than by reading the
/// whole history into memory — a customer of several years has a long history and this query runs
/// in front of every notification.
/// </para>
/// </summary>
internal sealed class ConsentRepository(HappyPawsDbContext db) : IConsentRepository
{
    public void Add(CustomerConsent consent) => db.Set<CustomerConsent>().Add(consent);

    public async Task<IReadOnlyList<CustomerConsent>> HistoryAsync(
        string customerId,
        CancellationToken cancellationToken = default) =>
        await db.Set<CustomerConsent>()
            .AsNoTracking()
            .Where(consent => consent.CustomerId == customerId)
            .OrderByDescending(consent => consent.RecordedAt)
            .ThenByDescending(consent => consent.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<ConsentPurpose, CustomerConsent?>> CurrentAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var latest = await db.Set<CustomerConsent>()
            .AsNoTracking()
            .Where(consent => consent.CustomerId == customerId)
            .GroupBy(consent => consent.Purpose)
            // Id breaks the tie: it is time-ordered, so two decisions recorded in the same
            // millisecond still resolve to the one written second.
            .Select(group => group
                .OrderByDescending(consent => consent.RecordedAt)
                .ThenByDescending(consent => consent.Id)
                .First())
            .ToListAsync(cancellationToken);

        return latest.ToDictionary(consent => consent.Purpose, consent => (CustomerConsent?)consent);
    }
}

/// <summary>
/// EF Core implementation of the data-request port. Oldest first: the top of this list is the
/// request closest to a statutory deadline.
/// </summary>
internal sealed class DataSubjectRequestRepository(HappyPawsDbContext db) : IDataSubjectRequestRepository
{
    public void Add(DataSubjectRequest request) => db.Set<DataSubjectRequest>().Add(request);

    public Task<DataSubjectRequest?> FindByIdAsync(
        string requestId,
        CancellationToken cancellationToken = default) =>
        db.Set<DataSubjectRequest>().FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken);

    public async Task<IReadOnlyList<DataSubjectRequest>> ListAsync(
        DataRequestStatus? status = null,
        string? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<DataSubjectRequest>().AsNoTracking().AsQueryable();

        if (status is { } value)
        {
            query = query.Where(request => request.Status == value);
        }

        if (customerId is { Length: > 0 })
        {
            query = query.Where(request => request.CustomerId == customerId);
        }

        return await query
            .OrderBy(request => request.RaisedAt)
            .ThenBy(request => request.Id)
            .ToListAsync(cancellationToken);
    }
}
