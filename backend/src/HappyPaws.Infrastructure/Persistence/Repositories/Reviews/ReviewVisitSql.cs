using Npgsql;
using NpgsqlTypes;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Reviews;

/// <summary>
/// The SQL behind "a visit that can be reviewed" (<c>docs/contract-extensions.md § 36</c>), kept in
/// one place so the portal's pending list, the invitation pages and the <c>visit_completed</c>
/// reminder cannot judge a visit differently.
/// <para>
/// <b>A visit</b> is a clinic day at one branch for one customer with a closed consultation or a bill
/// that was not voided, for a pet: <c>DueBackSql</c>'s visit (a consultation or a bill that was not
/// voided, credited to the record a merged-away customer was merged into), without the consultation
/// still being written and the counter sale with no pet. The record that names it is the first
/// closed consultation of the day, or else the first such bill.
/// </para>
/// <para>
/// <b>Reviewed</b> and <b>invited</b> compare by <c>(customer, branch, day)</c>, the unique key of
/// both tables, with a merged-away reviewer counted as the record they were merged into.
/// </para>
/// <para>Statuses compare as the text EF writes. Every value is a bound parameter.</para>
/// </summary>
internal static class ReviewVisitSql
{
    /// <summary>Between two pieces of a statement, so neither runs into the other.</summary>
    private const string Nl = "\n";

    /// <summary>
    /// Every visit with a day in <c>[@from, @to)</c>, of <c>@customer</c> when given, as the CTE
    /// <c>visits</c>. The instants bound the scan; the day is the clinic's.
    /// </summary>
    private const string VisitsCte =
        """
        WITH sources AS (
            SELECT COALESCE(cu.merged_into_customer_id, c.customer_id) AS customer_id, c.branch_id,
                   (c.opened_at AT TIME ZONE @tz)::date AS day, c.id AS source_id, 0 AS rank, c.opened_at AS at,
                   c.pet_id, c.appointment_id
            FROM consultations c
            LEFT JOIN customers cu ON cu.id = c.customer_id
            WHERE c.status = 'Closed' AND c.opened_at >= @from AND c.opened_at < @to
            UNION ALL
            SELECT COALESCE(cu.merged_into_customer_id, i.customer_id), i.branch_id,
                   (i.issued_at AT TIME ZONE @tz)::date, i.id, 1, i.issued_at,
                   i.pet_id, (SELECT ic.appointment_id FROM consultations ic WHERE ic.id = i.consultation_id)
            FROM invoices i
            LEFT JOIN customers cu ON cu.id = i.customer_id
            WHERE i.status <> 'Void' AND i.pet_id IS NOT NULL AND i.issued_at >= @from AND i.issued_at < @to),
        visits AS (
            SELECT DISTINCT ON (s.customer_id, s.branch_id, s.day)
                   s.customer_id, s.branch_id, s.day, s.source_id, s.pet_id, s.appointment_id
            FROM sources s
            WHERE (@customer::text IS NULL OR s.customer_id = @customer)
            ORDER BY s.customer_id, s.branch_id, s.day, s.rank, s.at, s.source_id)
        """;

    /// <summary>The booked service, or the first service line on the bill.</summary>
    private const string ServiceName =
        """
        COALESCE(svc.name, (SELECT l.description FROM invoice_lines l
                            WHERE l.invoice_id = v.source_id AND l.kind = 'Service'
                            ORDER BY l.id LIMIT 1))
        """;

    /// <summary>A review of the same visit, by this record or one merged into it.</summary>
    private const string Reviewed =
        """
        EXISTS (SELECT 1 FROM customer_reviews r
                LEFT JOIN customers rc ON rc.id = r.customer_id
                WHERE COALESCE(rc.merged_into_customer_id, r.customer_id) = v.customer_id
                  AND r.branch_id = v.branch_id AND r.visit_on = v.day)
        """;

    /// <summary>An invitation for the same visit.</summary>
    private const string Invited =
        """
        EXISTS (SELECT 1 FROM review_invitations ri
                LEFT JOIN customers ic ON ic.id = ri.customer_id
                WHERE COALESCE(ic.merged_into_customer_id, ri.customer_id) = v.customer_id
                  AND ri.branch_id = v.branch_id AND ri.visit_on = v.day)
        """;

