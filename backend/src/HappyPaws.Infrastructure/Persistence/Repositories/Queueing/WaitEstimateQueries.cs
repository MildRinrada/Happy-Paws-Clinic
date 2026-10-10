using HappyPaws.Application.Queueing.WaitEstimate;
using HappyPaws.Domain.Queueing;
using HappyPaws.Infrastructure.Persistence.Repositories.Analytics;
using HappyPaws.Infrastructure.Persistence.Repositories.Forecasting;
using Npgsql;
using NpgsqlTypes;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Queueing;

/// <summary>
/// SQL counts for the walk-in wait estimate of <c>docs/contract-extensions.md § 37</c>, run through
/// <see cref="AnalyticsSql"/> like the forecasts. Each query returns one number or one pair, never a
/// row about a ticket. Indexes leaned on: the queue's <c>(branch_id, service_date, status)</c> and the
/// shifts' <c>(branch_id, starts_at, ends_at)</c>.
/// </summary>
internal sealed class WaitEstimateQueries(HappyPawsDbContext db) : IWaitEstimateQueries
{
    private const string WaitingSql =
        """
        SELECT COUNT(*)::int
        FROM queue_tickets t
        WHERE t.branch_id = @branch AND t.service_date = @day AND t.status = ANY(@statuses)
        """;

    private const string ConsultationTimeSql =
        """
        SELECT COUNT(*)::int,
               (percentile_cont(0.5) WITHIN GROUP (ORDER BY EXTRACT(EPOCH FROM t.served_at - t.entered_room_at)::float8 / 60.0))::float8
        FROM queue_tickets t
        WHERE t.branch_id = @branch AND t.served_at >= @from AND t.served_at < @to
          AND t.entered_room_at IS NOT NULL
          AND t.served_at > t.entered_room_at
          AND t.served_at - t.entered_room_at < INTERVAL '4 hours'
        """;

    private const string VetsOnDutySql =
        $"""
        SELECT COUNT(DISTINCT s.staff_id)::int
        FROM staff_shifts s
        JOIN staff_memberships m ON m.id = s.staff_id
        WHERE s.branch_id = @branch AND s.cancelled_at IS NULL
          AND s.starts_at <= @at AND s.ends_at > @at
          AND {DueBackSql.VetCondition}
        """;

    private static readonly string[] AwaitingCall =
        [.. QueueTicketTransitions.AwaitingCallStatuses.Select(status => status.ToString())];

    public async Task<int> WaitingAsync(string branchId, DateOnly serviceDate, CancellationToken cancellationToken = default)
    {
        var rows = await AnalyticsSql.QueryAsync(
            db,
            WaitingSql,
            [
                AnalyticsSql.Text("branch", branchId),
                AnalyticsSql.Date("day", serviceDate),
                new("statuses", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = AwaitingCall },
            ],
            reader => reader.GetInt32(0),
            cancellationToken);

        return rows.Single();
    }

    public async Task<ConsultationTimeRow> ConsultationTimeAsync(
        string branchId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var rows = await AnalyticsSql.QueryAsync(
            db,
            ConsultationTimeSql,
            [AnalyticsSql.Text("branch", branchId), Instant("from", from), Instant("to", to)],
            reader => new ConsultationTimeRow(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetDouble(1)),
            cancellationToken);

        return rows.Single();
    }

    public async Task<int> VetsOnDutyAsync(string branchId, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var rows = await AnalyticsSql.QueryAsync(
            db,
            VetsOnDutySql,
            [AnalyticsSql.Text("branch", branchId), Instant("at", at)],
            reader => reader.GetInt32(0),
            cancellationToken);

        return rows.Single();
    }

    private static NpgsqlParameter Instant(string name, DateTimeOffset value) =>
        new(name, NpgsqlDbType.TimestampTz) { Value = value.ToUniversalTime() };
}
