namespace HappyPaws.Application.Forecasting;

/// <summary>
/// The read model behind the forecasts of <c>docs/contract-extensions.md § 33</c>: small grouped
/// rows over the stock ledger, appointments, queue tickets, the roster, vaccinations and visits.
/// <para>
/// Hand-written SQL in Infrastructure, like the operational statistics: the grouping happens in
/// PostgreSQL and the handlers only fold the rows with the rules in this folder. Nothing here
/// writes; a forecast is worked out again on every read.
/// </para>
/// </summary>
public interface IForecastQueries
{
    /// <summary>
    /// Per item at one branch: units taken out for use (<c>issue</c> and <c>dispense</c>) inside the
    /// two windows, and the first ledger row of any kind.
    /// </summary>
    Task<IReadOnlyList<StockUsageRow>> StockUsageAsync(
        string branchId,
        DateTimeOffset longFrom,
        DateTimeOffset shortFrom,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appointments at one branch starting in <c>[from, to)</c>, by clinic day and hour: booked
    /// ahead (not walk-ins) and still going ahead or attended (not draft, cancelled or no-show).
    /// </summary>
    Task<IReadOnlyList<HourCountRow>> AppointmentsByHourAsync(
        string branchId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Walk-in queue tickets (no appointment, or a walk-in appointment) at one branch, by service
    /// day and clinic hour issued, any status.
    /// </summary>
    Task<IReadOnlyList<HourCountRow>> WalkInsByHourAsync(
        string branchId,
        DateOnly first,
        DateOnly last,
        CancellationToken cancellationToken = default);

    /// <summary>The first service day the branch issued any queue ticket, or null when it never has.</summary>
    Task<DateOnly?> FirstTicketDayAsync(string branchId, CancellationToken cancellationToken = default);

    /// <summary>Active (not cancelled) shifts of vets at one branch that overlap <c>[from, to)</c>.</summary>
    Task<IReadOnlyList<ShiftSpan>> VetShiftsAsync(
        string branchId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>booked</c> and <c>confirmed</c> appointments (not walk-ins) starting in <c>[from, to)</c>,
    /// narrowed to the branches given (every branch when null), earliest first.
    /// </summary>
    Task<IReadOnlyList<RiskCandidateRow>> AppointmentsForRiskAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyCollection<string>? branchIds,
        CancellationToken cancellationToken = default);

    /// <summary>One page of the due-back call list, most recently due first.</summary>
    Task<IReadOnlyList<DueBackRow>> DueBackAsync(DueBackFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The living pets of these customers, and for <c>vaccine_overdue</c> their overdue doses.</summary>
    Task<IReadOnlyList<DueBackPetRow>> DueBackPetsAsync(
        DueBackFilter filter,
        IReadOnlyCollection<string> customerIds,
        CancellationToken cancellationToken = default);
}

/// <param name="ItemId">The item.</param>
/// <param name="UsedLong">Units issued or dispensed inside the long window.</param>
/// <param name="UsedShort">Units issued or dispensed inside the short window.</param>
/// <param name="FirstMovementAt">The item's first ledger row at the branch, of any kind.</param>
public sealed record StockUsageRow(string ItemId, int UsedLong, int UsedShort, DateTimeOffset FirstMovementAt);

/// <param name="Day">Clinic day.</param>
/// <param name="Hour">Clinic hour, 0 to 23.</param>
/// <param name="Count">How many.</param>
public sealed record HourCountRow(DateOnly Day, int Hour, int Count);

/// <param name="StaffId">The vet.</param>
/// <param name="StartsAt">Shift start.</param>
/// <param name="EndsAt">Shift end.</param>
public sealed record ShiftSpan(string StaffId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

/// <summary>An appointment whose attendance is judged.</summary>
public sealed record RiskCandidateRow(
    string AppointmentId,
    string BranchId,
    DateTimeOffset StartsAt,
    string Status,
    DateTimeOffset BookedAt,
    string CustomerId,
    string CustomerName,
    string PetId,
    string PetName,
    string? StaffId);

/// <summary>Why a customer is on the due-back list.</summary>
public enum DueBackReason
{
    /// <summary>A living pet's latest dose of some vaccine is past its next due date, and no visit is booked.</summary>
    VaccineOverdue,

    /// <summary>Longer since the last visit than the customer's own usual gap allows (<see cref="LapsedRule"/>).</summary>
    Lapsed,
}

/// <summary>What one page of the call list is asked for.</summary>
/// <param name="Reason">Which list.</param>
/// <param name="Today">The clinic's today.</param>
/// <param name="Now">Now, for "no upcoming appointment".</param>
/// <param name="BranchIds">Customers who have visited one of these branches, with visits counted only there; null for all.</param>
/// <param name="Limit">Rows wanted, plus one is read to know whether there is a next page.</param>
/// <param name="AfterDueOn">Cursor: the due date of the last row of the previous page.</param>
/// <param name="AfterCustomerId">Cursor: the customer of that row.</param>
public sealed record DueBackFilter(
    DueBackReason Reason,
    DateOnly Today,
    DateTimeOffset Now,
    IReadOnlyCollection<string>? BranchIds,
    int Limit,
    DateOnly? AfterDueOn = null,
    string? AfterCustomerId = null);

/// <summary>One customer on the call list.</summary>
/// <param name="CustomerId">The customer.</param>
/// <param name="CustomerName">Their display name.</param>
/// <param name="DueOn">The earliest overdue dose's due date, or the day they would usually have been back.</param>
/// <param name="LastVisitOn">Their latest visit, in the branches counted.</param>
/// <param name="VisitCount">Visit days counted (<c>lapsed</c> only).</param>
/// <param name="MedianGapDays">The median gap between those visits (<c>lapsed</c> only).</param>
/// <param name="AllowedDays">How long after the last visit they count as lapsed (<c>lapsed</c> only).</param>
public sealed record DueBackRow(
    string CustomerId,
    string CustomerName,
    DateOnly DueOn,
    DateOnly? LastVisitOn,
    int? VisitCount,
    double? MedianGapDays,
    int? AllowedDays);

/// <summary>A living pet of a customer on the list; with a vaccine for <c>vaccine_overdue</c>, one row per overdue dose.</summary>
public sealed record DueBackPetRow(
    string CustomerId,
    string PetId,
    string PetName,
    string Species,
    string? VaccineName,
    DateOnly? VaccineDueOn);
