using System.Security.Cryptography;
using System.Text;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Billing;

/// <summary>
/// EF Core implementation of the invoice port.
/// <para>
/// Single-invoice loads pull all five children as a <b>split query</b>, for the same reason the
/// consultation does: joining five one-to-many collections multiplies the rows together, and a bill
/// with four lines, two payments and a receipt would come back as eight copies of itself.
/// </para>
/// <para>
/// Lists load the same children, read-only. Trimming them to lines and payments is tempting and
/// wrong: every total on the DTO is derived from the collections, so a list that omitted discounts
/// would quote each discounted bill at its undiscounted price — and it would do so consistently
/// enough to look right. Correct totals are not a detail a list gets to skip.
/// </para>
/// </summary>
internal sealed class InvoiceRepository(HappyPawsDbContext db) : IInvoiceRepository
{
    public void Add(Invoice invoice) => db.Set<Invoice>().Add(invoice);

    public Task<Invoice?> FindByIdAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        WithParts(db.Set<Invoice>()).FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken);

    public Task<Invoice?> FindByConsultationAsync(
        string consultationId,
        CancellationToken cancellationToken = default) =>
        WithParts(db.Set<Invoice>())
            .FirstOrDefaultAsync(invoice => invoice.ConsultationId == consultationId, cancellationToken);

    public async Task LockConsultationAsync(string consultationId, CancellationToken cancellationToken = default)
    {
        // Transaction-scoped, keyed on the case and salted so it cannot collide with the stock or
        // document-number locks that share PostgreSQL's one advisory-lock namespace.
        var key = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes($"case-billing:{consultationId}")), 0);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", [key], cancellationToken);
    }

    public void MarkChanged(Invoice invoice)
    {
        // A modified root is written with its xmin in the WHERE clause, which is the check wanted.
        var entry = db.Entry(invoice);

        if (entry.State == EntityState.Unchanged)
        {
            entry.State = EntityState.Modified;
        }
    }

    public Task<CursorPage<Invoice>> ListAsync(
        InvoiceListFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = ForList(db.Set<Invoice>());

        if (filter.VisibleBranchIds is { Count: > 0 } visible)
        {
            query = query.Where(invoice => visible.Contains(invoice.BranchId));
        }

        if (filter.BranchId is { Length: > 0 } branchId)
        {
            query = query.Where(invoice => invoice.BranchId == branchId);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(invoice => invoice.Status == status);
        }

        if (filter.CustomerId is { Length: > 0 } customerId)
        {
            query = query.Where(invoice => invoice.CustomerId == customerId);
        }

        if (filter.From is { } from)
        {
            query = query.Where(invoice => invoice.IssuedAt >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(invoice => invoice.IssuedAt < to);
        }

        return PageAsync(query, page, cancellationToken);
    }

    public Task<CursorPage<Invoice>> ListForCustomerAsync(
        string customerId,
        bool unpaidOnly,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = ForList(db.Set<Invoice>()).Where(invoice => invoice.CustomerId == customerId);

        if (unpaidOnly)
        {
            // "จ่ายค้างชำระ" — what the customer still owes. Void and refunded bills are settled
            // business whatever their arithmetic says, and a voided bill on a customer's
            // outstanding list is a demand for money the clinic already withdrew.
            query = query.Where(invoice =>
                invoice.Status == InvoiceStatus.Open || invoice.Status == InvoiceStatus.PartiallyPaid);
        }

        return PageAsync(query, page, cancellationToken);
    }

    /// <summary>
    /// "ยอดค้างชำระ" — summed in the database, over the two statuses that still mean money is owed.
    /// <para>
    /// Void and refunded bills are excluded whatever their arithmetic says. A voided bill on a
    /// customer's outstanding balance is a demand for money the clinic itself withdrew.
    /// </para>
    /// </summary>
    public async Task<Money> OutstandingForCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var owed = await db.Set<Invoice>()
            .AsNoTracking()
            .Where(invoice => invoice.CustomerId == customerId
                && (invoice.Status == InvoiceStatus.Open || invoice.Status == InvoiceStatus.PartiallyPaid))
            .Select(invoice => new
            {
                Lines = invoice.Lines.Sum(line => line.UnitPriceSatang * line.Quantity),
                Discounts = invoice.Discounts.Sum(discount => (long?)discount.AmountSatang) ?? 0,
                Payments = invoice.Payments.Sum(payment => (long?)payment.AmountSatang) ?? 0,
            })
            .ToListAsync(cancellationToken);

        return new Money(owed.Sum(invoice => invoice.Lines - invoice.Discounts - invoice.Payments));
    }

    /// <summary>Newest first, keyset on <c>(issued_at, id)</c> — the pair the list indexes carry.</summary>
    private static async Task<CursorPage<Invoice>> PageAsync(
        IQueryable<Invoice> query,
        CursorPageRequest page,
        CancellationToken cancellationToken)
    {
        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);

            query = query.Where(invoice =>
                invoice.IssuedAt < sortKey
                || (invoice.IssuedAt == sortKey && string.Compare(invoice.Id, id) < 0));
        }

        var limit = page.SafeLimit;

        var rows = await query
            .OrderByDescending(invoice => invoice.IssuedAt).ThenByDescending(invoice => invoice.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var data = hasMore ? rows[..limit] : rows;

        return new CursorPage<Invoice>(
            data,
            hasMore ? Cursor.Encode(data[^1].IssuedAt, data[^1].Id) : null);
    }

    private static IQueryable<Invoice> WithParts(IQueryable<Invoice> query) =>
        query
            .Include(invoice => invoice.Lines)
            .Include(invoice => invoice.Discounts)
            .Include(invoice => invoice.Payments)
            .Include(invoice => invoice.Refunds)
            .Include(invoice => invoice.Receipt)
            .AsSplitQuery();

    private static IQueryable<Invoice> ForList(IQueryable<Invoice> query) => WithParts(query.AsNoTracking());
}
