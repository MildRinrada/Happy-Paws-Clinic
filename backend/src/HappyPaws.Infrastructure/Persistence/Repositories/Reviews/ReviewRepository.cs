using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Reviews;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.Reviews;
using HappyPaws.Infrastructure.Persistence.Repositories.Crm;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Reviews;

/// <summary>Customer reviews (<c>docs/contract-extensions.md § 36</c>).</summary>
internal sealed class ReviewRepository(HappyPawsDbContext db) : IReviewRepository
{
    public void Add(CustomerReview review) => db.Set<CustomerReview>().Add(review);

    public Task<CustomerReview?> FindAsync(string id, CancellationToken cancellationToken = default) =>
        db.Set<CustomerReview>().FirstOrDefaultAsync(review => review.Id == id, cancellationToken);

    public Task<StaffReviewRow?> FindRowAsync(string id, CancellationToken cancellationToken = default) =>
        Rows(db.Set<CustomerReview>().AsNoTracking().Where(review => review.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public async Task<CursorPage<StaffReviewRow>> ListForStaffAsync(
        ReviewStatus? status,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var reviews = db.Set<CustomerReview>().AsNoTracking();

        if (status is { } wanted)
        {
            reviews = reviews.Where(review => review.Status == wanted);
        }

        var rows = await Rows(After(reviews, page))
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        return NewestFirst.Page(rows, page, row => row.Review.CreatedAt, row => row.Review.Id);
    }

    public async Task<CursorPage<CustomerReview>> ListPublishedAsync(CursorPageRequest page, CancellationToken cancellationToken = default)
    {
        var reviews = db.Set<CustomerReview>().AsNoTracking().Where(review => review.Status == ReviewStatus.Published);

        var rows = await After(reviews, page)
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        return NewestFirst.Page(rows, page, review => review.CreatedAt, review => review.Id);
    }

    public async Task<IReadOnlyDictionary<int, int>> PublishedCountsByRatingAsync(CancellationToken cancellationToken = default) =>
        await db.Set<CustomerReview>().AsNoTracking()
            .Where(review => review.Status == ReviewStatus.Published)
            .GroupBy(review => review.Rating)
            .Select(group => new { Rating = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Rating, group => group.Count, cancellationToken);

    /// <summary>Newest written first, after the cursor.</summary>
    private static IQueryable<CustomerReview> After(IQueryable<CustomerReview> reviews, CursorPageRequest page)
    {
        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            reviews = reviews.Where(review =>
                review.CreatedAt < sortKey || (review.CreatedAt == sortKey && string.Compare(review.Id, id) < 0));
        }

        return reviews.OrderByDescending(review => review.CreatedAt).ThenByDescending(review => review.Id);
    }

    /// <summary>Each review with the names the moderation list shows beside it.</summary>
    private IQueryable<StaffReviewRow> Rows(IQueryable<CustomerReview> reviews) =>
        from review in reviews
        join customer in db.Set<Customer>() on review.CustomerId equals customer.Id into customers
        from customer in customers.DefaultIfEmpty()
        join pet in db.Set<Pet>() on review.PetId equals pet.Id into pets
        from pet in pets.DefaultIfEmpty()
        join branch in db.Set<Branch>() on review.BranchId equals branch.Id into branches
        from branch in branches.DefaultIfEmpty()
        select new StaffReviewRow(review, customer.DisplayName, pet.Name, branch.Name);
}
