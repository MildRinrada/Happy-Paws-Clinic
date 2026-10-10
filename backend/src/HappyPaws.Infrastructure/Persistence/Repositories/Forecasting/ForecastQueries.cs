using System.Data.Common;
using HappyPaws.Application.Forecasting;
using HappyPaws.Infrastructure.Common;
using HappyPaws.Infrastructure.Persistence.Repositories.Analytics;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Forecasting;

/// <summary>
/// SQL projections for the forecasts of <c>docs/contract-extensions.md § 33</c>, run through
/// <see cref="AnalyticsSql"/> like the statistics reports.
/// <para>
/// Days and hours are cut with <c>AT TIME ZONE</c> in the clinic's zone, exactly as
/// <c>OperationalStatsQueries</c> cuts them, so "10:00 on Monday" means the same hour in the demand
/// forecast as on the queue report. Statuses and movement kinds compare as the text EF writes.
/// </para>
/// <para>
/// Indexes leaned on: <c>ix_appointments_branch_id_starts_at_status</c>, the queue's per-branch
/// service date, the shifts' <c>(branch_id, starts_at, ends_at)</c>. The stock usage read groups a
/// branch's whole ledger; see the "index ที่ควรเพิ่มเมื่อข้อมูลโต" note in the § 33 docs.
/// </para>
/// </summary>
internal sealed class ForecastQueries(HappyPawsDbContext db, IOptions<ClinicOptions> clinic) : IForecastQueries
{
    private const string StockUsageSql =
        """
        SELECT m.item_id,
               COALESCE(-SUM(m.quantity) FILTER (WHERE m.kind IN ('Issue', 'Dispense')
                                                 AND m.occurred_at >= @long_from AND m.occurred_at < @to), 0)::int,
               COALESCE(-SUM(m.quantity) FILTER (WHERE m.kind IN ('Issue', 'Dispense')
                                                 AND m.occurred_at >= @short_from AND m.occurred_at < @to), 0)::int,
               MIN(m.occurred_at)
        FROM stock_movements m
        WHERE m.branch_id = @branch
        GROUP BY m.item_id
        """;

    private const string AppointmentsByHourSql =
        """
        SELECT (a.starts_at AT TIME ZONE @tz)::date,
               EXTRACT(HOUR FROM a.starts_at AT TIME ZONE @tz)::int,
               COUNT(*)::int
        FROM appointments a
        WHERE a.branch_id = @branch AND a.starts_at >= @from AND a.starts_at < @to
          AND NOT a.is_walk_in
          AND a.status NOT IN ('Draft', 'Cancelled', 'NoShow')
        GROUP BY 1, 2
        """;

    private const string WalkInsByHourSql =
        """
        SELECT t.service_date,
               EXTRACT(HOUR FROM t.issued_at AT TIME ZONE @tz)::int,
               COUNT(*)::int
        FROM queue_tickets t
        LEFT JOIN appointments a ON a.id = t.appointment_id
        WHERE t.branch_id = @branch AND t.service_date BETWEEN @first AND @last
          AND (t.appointment_id IS NULL OR a.is_walk_in)
        GROUP BY 1, 2
        """;

    private const string FirstTicketSql =
        """
        SELECT MIN(t.service_date) FROM queue_tickets t WHERE t.branch_id = @branch
        """;

    private const string VetShiftsSql =
        $"""
        SELECT s.staff_id, s.starts_at, s.ends_at
        FROM staff_shifts s
        JOIN staff_memberships m ON m.id = s.staff_id
        WHERE s.branch_id = @branch AND s.cancelled_at IS NULL
          AND s.starts_at < @to AND s.ends_at > @from
          AND {DueBackSql.VetCondition}
        """;

    private const string RiskCandidatesSql =
        """
        SELECT a.id, a.branch_id, a.starts_at, a.status, a.created_at,
               a.customer_id, c.display_name, a.pet_id, p.name, a.staff_id
        FROM appointments a
        JOIN customers c ON c.id = a.customer_id
        JOIN pets p ON p.id = a.pet_id
        WHERE a.starts_at >= @from AND a.starts_at < @to
          AND a.status IN ('Booked', 'Confirmed')
          AND NOT a.is_walk_in
          AND (@all_branches OR a.branch_id = ANY(@branches))
        ORDER BY a.starts_at, a.id
        """;

    private string TimeZone => clinic.Value.TimeZone;

