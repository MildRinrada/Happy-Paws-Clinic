using HappyPaws.Application.Analytics;
using HappyPaws.Domain.Analytics;
using HappyPaws.Domain.Billing;
using HappyPaws.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Analytics;

/// <summary>
/// SQL projections over the billing tables, consultations, customers and staff.
/// <para>
/// No invoice total is stored anywhere (<c>docs/features/billing.md</c>), so a bill's total is
/// recomputed here the way the aggregate computes it: lines less discounts. Lines and discounts are
/// summed in separate subqueries per invoice; joining both to the invoice at once would multiply
/// every line by the number of discounts.
/// </para>
/// </summary>
internal sealed class BusinessAnalyticsQueries(HappyPawsDbContext db, IOptions<ClinicOptions> clinic)
    : IBusinessAnalyticsQueries
{
    private const string RevenueSql =
        """
        WITH money AS (
            SELECT (p.collected_at AT TIME ZONE @tz)::date AS day, i.branch_id, p.method,
                   p.amount_satang AS collected, 1 AS payments, 0::bigint AS refunded, 0 AS refunds
            FROM invoice_payments p
            JOIN invoices i ON i.id = p.invoice_id
            WHERE p.collected_at >= @from AND p.collected_at < @to
              AND (@all_branches OR i.branch_id = ANY(@branches))
            UNION ALL
            SELECT (r.refunded_at AT TIME ZONE @tz)::date, i.branch_id, r.method,
                   0::bigint, 0, r.amount_satang, 1
            FROM invoice_refunds r
            JOIN invoices i ON i.id = r.invoice_id
            WHERE r.refunded_at >= @from AND r.refunded_at < @to
              AND (@all_branches OR i.branch_id = ANY(@branches)))
        SELECT m.day, m.branch_id, b.name, m.method,
               SUM(m.collected)::bigint, SUM(m.payments)::int,
               SUM(m.refunded)::bigint, SUM(m.refunds)::int
        FROM money m
        LEFT JOIN branches b ON b.id = m.branch_id
        GROUP BY m.day, m.branch_id, b.name, m.method
        """;

    private const string InvoicedSql =
        """
        SELECT COALESCE(SUM(
                 COALESCE((SELECT SUM(l.quantity * l.unit_price_satang) FROM invoice_lines l WHERE l.invoice_id = i.id), 0)
               - COALESCE((SELECT SUM(d.amount_satang) FROM invoice_discounts d WHERE d.invoice_id = i.id), 0)), 0)::bigint,
               COUNT(*)::int
        FROM invoices i
        WHERE i.status <> 'Void' AND i.issued_at >= @from AND i.issued_at < @to
          AND (@all_branches OR i.branch_id = ANY(@branches))
        """;

    // Each line's share of what its bill kept is amount × kept ÷ bill subtotal, so the lines of a
    // bill add back up to the bill. Medicine is then shared over the bill's service lines by their
    // own amounts. Rounding happens once, on the grouped sums, not per line.
    private const string ServiceRevenueSql =
        """
        WITH bills AS (
            SELECT i.id,
                   COALESCE((SELECT SUM(p.amount_satang) FROM invoice_payments p WHERE p.invoice_id = i.id), 0)
                 - COALESCE((SELECT SUM(r.amount_satang) FROM invoice_refunds r WHERE r.invoice_id = i.id), 0) AS kept
            FROM invoices i
            WHERE i.status <> 'Void' AND i.issued_at >= @from AND i.issued_at < @to
              AND (@all_branches OR i.branch_id = ANY(@branches))),
        lines AS (
            SELECT l.invoice_id, l.kind, l.description, l.quantity,
                   (l.quantity * l.unit_price_satang)::numeric AS amount,
                   COALESCE(ct.service_id, direct.id) AS service_id,
                   b.kept::numeric AS kept
            FROM bills b
            JOIN invoice_lines l ON l.invoice_id = b.id
            LEFT JOIN consultation_treatments ct ON ct.id = l.source_id
            LEFT JOIN clinic_services direct ON direct.id = l.source_id),
        shared AS (
            SELECT ln.*,
                   SUM(ln.amount) OVER (PARTITION BY ln.invoice_id) AS bill_amount,
                   COALESCE(SUM(ln.amount) FILTER (WHERE ln.kind = 'Service') OVER (PARTITION BY ln.invoice_id), 0) AS service_amount,
                   COALESCE(SUM(ln.amount) FILTER (WHERE ln.kind = 'Medicine') OVER (PARTITION BY ln.invoice_id), 0) AS medicine_amount
            FROM lines ln),
        allocated AS (
            SELECT s.*,
                   CASE WHEN s.bill_amount > 0 THEN s.amount * s.kept / s.bill_amount ELSE 0 END AS revenue,
                   CASE WHEN s.kind = 'Service' AND s.service_amount > 0 AND s.bill_amount > 0
                        THEN s.medicine_amount * s.kept / s.bill_amount * s.amount / s.service_amount
                        ELSE 0 END AS medicine
            FROM shared s)
        SELECT CASE WHEN k.kind = 'Service' THEN k.service_id END AS service_id,
               CASE WHEN k.kind = 'Service' THEN COALESCE(cs.name, k.description) END AS name,
               k.kind,
               SUM(k.quantity)::int,
               COUNT(DISTINCT k.invoice_id)::int,
               SUM(k.amount)::bigint,
               ROUND(SUM(k.revenue))::bigint,
               ROUND(SUM(k.medicine))::bigint
        FROM allocated k
        LEFT JOIN clinic_services cs ON cs.id = k.service_id
        GROUP BY 1, 2, 3
        """;

    // A visit is a consultation or a bill that was not voided. A merged-away customer counts as the
    // record they were merged into, so a duplicate fixed at the counter is not a "new" customer.
    private const string RetentionSql =
        """
        WITH visits AS (
            SELECT COALESCE(cu.merged_into_customer_id, c.customer_id) AS customer_id,
                   (c.opened_at AT TIME ZONE @tz)::date AS day
            FROM consultations c
            LEFT JOIN customers cu ON cu.id = c.customer_id
            WHERE c.opened_at < @to
              AND (@all_branches OR c.branch_id = ANY(@branches))
            UNION ALL
            SELECT COALESCE(cu.merged_into_customer_id, i.customer_id),
                   (i.issued_at AT TIME ZONE @tz)::date
            FROM invoices i
            LEFT JOIN customers cu ON cu.id = i.customer_id
            WHERE i.status <> 'Void' AND i.issued_at < @to
              AND (@all_branches OR i.branch_id = ANY(@branches))),
        firsts AS (
            SELECT customer_id, MIN(day) AS first_day FROM visits GROUP BY customer_id),
        active AS (
            SELECT DISTINCT customer_id, date_trunc('month', day)::date AS month
            FROM visits
            WHERE day >= @first)
        SELECT a.month, date_trunc('month', f.first_day)::date AS cohort, COUNT(*)::int
        FROM active a
        JOIN firsts f ON f.customer_id = a.customer_id
        GROUP BY 1, 2
        """;

    private const string StaffPerformanceSql =
        """
        WITH closed AS (
            SELECT c.vet_staff_id AS staff_id,
                   COUNT(*)::int AS cases,
                   SUM(GREATEST(EXTRACT(EPOCH FROM c.closed_at - c.opened_at), 0))::float8 AS case_seconds
            FROM consultations c
            WHERE c.status = 'Closed' AND c.closed_at >= @from AND c.closed_at < @to
              AND (@all_branches OR c.branch_id = ANY(@branches))
            GROUP BY 1),
        treated AS (
            SELECT t.performed_by_staff_id AS staff_id, COUNT(*)::int AS treatments
            FROM consultation_treatments t
            JOIN consultations c ON c.id = t.consultation_id
            WHERE t.performed_at >= @from AND t.performed_at < @to
              AND (@all_branches OR c.branch_id = ANY(@branches))
            GROUP BY 1),
        attributed AS (
            SELECT c.vet_staff_id AS staff_id, SUM(x.amount)::bigint AS revenue
            FROM (
                SELECT p.invoice_id, p.amount_satang AS amount
                FROM invoice_payments p
                WHERE p.collected_at >= @from AND p.collected_at < @to
                UNION ALL
                SELECT r.invoice_id, -r.amount_satang
                FROM invoice_refunds r
                WHERE r.refunded_at >= @from AND r.refunded_at < @to) x
            JOIN invoices i ON i.id = x.invoice_id
            JOIN consultations c ON c.id = i.consultation_id
            WHERE (@all_branches OR i.branch_id = ANY(@branches))
            GROUP BY 1),
        collected AS (
            SELECT p.collected_by_staff_id AS staff_id, SUM(p.amount_satang)::bigint AS collected
            FROM invoice_payments p
            JOIN invoices i ON i.id = p.invoice_id
            WHERE p.collected_at >= @from AND p.collected_at < @to
              AND (@all_branches OR i.branch_id = ANY(@branches))
            GROUP BY 1),
        people AS (
            SELECT m.id AS staff_id
            FROM staff_memberships m
            WHERE m.is_active
              AND (@all_branches OR cardinality(m.branch_ids) = 0 OR m.branch_ids && @branches)
            UNION SELECT staff_id FROM closed
            UNION SELECT staff_id FROM treated
            UNION SELECT staff_id FROM attributed
            UNION SELECT staff_id FROM collected)
        SELECT p.staff_id,
               COALESCE(u.display_name, p.staff_id),
               m.staff_type,
               COALESCE(cl.cases, 0),
               COALESCE(cl.case_seconds, 0)::float8,
               COALESCE(t.treatments, 0),
               COALESCE(a.revenue, 0)::bigint,
               COALESCE(co.collected, 0)::bigint
        FROM people p
        LEFT JOIN staff_memberships m ON m.id = p.staff_id
        LEFT JOIN user_accounts u ON u.id = m.user_id
        LEFT JOIN closed cl ON cl.staff_id = p.staff_id
        LEFT JOIN treated t ON t.staff_id = p.staff_id
        LEFT JOIN attributed a ON a.staff_id = p.staff_id
        LEFT JOIN collected co ON co.staff_id = p.staff_id
        """;

    private string TimeZone => clinic.Value.TimeZone;

    public async Task<IReadOnlyList<RevenueRow>> RevenueAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            RevenueSql,
            AnalyticsSql.WindowParameters(window, TimeZone),
            reader => new RevenueRow(
                reader.GetFieldValue<DateOnly>(0),
                reader.GetString(1),
                reader.NullableString(2),
                reader.ParseEnum<PaymentMethod>(3),
                reader.GetInt64(4),
                reader.GetInt32(5),
                reader.GetInt64(6),
                reader.GetInt32(7)),
            cancellationToken);

    public async Task<InvoicedTotal> InvoicedAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default)
    {
        var rows = await AnalyticsSql.QueryAsync(
            db,
            InvoicedSql,
            AnalyticsSql.WindowParameters(window, TimeZone),
            reader => new InvoicedTotal(reader.GetInt64(0), reader.GetInt32(1)),
            cancellationToken);

        return rows.Single();
    }

    public async Task<IReadOnlyList<ServiceRevenueRow>> ServiceRevenueAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            ServiceRevenueSql,
            AnalyticsSql.WindowParameters(window, TimeZone),
            reader => new ServiceRevenueRow(
                reader.NullableString(0),
                reader.NullableString(1),
                reader.ParseEnum<InvoiceLineKind>(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt64(5),
                reader.GetInt64(6),
                reader.GetInt64(7)),
            cancellationToken);

    public async Task<IReadOnlyList<RetentionCell>> RetentionAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            RetentionSql,
            AnalyticsSql.WindowParameters(window, TimeZone),
            reader => new RetentionCell(
                reader.GetFieldValue<DateOnly>(0),
                reader.GetFieldValue<DateOnly>(1),
                reader.GetInt32(2)),
            cancellationToken);

    public async Task<IReadOnlyList<StaffPerformanceRow>> StaffPerformanceAsync(
        ReportWindow window,
        CancellationToken cancellationToken = default) =>
        await AnalyticsSql.QueryAsync(
            db,
            StaffPerformanceSql,
            AnalyticsSql.WindowParameters(window, TimeZone),
            reader => new StaffPerformanceRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.NullableString(2),
                reader.GetInt32(3),
                reader.GetDouble(4),
                reader.GetInt32(5),
                reader.GetInt64(6),
                reader.GetInt64(7)),
            cancellationToken);
}
