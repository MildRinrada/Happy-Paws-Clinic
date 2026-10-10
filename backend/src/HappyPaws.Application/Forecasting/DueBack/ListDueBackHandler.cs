using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Forecasting.DueBack;

/// <summary><c>GET /staff/customers/due-back?branch=&amp;reason=&amp;limit=&amp;cursor=</c>.</summary>
/// <param name="Reason"><c>vaccine_overdue</c> or <c>lapsed</c>. Required.</param>
/// <param name="BranchId">Customers who have visited this branch; every branch the caller may see when absent.</param>
/// <param name="Limit">Page size, 1 to 100.</param>
/// <param name="Cursor">From the previous page.</param>
public sealed record ListDueBackQuery(string? Reason, string? BranchId = null, int Limit = CursorPaging.DefaultLimit, string? Cursor = null);

/// <summary>One customer to call. Not in the API reference: <c>docs/contract-extensions.md § 33</c>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>pets</c>: their living pets; for <c>vaccine_overdue</c> only the pets with a dose overdue, each with its doses.</item>
/// <item><c>last_visit_on</c>: their latest visit at the branches counted; absent when none.</item>
/// <item><c>visit_count</c>, <c>usual_interval_days</c>, <c>allowed_days</c>: <c>lapsed</c> only. The usual
/// interval is the median gap between visits, and <c>allowed_days</c> how long after the last visit
/// they count as lapsed.</item>
/// <item><c>due_on</c>: the earliest overdue dose's due date, or the day they would usually have been back by.</item>
/// <item><c>days_overdue</c>: whole days from <c>due_on</c> to today.</item>
/// </list>
/// </remarks>
public sealed record DueBackDto(
    string CustomerId,
    string CustomerName,
    string Reason,
    IReadOnlyList<DueBackPetDto> Pets,
    DateOnly? LastVisitOn,
    int? VisitCount,
    double? UsualIntervalDays,
    int? AllowedDays,
    DateOnly DueOn,
    int DaysOverdue);

/// <param name="PetId">The pet.</param>
/// <param name="Name">Its name.</param>
/// <param name="Species">As the clinic writes it.</param>
/// <param name="OverdueVaccines">For <c>vaccine_overdue</c>: each vaccine whose latest dose is overdue, earliest due first.</param>
public sealed record DueBackPetDto(string PetId, string Name, string Species, IReadOnlyList<OverdueVaccineDto> OverdueVaccines);

/// <param name="VaccineName">As recorded on the latest dose.</param>
/// <param name="DueOn">That dose's next due date.</param>
public sealed record OverdueVaccineDto(string VaccineName, DateOnly DueOn);

/// <summary>
/// "ใครควรกลับมาแล้วแต่ยังไม่มา": a call list worked out from each customer's own records, by one
/// of two reasons.
/// <list type="bullet">
/// <item><b><c>vaccine_overdue</c></b>: a living pet whose latest dose of some vaccine (compared
/// without case, as the reminder rules compare it) has a next due date before today, and which has
/// no booked or confirmed appointment from now on. One row per primary owner.</item>
/// <item><b><c>lapsed</c></b>: <see cref="LapsedRule"/>, for customers with at least one living pet and
/// no booked or confirmed appointment from now on.</item>
/// </list>
/// <para>
/// Both lists hold active, unmerged customers only. <b>Branch</b>: like a segment, a customer belongs
/// to the branches they have visited, and with a branch (or a branch-bound caller) their visits count
/// only there. A named branch the caller does not work at is <c>403</c>.
/// </para>
/// <para>
/// <b>Order</b>: most recently due first, so the people most likely to come back are at the top and
/// a dose that fell due years ago sinks rather than heading the list for ever. Paged by cursor on
/// <c>(due_on, customer id)</c>.
/// </para>
/// </summary>
public sealed class ListDueBackHandler(
    IForecastQueries queries,
    ForecastBranches branches,
    ClinicDays clinicDays,
    IClock clock)
{
    public async Task<CursorPage<DueBackDto>> HandleAsync(ListDueBackQuery query, CancellationToken cancellationToken = default)
    {
        if (!WireEnum.TryParse<DueBackReason>(query.Reason, out var reason))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["reason"] = [$"reason ต้องเป็น {WireEnum.Names<DueBackReason>()}"],
            });
        }

        DateOnly? afterDue = null;
        string? afterId = null;

        if (query.Cursor is { Length: > 0 } cursor)
        {
            var (sortKey, id) = Cursor.Decode(cursor);
            afterDue = DateOnly.FromDateTime(sortKey.UtcDateTime);
            afterId = id;
        }

        var scope = branches.Scope(query.BranchId);
        var today = clinicDays.Today;
        var limit = new CursorPageRequest(query.Limit, query.Cursor).SafeLimit;

        var filter = new DueBackFilter(reason, today, clock.UtcNow, scope, limit, afterDue, afterId);
        var found = await queries.DueBackAsync(filter, cancellationToken);
        var page = found.Take(limit).ToList();

        var pets = page.Count == 0
            ? []
            : await queries.DueBackPetsAsync(filter, [.. page.Select(row => row.CustomerId)], cancellationToken);
        var petsByCustomer = pets.ToLookup(pet => pet.CustomerId, StringComparer.Ordinal);

        var data = page.Select(row => new DueBackDto(
            row.CustomerId,
            row.CustomerName,
            WireEnum.Of(reason),
            [.. petsByCustomer[row.CustomerId]
                .GroupBy(pet => pet.PetId, StringComparer.Ordinal)
                .Select(group => new DueBackPetDto(
                    group.Key,
                    group.First().PetName,
                    group.First().Species,
                    [.. group
                        .Where(pet => pet is { VaccineName: not null, VaccineDueOn: not null })
                        .OrderBy(pet => pet.VaccineDueOn)
                        .ThenBy(pet => pet.VaccineName, StringComparer.Ordinal)
                        .Select(pet => new OverdueVaccineDto(pet.VaccineName!, pet.VaccineDueOn!.Value))]))
                .OrderBy(pet => pet.Name, StringComparer.Ordinal)],
            row.LastVisitOn,
            row.VisitCount,
            row.MedianGapDays is { } gap ? Math.Round(gap, 1, MidpointRounding.AwayFromZero) : null,
            row.AllowedDays,
            row.DueOn,
            today.DayNumber - row.DueOn.DayNumber)).ToList();

        var next = found.Count > limit
            ? Cursor.Encode(new DateTimeOffset(page[^1].DueOn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), page[^1].CustomerId)
            : null;

        return new CursorPage<DueBackDto>(data, next);
    }
}