    private const string Joins =
        """
        FROM visits v
        JOIN pets p ON p.id = v.pet_id
        JOIN branches b ON b.id = v.branch_id
        LEFT JOIN appointments a ON a.id = v.appointment_id
        LEFT JOIN clinic_services svc ON svc.id = a.service_id
        """;

    /// <summary>
    /// The visits of one customer, or every visit of a day when <c>@customer</c> is null, newest
    /// first: visit id, customer, branch, branch name, day, pet id, name, species, alive, service,
    /// reviewed.
    /// </summary>
    public const string Visits =
        VisitsCte + Nl +
        "SELECT v.source_id, v.customer_id, v.branch_id, b.name, v.day, p.id, p.name, p.species, p.deceased_at IS NULL, " +
        ServiceName + ", " + Reviewed + Nl +
        Joins + Nl +
        "ORDER BY v.day DESC, v.branch_id";

    /// <summary>The customer, branch and instant of the visit a consultation or bill belongs to.</summary>
    public const string Owner =
        """
        SELECT COALESCE(cu.merged_into_customer_id, c.customer_id), c.branch_id, c.opened_at
        FROM consultations c
        LEFT JOIN customers cu ON cu.id = c.customer_id
        WHERE c.id = @visit_id AND c.status = 'Closed'
        UNION ALL
        SELECT COALESCE(cu.merged_into_customer_id, i.customer_id), i.branch_id, i.issued_at
        FROM invoices i
        LEFT JOIN customers cu ON cu.id = i.customer_id
        WHERE i.id = @visit_id AND i.status <> 'Void' AND i.pet_id IS NOT NULL
        LIMIT 1
        """;

    /// <summary>
    /// The subjects of the <c>visit_completed</c> reminder: visits with no review and no invitation,
    /// of an active customer whose pet is alive, narrowed by branch and species. Visit id, day,
    /// customer, customer name, pet id, pet name, branch, service.
    /// </summary>
    public const string Subjects =
        VisitsCte + Nl +
        "SELECT v.source_id, v.day, c.id, c.display_name, p.id, p.name, v.branch_id, " + ServiceName + Nl +
        Joins + Nl +
        """
        JOIN customers c ON c.id = v.customer_id
        WHERE c.is_active AND c.merged_into_customer_id IS NULL
          AND p.deceased_at IS NULL
          AND (@branch::text IS NULL OR v.branch_id = @branch)
          AND (@species::text IS NULL OR lower(p.species) = lower(@species))
          AND NOT
        """ + " " + Reviewed + Nl +
        "  AND NOT " + Invited;

    /// <summary>The parameters the statements read: the zone, the instants bounding the days, and the filters.</summary>
    public static List<NpgsqlParameter> Parameters(
        string timeZone,
        DateTimeOffset from,
        DateTimeOffset to,
        string? customerId = null,
        string? branchId = null,
        string? species = null) =>
    [
        new("tz", NpgsqlDbType.Text) { Value = timeZone },
        new("from", NpgsqlDbType.TimestampTz) { Value = from.ToUniversalTime() },
        new("to", NpgsqlDbType.TimestampTz) { Value = to.ToUniversalTime() },
        new("customer", NpgsqlDbType.Text) { Value = (object?)customerId ?? DBNull.Value },
        new("branch", NpgsqlDbType.Text) { Value = (object?)branchId ?? DBNull.Value },
        new("species", NpgsqlDbType.Text) { Value = (object?)species ?? DBNull.Value },
    ];

    /// <summary>The instants that bound clinic days <paramref name="firstDay"/> to <paramref name="lastDay"/>, both included.</summary>
    public static (DateTimeOffset From, DateTimeOffset To) DaySpan(DateOnly firstDay, DateOnly lastDay, TimeZoneInfo zone) =>
        (StartOf(firstDay, zone), StartOf(lastDay.AddDays(1), zone));

    private static DateTimeOffset StartOf(DateOnly day, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}
