using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Customers;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.MedicalRecords;
using HappyPaws.Domain.Patients;
using HappyPaws.Domain.Queueing;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Customers;

/// <summary>EF Core implementation of the customer port.</summary>
internal sealed class CustomerRepository(HappyPawsDbContext db) : ICustomerRepository
{
    public void Add(Customer customer) => db.Set<Customer>().Add(customer);

    public Task<Customer?> FindByIdAsync(string customerId, CancellationToken cancellationToken = default) =>
        db.Set<Customer>().AsNoTracking()
            .FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);

    public Task<Customer?> FindTrackedByIdAsync(string customerId, CancellationToken cancellationToken = default) =>
        db.Set<Customer>().FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);

    public Task<Customer?> WithLineAddressAsync(string lineUserId, CancellationToken cancellationToken = default) =>
        db.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(customer => customer.LineUserId == lineUserId, cancellationToken);

    public async Task<IReadOnlyList<Customer>> WithPhoneAsync(
        string phone,
        CancellationToken cancellationToken = default) =>
        await db.Set<Customer>().AsNoTracking()
            .Where(customer => customer.Phone == phone && customer.MergedIntoCustomerId == null)
            .OrderBy(customer => customer.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Customer>> UnlinkedWithPhoneDigitsAsync(
        string phoneDigits,
        CancellationToken cancellationToken = default)
    {
        // Narrowed in SQL by the last four digits, which survive any punctuation the counter used,
        // then compared exactly as digits here.
        var tail = $"%{phoneDigits[^Math.Min(4, phoneDigits.Length)..]}";

        var candidates = await db.Set<Customer>()
            .Where(customer => customer.UserId == null
                && customer.MergedIntoCustomerId == null
                && EF.Functions.Like(customer.Phone, tail))
            .OrderBy(customer => customer.CreatedAt)
            .ToListAsync(cancellationToken);

        return [.. candidates.Where(customer => UserAccount.NormalisePhone(customer.Phone) == phoneDigits)];
    }

    /// <summary>
    /// "ค้นด้วยชื่อ เบอร์ HN หรือชื่อสัตว์" — one box, four things behind it.
    /// <para>
    /// The two patient columns are reached through the carer links rather than
    /// <c>primary_customer_id</c>, so searching a pet's name finds every person who cares for it,
    /// not only the one it is billed to. A co-owner standing at the counter saying "I'm here with
    /// ลูน่า" is exactly the case that would otherwise return nothing.
    /// </para>
    /// </summary>
    public async Task<CursorPage<Customer>> SearchAsync(
        string? query,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        // Merged records never appear. They exist so an old id still resolves, not so a
        // receptionist can pick one by mistake.
        var customers = db.Set<Customer>().AsNoTracking()
            .Where(customer => customer.MergedIntoCustomerId == null);

        if (query is { Length: > 0 } text)
        {
            var pattern = $"%{text}%";

            customers = customers.Where(customer =>
                EF.Functions.ILike(customer.DisplayName, pattern)
                || EF.Functions.ILike(customer.Phone, pattern)
                || db.Set<PetOwnership>()
                    .Where(link => link.CustomerId == customer.Id && link.RevokedAt == null)
                    .Join(db.Set<Pet>(), link => link.PetId, pet => pet.Id, (_, pet) => pet)
                    .Any(pet =>
                        EF.Functions.ILike(pet.Name, pattern)
                        || EF.Functions.ILike(pet.HospitalNumber, pattern)));
        }

        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);

            customers = customers.Where(customer =>
                customer.CreatedAt < sortKey
                || (customer.CreatedAt == sortKey && string.Compare(customer.Id, id) < 0));
        }

        var limit = page.SafeLimit;

        var rows = await customers
            .OrderByDescending(customer => customer.CreatedAt).ThenByDescending(customer => customer.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var data = hasMore ? rows[..limit] : rows;

        return new CursorPage<Customer>(
            data, hasMore ? Cursor.Encode(data[^1].CreatedAt, data[^1].Id) : null);
    }

    /// <summary>
    /// Moves every row that points at the duplicate onto the survivor, one <c>UPDATE</c> per table.
    /// <para>
    /// Set-based rather than entity-by-entity. A customer of ten years has hundreds of rows across
    /// these tables and none of them holds an invariant that depends on <i>which</i> customer they
    /// point at — loading them to change one column would be slower and would prove nothing.
    /// </para>
    /// <para>
    /// <c>ExecuteUpdateAsync</c> bypasses the change tracker and the <c>xmin</c> concurrency token,
    /// which is correct here and would not be elsewhere: this is a bulk reassignment inside a
    /// transaction that has already taken its own <c>If-Match</c> check on the survivor. Any entity
    /// loaded before this call is stale afterwards — nothing in the merge handler reads one.
    /// </para>
    /// <para>
    /// Carer links are <b>not</b> in this list. They carry a unique index on
    /// <c>(pet_id, customer_id)</c> that a blind reassignment would violate whenever both records
    /// hold a link to the same patient, so the merge handler folds them first.
    /// </para>
    /// </summary>
    public async Task<IReadOnlyDictionary<string, int>> ReassignAsync(
        string duplicateId,
        string survivingId,
        CancellationToken cancellationToken = default) =>
        new Dictionary<string, int>
        {
            ["pets"] = await db.Set<Pet>()
                .Where(pet => pet.PrimaryCustomerId == duplicateId)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(pet => pet.PrimaryCustomerId, survivingId), cancellationToken),

            ["appointments"] = await db.Set<Appointment>()
                .Where(appointment => appointment.CustomerId == duplicateId)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(appointment => appointment.CustomerId, survivingId), cancellationToken),

            ["queue_tickets"] = await db.Set<QueueTicket>()
                .Where(ticket => ticket.CustomerId == duplicateId)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(ticket => ticket.CustomerId, survivingId), cancellationToken),

            ["consultations"] = await db.Set<Consultation>()
                .Where(consultation => consultation.CustomerId == duplicateId)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(consultation => consultation.CustomerId, survivingId), cancellationToken),

            ["medical_records"] = await db.Set<MedicalRecord>()
                .Where(record => record.CustomerId == duplicateId)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(record => record.CustomerId, survivingId), cancellationToken),

            ["invoices"] = await db.Set<Invoice>()
                .Where(invoice => invoice.CustomerId == duplicateId)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(invoice => invoice.CustomerId, survivingId), cancellationToken),

            // Consent moves with the person. Both records' decisions become one history, and
            // "newest row per purpose" then answers for the person rather than for a fragment.
            ["customer_consents"] = await db.Set<CustomerConsent>()
                .Where(consent => consent.CustomerId == duplicateId)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(consent => consent.CustomerId, survivingId), cancellationToken),

            ["data_subject_requests"] = await db.Set<DataSubjectRequest>()
                .Where(request => request.CustomerId == duplicateId)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(request => request.CustomerId, survivingId), cancellationToken),
        };
}

/// <summary>EF Core implementation of the portal-invitation port.</summary>
internal sealed class CustomerInvitationRepository(HappyPawsDbContext db) : ICustomerInvitationRepository
{
    public void Add(CustomerInvitation invitation) => db.Set<CustomerInvitation>().Add(invitation);

    /// <summary>Tracked: the caller refreshes it in place rather than writing a second row.</summary>
    public Task<CustomerInvitation?> LatestForCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default) =>
        db.Set<CustomerInvitation>()
            .Where(invitation => invitation.CustomerId == customerId)
            .OrderByDescending(invitation => invitation.SentAt)
            .FirstOrDefaultAsync(cancellationToken);
}
