using HappyPaws.Application.Clinics;
using HappyPaws.Application.Crm;
using HappyPaws.Domain.Crm;
using HappyPaws.Infrastructure.Common;
using HappyPaws.Infrastructure.Persistence.Repositories.Analytics;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Crm;

/// <summary>
/// Segment membership in one SQL statement over customers, visits, payments, the points ledger and
/// packages.
/// <para>
/// <b>A visit</b> is a consultation or a bill that was not voided, the same as the retention report;
/// a merged-away customer's visits and money count for the record they were merged into, and only
/// active, unmerged customers are members. <b>Spend</b> is money collected less money refunded over
/// the last 365 clinic days. <b>Reachable</b> means the latest marketing consent is a yes and the
/// LINE address still follows the clinic: exactly who the broadcast's consent gate would let through.
/// With a branch, only customers who have visited that branch are members, and visits and spend
/// count only there.
/// </para>
/// <para>
/// Every value is a bound parameter; the text is a constant. A condition that was not given is
/// switched off by its parameter (<c>@x IS NULL OR ...</c>), so one statement serves every mix.
/// </para>
/// </summary>
internal sealed class SegmentQuery(
    HappyPawsDbContext db,
    IOptions<ClinicOptions> clinic,
    IClinicSettingsStore settings) : ISegmentQuery
{
    private const string MembersSql =
        """
        WITH visits AS (
            SELECT COALESCE(cu.merged_into_customer_id, v.customer_id) AS customer_id, MAX(v.day) AS last_day
            FROM (
                SELECT c.customer_id, (c.opened_at AT TIME ZONE @tz)::date AS day, c.branch_id
                FROM consultations c
                UNION ALL
                SELECT i.customer_id, (i.issued_at AT TIME ZONE @tz)::date, i.branch_id
                FROM invoices i
                WHERE i.status <> 'Void') v
            LEFT JOIN customers cu ON cu.id = v.customer_id
            WHERE (@branch::text IS NULL OR v.branch_id = @branch)
            GROUP BY 1),
        spend AS (
            SELECT COALESCE(cu.merged_into_customer_id, m.customer_id) AS customer_id, SUM(m.amount) AS amount
            FROM (
                SELECT i.customer_id, i.branch_id, p.amount_satang AS amount
                FROM invoice_payments p
                JOIN invoices i ON i.id = p.invoice_id
                WHERE p.collected_at >= (@spend_from::timestamp AT TIME ZONE @tz)
                UNION ALL
                SELECT i.customer_id, i.branch_id, -r.amount_satang
                FROM invoice_refunds r
                JOIN invoices i ON i.id = r.invoice_id
                WHERE r.refunded_at >= (@spend_from::timestamp AT TIME ZONE @tz)) m
            LEFT JOIN customers cu ON cu.id = m.customer_id
            WHERE (@branch::text IS NULL OR m.branch_id = @branch)
            GROUP BY 1),
        points AS (
            SELECT customer_id, SUM(points) AS lifetime
            FROM loyalty_entries
            WHERE kind IN ('Earn', 'Reverse')
            GROUP BY customer_id),
        members AS (
            SELECT c.id,
                   (c.line_user_id IS NOT NULL AND c.line_unfollowed_at IS NULL
                    AND COALESCE((SELECT cc.granted FROM customer_consents cc
                                  WHERE cc.customer_id = c.id AND cc.purpose = 'Marketing'
                                  ORDER BY cc.recorded_at DESC, cc.id DESC LIMIT 1), false)) AS reachable
            FROM customers c
            LEFT JOIN visits v ON v.customer_id = c.id
            LEFT JOIN spend s ON s.customer_id = c.id
            LEFT JOIN points pt ON pt.customer_id = c.id
            WHERE c.is_active AND c.merged_into_customer_id IS NULL
              AND (@branch::text IS NULL OR v.last_day IS NOT NULL)
              AND (cardinality(@species) = 0 OR EXISTS (
                    SELECT 1 FROM pet_ownerships o
                    JOIN pets p ON p.id = o.pet_id
                    WHERE o.customer_id = c.id AND o.revoked_at IS NULL AND p.deceased_at IS NULL
                      AND lower(p.species) = ANY(@species)))
              AND (@inactive_cutoff::date IS NULL OR (v.last_day IS NOT NULL AND v.last_day <= @inactive_cutoff))
              AND (@visited_cutoff::date IS NULL OR v.last_day > @visited_cutoff)
              AND (@min_spend::bigint IS NULL OR COALESCE(s.amount, 0) >= @min_spend)
              AND (cardinality(@tiers) = 0 OR (CASE WHEN COALESCE(pt.lifetime, 0) >= @gold_min THEN 'Gold'
                                                    WHEN COALESCE(pt.lifetime, 0) >= @silver_min THEN 'Silver'
                                                    ELSE 'Member' END) = ANY(@tiers))
              AND (@has_package::boolean IS NULL OR @has_package = EXISTS (
                    SELECT 1 FROM customer_packages cp
                    WHERE cp.customer_id = c.id AND cp.cancelled_at IS NULL AND cp.activated_at IS NOT NULL
                      AND cp.used_count < cp.total_uses
                      AND (cp.expires_on IS NULL OR cp.expires_on >= @today))))
        """;

    private const string CountSql =
        MembersSql +
        """

        SELECT COUNT(*)::int, (COUNT(*) FILTER (WHERE reachable))::int FROM members
        """;

    private const string ReachableSql =
        MembersSql +
        """

        SELECT id FROM members WHERE reachable ORDER BY id LIMIT @limit
        """;

    public async Task<SegmentCounts> CountAsync(
        SegmentCriteria criteria,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var rows = await AnalyticsSql.QueryAsync(
            db,
            CountSql,
            await ParametersAsync(criteria, today, cancellationToken),
            reader => new SegmentCounts(reader.GetInt32(0), reader.GetInt32(1)),
            cancellationToken);

        return rows.Single();
    }

    public async Task<IReadOnlyList<string>> ReachableMembersAsync(
        SegmentCriteria criteria,
        DateOnly today,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var parameters = await ParametersAsync(criteria, today, cancellationToken);
        parameters.Add(new NpgsqlParameter("limit", NpgsqlDbType.Integer) { Value = limit });

        return await AnalyticsSql.QueryAsync(db, ReachableSql, parameters, reader => reader.GetString(0), cancellationToken);
    }

    /// <summary>
    /// The level thresholds are the clinic's current ones (<c>docs/contract-extensions.md § 25</c>),
    /// the same the loyalty page labels a customer with.
    /// </summary>
    private async Task<List<NpgsqlParameter>> ParametersAsync(SegmentCriteria criteria, DateOnly today, CancellationToken cancellationToken)
    {
        var loyalty = await settings.LoyaltyAsync(cancellationToken);
        var silver = loyalty.SilverMinPoints;
        var gold = loyalty.GoldMinPoints;

        return
        [
            AnalyticsSql.Text("tz", clinic.Value.TimeZone),
            AnalyticsSql.Date("today", today),
            AnalyticsSql.Date("spend_from", today.AddDays(1 - SegmentCriteria.SpendWindowDays)),
            AnalyticsSql.Text("branch", criteria.BranchId),
            new("species", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = criteria.Species.Select(species => species.Trim().ToLowerInvariant()).ToArray(),
            },
            NullableDate("inactive_cutoff", criteria.InactiveDays is { } inactive ? today.AddDays(-inactive) : null),
            NullableDate("visited_cutoff", criteria.VisitedWithinDays is { } within ? today.AddDays(-within) : null),
            new("min_spend", NpgsqlDbType.Bigint) { Value = (object?)criteria.MinSpendSatang ?? DBNull.Value },
            new("tiers", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = criteria.Tiers.Select(tier => tier.ToString()).ToArray(),
            },
            new("silver_min", NpgsqlDbType.Integer) { Value = silver },
            new("gold_min", NpgsqlDbType.Integer) { Value = gold },
            new("has_package", NpgsqlDbType.Boolean) { Value = (object?)criteria.HasActivePackage ?? DBNull.Value },
        ];
    }

    private static NpgsqlParameter NullableDate(string name, DateOnly? value) =>
        new(name, NpgsqlDbType.Date) { Value = value.HasValue ? value.Value : DBNull.Value };
}

/// <summary>
/// The catalogue service behind each line of a bill: the treatment's service on a case bill, or the
/// line's own source when it is a service id. Mirrors the service revenue report.
/// </summary>
internal sealed class BilledServiceLookup(HappyPawsDbContext db) : IBilledServiceLookup
{
    private const string Sql =
        """
        SELECT l.id, COALESCE(ct.service_id, direct.id)
        FROM invoice_lines l
        LEFT JOIN consultation_treatments ct ON ct.id = l.source_id
        LEFT JOIN clinic_services direct ON direct.id = l.source_id
        WHERE l.invoice_id = @invoice AND COALESCE(ct.service_id, direct.id) IS NOT NULL
        """;

    public async Task<IReadOnlyDictionary<string, string>> ServiceIdsByLineAsync(
        string invoiceId,
        CancellationToken cancellationToken = default)
    {
        var rows = await AnalyticsSql.QueryAsync(
            db,
            Sql,
            [AnalyticsSql.Text("invoice", invoiceId)],
            reader => (Line: reader.GetString(0), Service: reader.GetString(1)),
            cancellationToken);

        return rows.ToDictionary(row => row.Line, row => row.Service, StringComparer.Ordinal);
    }
}
