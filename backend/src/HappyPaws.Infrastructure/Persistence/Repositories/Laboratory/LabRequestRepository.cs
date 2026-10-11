using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Laboratory;
using HappyPaws.Domain.Laboratory;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Laboratory;

internal sealed class LabRequestRepository(HappyPawsDbContext db) : ILabRequestRepository
{
    public void Add(LabRequest request) => db.Set<LabRequest>().Add(request);

    public Task<LabRequest?> FindTrackedByIdAsync(string labRequestId, CancellationToken cancellationToken = default) =>
        db.Set<LabRequest>()
            .Include(request => request.Results)
            .Include(request => request.Attachments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(request => request.Id == labRequestId, cancellationToken);

    public async Task<CursorPage<LabRequest>> ListAsync(
        LabRequestFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<LabRequest>().AsNoTracking()
            .Include(request => request.Results)
            .Include(request => request.Attachments)
            .AsQueryable();

        if (filter.BranchIds is { } visible)
        {
            query = query.Where(request => visible.Contains(request.BranchId));
        }

        if (filter.Status is { } status)
        {
            query = query.Where(request => request.Status == status);
        }

        if (filter.PetId is { Length: > 0 } petId)
        {
            query = query.Where(request => request.PetId == petId);
        }

        if (filter.RequestedByStaffId is { Length: > 0 } staffId)
        {
            query = query.Where(request => request.RequestedByStaffId == staffId);
        }

        if (filter.BranchId is { Length: > 0 } branchId)
        {
            query = query.Where(request => request.BranchId == branchId);
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);

            query = query.Where(request =>
                request.RequestedAt < sortKey
                || (request.RequestedAt == sortKey && string.Compare(request.Id, id) < 0));
        }

        var limit = page.SafeLimit;

        var rows = await query
            .OrderByDescending(request => request.RequestedAt).ThenByDescending(request => request.Id)
            .Take(limit + 1)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var data = hasMore ? rows[..limit] : rows;

        return new CursorPage<LabRequest>(
            data, hasMore ? Cursor.Encode(data[^1].RequestedAt, data[^1].Id) : null);
    }
}