    public async Task<IReadOnlyList<StockUsageRow>> StockUsageAsync(
        string branchId,
        DateTimeOffset longFrom,
        DateTimeOffset shortFrom,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            StockUsageSql,
            [
                AnalyticsSql.Text("branch", branchId),
                Instant("long_from", longFrom),
                Instant("short_from", shortFrom),
                Instant("to", to),
            ],
            reader => new StockUsageRow(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetFieldValue<DateTimeOffset>(3)),
            cancellationToken);

    public async Task<IReadOnlyList<HourCountRow>> AppointmentsByHourAsync(
        string branchId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            AppointmentsByHourSql,
            [AnalyticsSql.Text("tz", TimeZone), AnalyticsSql.Text("branch", branchId), Instant("from", from), Instant("to", to)],
            HourCount,
            cancellationToken);

    public async Task<IReadOnlyList<HourCountRow>> WalkInsByHourAsync(
        string branchId,
        DateOnly first,
        DateOnly last,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            WalkInsByHourSql,
            [
                AnalyticsSql.Text("tz", TimeZone),
                AnalyticsSql.Text("branch", branchId),
                AnalyticsSql.Date("first", first),
                AnalyticsSql.Date("last", last),
            ],
            HourCount,
            cancellationToken);

    public async Task<DateOnly?> FirstTicketDayAsync(string branchId, CancellationToken cancellationToken = default)
    {
        var rows = await AnalyticsSql.QueryAsync(
            db,
            FirstTicketSql,
            [AnalyticsSql.Text("branch", branchId)],
            reader => reader.IsDBNull(0) ? (DateOnly?)null : reader.GetFieldValue<DateOnly>(0),
            cancellationToken);

        return rows.SingleOrDefault();
    }

    public async Task<IReadOnlyList<ShiftSpan>> VetShiftsAsync(
        string branchId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            VetShiftsSql,
            [AnalyticsSql.Text("branch", branchId), Instant("from", from), Instant("to", to)],
            reader => new ShiftSpan(
                reader.GetString(0),
                reader.GetFieldValue<DateTimeOffset>(1),
                reader.GetFieldValue<DateTimeOffset>(2)),
            cancellationToken);

    public async Task<IReadOnlyList<RiskCandidateRow>> AppointmentsForRiskAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyCollection<string>? branchIds,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            RiskCandidatesSql,
            [
                Instant("from", from),
                Instant("to", to),
                new("all_branches", NpgsqlDbType.Boolean) { Value = branchIds is null },
                new("branches", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = branchIds?.ToArray() ?? [] },
            ],
            reader => new RiskCandidateRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.NullableString(9)),
            cancellationToken);

    public async Task<IReadOnlyList<DueBackRow>> DueBackAsync(DueBackFilter filter, CancellationToken cancellationToken = default)
    {
        var parameters = DueBackSql.Parameters(
            TimeZone,
            filter.Today,
            filter.Now,
            filter.BranchIds,
            limit: filter.Limit + 1,
            afterDue: filter.AfterDueOn,
            afterId: filter.AfterCustomerId);

        return filter.Reason == DueBackReason.Lapsed
            ? await AnalyticsSql.QueryAsync(
                db,
                DueBackSql.LapsedPage,
                parameters,
                reader => new DueBackRow(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetFieldValue<DateOnly>(2),
                    reader.GetFieldValue<DateOnly>(3),
                    reader.GetInt32(4),
                    reader.GetDouble(5),
                    reader.GetInt32(6)),
                cancellationToken)
            : await AnalyticsSql.QueryAsync(
                db,
                DueBackSql.OverduePage,
                parameters,
                reader => new DueBackRow(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetFieldValue<DateOnly>(2),
                    reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
                    VisitCount: null,
                    MedianGapDays: null,
                    AllowedDays: null),
                cancellationToken);
    }

    public async Task<IReadOnlyList<DueBackPetRow>> DueBackPetsAsync(
        DueBackFilter filter,
        IReadOnlyCollection<string> customerIds,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            filter.Reason == DueBackReason.Lapsed ? DueBackSql.LivingPets : DueBackSql.OverduePets,
            DueBackSql.Parameters(TimeZone, filter.Today, filter.Now, filter.BranchIds, customerIds: customerIds),
            DuePet,
            cancellationToken);

    private static HourCountRow HourCount(DbDataReader reader) =>
        new(reader.GetFieldValue<DateOnly>(0), reader.GetInt32(1), reader.GetInt32(2));

    private static DueBackPetRow DuePet(DbDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.NullableString(4),
            reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5));

    private static NpgsqlParameter Instant(string name, DateTimeOffset value) =>
        new(name, NpgsqlDbType.TimestampTz) { Value = value.ToUniversalTime() };
}
