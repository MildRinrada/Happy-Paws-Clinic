using HappyPaws.Application.Analytics;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Queueing;
using HappyPaws.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Analytics;

/// <summary>
/// SQL projections over appointments, queue tickets and consultations.
/// <para>
/// Statuses are compared as the text EF writes for each enum member (<c>'Cancelled'</c>,
/// <c>'InRoom'</c>), and days are cut with <c>AT TIME ZONE</c> in the clinic's zone, so a 00:30
/// Bangkok check-in is the day it is in Bangkok. The indexes these lean on already exist:
/// <c>ix_appointments_branch_id_starts_at_status</c> and the queue's per-branch service date.
/// </para>
/// </summary>
internal sealed class OperationalStatsQueries(HappyPawsDbContext db, IOptions<ClinicOptions> clinic)
    : IOperationalStatsQueries
{
    private const string AppointmentCountsSql =
        """
        SELECT (a.starts_at AT TIME ZONE @tz)::date AS day,
               a.status,
               a.is_walk_in,
               a.is_late_cancellation,
               (a.booked_by_staff_id IS NULL AND NOT a.is_walk_in) AS booked_online,
               COUNT(*)::int AS appointments
        FROM appointments a
        WHERE a.starts_at >= @from AND a.starts_at < @to
          AND a.status <> 'Draft'
          AND (@all_branches OR a.branch_id = ANY(@branches))
        GROUP BY 1, 2, 3, 4, 5
        """;

    private const string QueueCountsSql =
        """
        SELECT t.service_date,
               EXTRACT(HOUR FROM t.issued_at AT TIME ZONE @tz)::int AS hour,
               t.status,
               COUNT(*)::int AS tickets,
               COUNT(t.called_at)::int AS called,
               COALESCE(SUM(GREATEST(EXTRACT(EPOCH FROM t.called_at - t.issued_at), 0)), 0)::float8 AS wait_seconds,
               COUNT(*) FILTER (WHERE t.entered_room_at IS NOT NULL AND t.served_at IS NOT NULL)::int AS roomed,
               COALESCE(SUM(GREATEST(EXTRACT(EPOCH FROM t.served_at - t.entered_room_at), 0)), 0)::float8 AS room_seconds
        FROM queue_tickets t
        WHERE t.service_date BETWEEN @first AND @last
          AND (@all_branches OR t.branch_id = ANY(@branches))
        GROUP BY 1, 2, 3
        """;

    private const string WorkloadSql =
        """
        WITH closed AS (
            SELECT c.vet_staff_id AS staff_id,
                   COUNT(*)::int AS cases,
                   SUM(GREATEST(EXTRACT(EPOCH FROM c.closed_at - c.opened_at), 0))::float8 AS case_seconds
            FROM consultations c
            WHERE c.status = 'Closed' AND c.closed_at >= @from AND c.closed_at < @to
              AND (@all_branches OR c.branch_id = ANY(@branches))
            GROUP BY 1),
        drafts AS (
            SELECT c.vet_staff_id AS staff_id, COUNT(*)::int AS open_cases
            FROM consultations c
            WHERE c.status = 'Draft'
              AND (@all_branches OR c.branch_id = ANY(@branches))
            GROUP BY 1),
        waiting AS (
            SELECT t.staff_id, COUNT(*)::int AS waiting
            FROM queue_tickets t
            WHERE t.service_date = @today AND t.status IN ('Waiting', 'Called')
              AND (@all_branches OR t.branch_id = ANY(@branches))
            GROUP BY 1),
        booked AS (
            SELECT a.staff_id, COUNT(*)::int AS appointments
            FROM appointments a
            WHERE a.starts_at >= @from AND a.starts_at < @to
              AND a.staff_id IS NOT NULL
              AND a.status NOT IN ('Draft', 'Cancelled')
              AND (@all_branches OR a.branch_id = ANY(@branches))
            GROUP BY 1),
        people AS (
            SELECT staff_id FROM closed
            UNION SELECT staff_id FROM drafts
            UNION SELECT staff_id FROM waiting
            UNION SELECT staff_id FROM booked)
        SELECT p.staff_id,
               u.display_name,
               COALESCE(cl.cases, 0),
               COALESCE(cl.case_seconds, 0)::float8,
               COALESCE(d.open_cases, 0),
               COALESCE(w.waiting, 0),
               COALESCE(b.appointments, 0)
        FROM people p
        LEFT JOIN staff_memberships m ON m.id = p.staff_id
        LEFT JOIN user_accounts u ON u.id = m.user_id
        LEFT JOIN closed cl ON cl.staff_id = p.staff_id
        LEFT JOIN drafts d ON d.staff_id = p.staff_id
        LEFT JOIN waiting w ON w.staff_id IS NOT DISTINCT FROM p.staff_id
        LEFT JOIN booked b ON b.staff_id = p.staff_id
        """;

    private const string TodaySnapshotSql =
        """
        SELECT
          (SELECT COUNT(*) FROM consultations c
            WHERE c.status = 'Draft'
              AND (@all_branches OR c.branch_id = ANY(@branches)))::int,
          (SELECT COUNT(*) FROM consultations c
            WHERE c.status = 'Closed' AND c.closed_at >= @from AND c.closed_at < @to
              AND (@all_branches OR c.branch_id = ANY(@branches)))::int,
          (SELECT COUNT(*) FROM appointments a
            WHERE a.staff_id = @staff AND a.starts_at >= @from AND a.starts_at < @to
              AND a.status NOT IN ('Draft', 'Cancelled')
              AND (@all_branches OR a.branch_id = ANY(@branches)))::int,
          (SELECT COUNT(*) FROM queue_tickets t
            WHERE t.staff_id = @staff AND t.service_date = @first AND t.status IN ('Waiting', 'Called')
              AND (@all_branches OR t.branch_id = ANY(@branches)))::int,
          (SELECT COUNT(*) FROM consultations c
            WHERE c.vet_staff_id = @staff AND c.status = 'Draft'
              AND (@all_branches OR c.branch_id = ANY(@branches)))::int,
          (SELECT MIN(t.issued_at) FROM queue_tickets t
            WHERE t.service_date = @first AND t.status = 'Waiting'
              AND (@all_branches OR t.branch_id = ANY(@branches)))
        """;

    private string TimeZone => clinic.Value.TimeZone;

    public async Task<IReadOnlyList<AppointmentCountRow>> AppointmentCountsAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            AppointmentCountsSql,
            AnalyticsSql.WindowParameters(window, TimeZone),
            reader => new AppointmentCountRow(
                reader.GetFieldValue<DateOnly>(0),
                reader.ParseEnum<AppointmentStatus>(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3),
                reader.GetBoolean(4),
                reader.GetInt32(5)),
            cancellationToken);

    public async Task<IReadOnlyList<QueueCountRow>> QueueCountsAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            QueueCountsSql,
            AnalyticsSql.WindowParameters(window, TimeZone),
            reader => new QueueCountRow(
                reader.GetFieldValue<DateOnly>(0),
                reader.GetInt32(1),
                reader.ParseEnum<QueueTicketStatus>(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetDouble(5),
                reader.GetInt32(6),
                reader.GetDouble(7)),
            cancellationToken);

    public async Task<IReadOnlyList<WorkloadRow>> WorkloadAsync(
        ReportWindow window,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var parameters = AnalyticsSql.WindowParameters(window, TimeZone);
        parameters.Add(AnalyticsSql.Date("today", today));

        return await AnalyticsSql.QueryAsync(
            db,
            WorkloadSql,
            parameters,
            reader => new WorkloadRow(
                reader.NullableString(0),
                reader.NullableString(1),
                reader.GetInt32(2),
                reader.GetDouble(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetInt32(6)),
            cancellationToken);
    }

    public async Task<TodaySnapshotRow> TodaySnapshotAsync(
        ReportWindow today,
        string? staffId,
        CancellationToken cancellationToken = default)
    {
        var parameters = AnalyticsSql.WindowParameters(today, TimeZone);
        parameters.Add(AnalyticsSql.Text("staff", staffId));

        var rows = await AnalyticsSql.QueryAsync(
            db,
            TodaySnapshotSql,
            parameters,
            reader => new TodaySnapshotRow(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5)),
            cancellationToken);

        return rows.Single();
    }
}
