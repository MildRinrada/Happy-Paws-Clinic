using HappyPaws.Application.Appointments;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Appointments;

/// <summary>
/// EF Core implementation of the appointment port.
/// <para>
/// Paging is keyset-based on <c>(starts_at, id)</c>, the pair the list indexes are built on.
/// Offset paging would drift as the day's board is appended to while a client reads it — which is
/// the normal state of a clinic morning, not an edge case.
/// </para>
/// </summary>
internal sealed class AppointmentRepository(HappyPawsDbContext db, IClock clock) : IAppointmentRepository
{
    public Task<Appointment?> FindByIdAsync(string appointmentId, CancellationToken cancellationToken = default) =>
        db.Set<Appointment>().FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

    public void Add(Appointment appointment) => db.Set<Appointment>().Add(appointment);

    public async Task<IReadOnlyList<Appointment>> OccupyingSlotsAsync(
        string branchId,
        IReadOnlyCollection<string> staffIds,
        DateTimeOffset from,
        DateTimeOffset to,
        string? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (staffIds.Count == 0)
        {
            return [];
        }

        var occupying = AppointmentTransitions.OccupyingStatuses;

        var query = db.Set<Appointment>()
            .AsNoTracking()
            .Where(a => a.BranchId == branchId
                && a.StaffId != null
                && staffIds.Contains(a.StaffId)
                && occupying.Contains(a.Status)
                // Half-open overlap with the window, so an appointment straddling midnight is
                // still returned for the day it runs into.
                && a.StartsAt < to
                && a.EndsAt > from);

        if (excludeAppointmentId is { Length: > 0 })
        {
            query = query.Where(a => a.Id != excludeAppointmentId);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> UpcomingForPetAsync(
        string petId,
        DateTimeOffset from,
        CancellationToken cancellationToken = default)
    {
        var occupying = AppointmentTransitions.OccupyingStatuses;

        // Tracked, not AsNoTracking: the caller cancels these.
        return await db.Set<Appointment>()
            .Where(a => a.PetId == petId && a.StartsAt >= from && occupying.Contains(a.Status))
            .OrderBy(a => a.StartsAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> CancelledForPetAsync(
        string petId,
        string reason,
        DateTimeOffset? since,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<Appointment>()
            .AsNoTracking()
            .Where(a => a.PetId == petId
                && a.Status == AppointmentStatus.Cancelled
                && a.CancellationReason == reason);

        if (since is { } from)
        {
            query = query.Where(a => a.CancelledAt >= from);
        }

        return await query
            .OrderBy(a => a.StartsAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<CursorPage<Appointment>> ListForStaffAsync(
        AppointmentListFilter filter,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = db.Set<Appointment>().AsNoTracking().AsQueryable();

        if (filter.VisibleBranchIds is { Count: > 0 } visible)
        {
            // Applied at the database, always. This is the branch restriction from the caller's
            // token, and a caller must not be able to page past it.
            query = query.Where(a => visible.Contains(a.BranchId));
        }

        if (filter.BranchId is { Length: > 0 } branchId)
        {
            query = query.Where(a => a.BranchId == branchId);
        }

        if (filter.Date is { } date)
        {
            var (from, to) = ClinicDay(date);
            query = query.Where(a => a.StartsAt >= from && a.StartsAt < to);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (filter.StaffId is { Length: > 0 } staffId)
        {
            query = query.Where(a => a.StaffId == staffId);
        }

        if (filter.Query is { Length: > 0 } term)
        {
            // "?q=มิโล" searches the patient, which is how the counter thinks about the board.
            var pattern = $"%{term}%";
            var petIds = db.Set<Pet>()
                .Where(pet => EF.Functions.ILike(pet.Name, pattern) || EF.Functions.ILike(pet.HospitalNumber, pattern))
                .Select(pet => pet.Id);

            query = query.Where(a => petIds.Contains(a.PetId));
        }

        return await PageAscendingAsync(query, page, cancellationToken);
    }

    public async Task<CursorPage<Appointment>> ListForCustomerAsync(
        string customerId,
        bool upcomingOnly,
        CursorPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var query = db.Set<Appointment>()
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId);

        return upcomingOnly
            ? await PageAscendingAsync(query.Where(a => a.StartsAt >= now), page, cancellationToken)
            : await PageDescendingAsync(query.Where(a => a.StartsAt < now), page, cancellationToken);
    }

    public async Task<AttendanceCounts> AttendanceAsync(
        string customerId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        // One grouped read rather than four counts. Measured against the visit's own time, so a
        // strike falls out of the window a year after the visit, not a year after it was recorded.
        var rows = await db.Set<Appointment>()
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId
                && (a.Status == AppointmentStatus.NoShow || (a.Status == AppointmentStatus.Cancelled && a.IsLateCancellation)))
            .GroupBy(a => new { NoShow = a.Status == AppointmentStatus.NoShow, Recent = a.StartsAt >= since })
            .Select(group => new { group.Key.NoShow, group.Key.Recent, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int Sum(bool noShow, bool recentOnly) =>
            rows.Where(row => row.NoShow == noShow && (!recentOnly || row.Recent)).Sum(row => row.Count);

        return new AttendanceCounts(Sum(true, false), Sum(false, false), Sum(true, true), Sum(false, true));
    }

    public async Task<IReadOnlyList<Appointment>> OccupyingForPetAsync(
        string petId,
        DateTimeOffset from,
        DateTimeOffset to,
        string? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var occupying = AppointmentTransitions.OccupyingStatuses;

        var query = db.Set<Appointment>()
            .AsNoTracking()
            .Where(a => a.PetId == petId && occupying.Contains(a.Status) && a.StartsAt < to && a.EndsAt > from);

        if (excludeAppointmentId is { Length: > 0 })
        {
            query = query.Where(a => a.Id != excludeAppointmentId);
        }

        return await query.OrderBy(a => a.StartsAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> OccupyingInWindowAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? branchId,
        string? staffId,
        IReadOnlyCollection<string>? visibleBranchIds,
        CancellationToken cancellationToken = default)
    {
        var occupying = AppointmentTransitions.OccupyingStatuses;

        var query = db.Set<Appointment>()
            .AsNoTracking()
            .Where(a => occupying.Contains(a.Status) && a.StartsAt < to && a.EndsAt > from);

        if (visibleBranchIds is { Count: > 0 } visible)
        {
            query = query.Where(a => visible.Contains(a.BranchId));
        }

        if (branchId is { Length: > 0 })
        {
            query = query.Where(a => a.BranchId == branchId);
        }

        if (staffId is { Length: > 0 })
        {
            query = query.Where(a => a.StaffId == staffId);
        }

        return await query.OrderBy(a => a.StartsAt).ThenBy(a => a.Id).ToListAsync(cancellationToken);
    }

    /// <summary>Soonest first — today's board and upcoming visits.</summary>
    private static async Task<CursorPage<Appointment>> PageAscendingAsync(
        IQueryable<Appointment> query,
        CursorPageRequest page,
        CancellationToken cancellationToken)
    {
        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            query = query.Where(a => a.StartsAt > sortKey || (a.StartsAt == sortKey && string.Compare(a.Id, id) > 0));
        }

        var rows = await query
            .OrderBy(a => a.StartsAt).ThenBy(a => a.Id)
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        return Slice(rows, page.SafeLimit);
    }

    /// <summary>Newest first — the customer's history.</summary>
    private static async Task<CursorPage<Appointment>> PageDescendingAsync(
        IQueryable<Appointment> query,
        CursorPageRequest page,
        CancellationToken cancellationToken)
    {
        if (page.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            query = query.Where(a => a.StartsAt < sortKey || (a.StartsAt == sortKey && string.Compare(a.Id, id) < 0));
        }

        var rows = await query
            .OrderByDescending(a => a.StartsAt).ThenByDescending(a => a.Id)
            .Take(page.SafeLimit + 1)
            .ToListAsync(cancellationToken);

        return Slice(rows, page.SafeLimit);
    }

    /// <summary>
    /// Trims the extra row fetched to detect a further page. Reading one more than asked for is
    /// what lets <c>next_cursor</c> be null on the last page instead of pointing at nothing.
    /// </summary>
    private static CursorPage<Appointment> Slice(List<Appointment> rows, int limit)
    {
        if (rows.Count <= limit)
        {
            return new CursorPage<Appointment>(rows, null);
        }

        var data = rows.Take(limit).ToList();
        var last = data[^1];

        return new CursorPage<Appointment>(data, Cursor.Encode(last.StartsAt, last.Id));
    }

    private (DateTimeOffset From, DateTimeOffset To) ClinicDay(DateOnly date)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue);
        var offset = clock.ClinicTimeZone.GetUtcOffset(midnight);
        var from = new DateTimeOffset(midnight, offset);

        return (from, from.AddDays(1));
    }
}
