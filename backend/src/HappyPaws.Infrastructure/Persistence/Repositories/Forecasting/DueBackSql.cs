using HappyPaws.Application.Forecasting;
using HappyPaws.Domain.Staff;
using Npgsql;
using NpgsqlTypes;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Forecasting;

/// <summary>
/// The SQL behind the due-back list and the <c>customer_lapsed</c> reminder
/// (<c>docs/contract-extensions.md § 33</c>), kept in one place so the endpoint and the automation
/// cannot judge "lapsed" differently.
/// <para>
/// <b>A visit</b> is a clinic day with a consultation or a bill that was not voided, credited to the
/// record a merged-away customer was merged into, the same as <c>SegmentQuery</c>. With branches
/// (<c>@all_branches</c> false) only visits at <c>@branches</c> count, and a customer with none there
/// is not on the list.
/// </para>
/// <para>
/// <b>Lapsed</b> is <see cref="LapsedRule"/>: at least <c>@min_visits</c> visit days, the median gap
/// by <c>percentile_cont(0.5)</c>, allowed <c>CEIL(GREATEST(@factor × median, median + @slack))</c>
/// days after the last visit, due back on the last visit plus that. Every constant is a parameter
/// filled from <see cref="LapsedRule"/>.
/// </para>
/// <para>
/// Statuses compare as the text EF writes. Every value is a bound parameter; the text is constant.
/// </para>
/// </summary>
internal static class DueBackSql
{
    /// <summary>Between two pieces of a statement, so neither runs into the other.</summary>
    private const string Nl = "\n";

    /// <summary>Visit days per customer at the branches counted. Opens a <c>WITH</c>.</summary>
    private const string VisitDays =
        """
        WITH visit_days AS (
            SELECT DISTINCT COALESCE(cu.merged_into_customer_id, v.customer_id) AS customer_id, v.day, v.branch_id
            FROM (
                SELECT c.customer_id, (c.opened_at AT TIME ZONE @tz)::date AS day, c.branch_id
                FROM consultations c
                UNION ALL
                SELECT i.customer_id, (i.issued_at AT TIME ZONE @tz)::date, i.branch_id
                FROM invoices i
                WHERE i.status <> 'Void') v
            LEFT JOIN customers cu ON cu.id = v.customer_id
            WHERE (@all_branches OR v.branch_id = ANY(@branches))),
        last_visits AS (
            SELECT customer_id, MAX(day) AS last_day
            FROM visit_days
            GROUP BY customer_id)
        """;

    /// <summary>No booked or confirmed appointment from now on, for a customer (<c>c</c>).</summary>
    private const string NoUpcomingForCustomer =
        """
        NOT EXISTS (SELECT 1 FROM appointments a
                    WHERE a.customer_id = c.id AND a.starts_at >= @now AND a.status IN ('Booked', 'Confirmed'))
        """;

    /// <summary>Customers past their own usual gap, as the CTE <c>due</c>.</summary>
    private const string LapsedCte =
        VisitDays +
        """
        ,
        days AS (
            SELECT DISTINCT customer_id, day FROM visit_days),
        gaps AS (
            SELECT customer_id, day, day - LAG(day) OVER (PARTITION BY customer_id ORDER BY day) AS gap
            FROM days),
        rhythm AS (
            SELECT customer_id,
                   COUNT(*)::int AS visits,
                   MAX(day) AS last_day,
                   (percentile_cont(0.5) WITHIN GROUP (ORDER BY gap::float8) FILTER (WHERE gap IS NOT NULL))::float8 AS median_gap
            FROM gaps
            GROUP BY customer_id
            HAVING COUNT(*) >= @min_visits),
        due AS (
            SELECT r.customer_id, r.visits, r.last_day, r.median_gap,
                   CEIL(GREATEST(@factor * r.median_gap, r.median_gap + @slack))::int AS allowed_days,
                   r.last_day + CEIL(GREATEST(@factor * r.median_gap, r.median_gap + @slack))::int AS due_on
            FROM rhythm r)
        """;

    /// <summary>Who of <c>due</c> is on the list: active, a living pet (of <c>@species</c>), nothing booked.</summary>
    private const string LapsedWhere =
        """
        WHERE c.is_active AND c.merged_into_customer_id IS NULL
          AND d.due_on <= @due_to
          AND (@due_from::date IS NULL OR d.due_on >= @due_from)
          AND EXISTS (SELECT 1 FROM pet_ownerships o
                      JOIN pets p ON p.id = o.pet_id
                      WHERE o.customer_id = c.id AND o.revoked_at IS NULL AND p.deceased_at IS NULL
                        AND (@species::text IS NULL OR lower(p.species) = lower(@species)))
          AND
        """ + " " + NoUpcomingForCustomer;

    /// <summary>Most recently due first, after the cursor.</summary>
    private const string Paging =
        """

          AND (@after_due::date IS NULL OR d.due_on < @after_due OR (d.due_on = @after_due AND d.customer_id > @after_id))
        ORDER BY d.due_on DESC, d.customer_id
        LIMIT @limit
        """;

    /// <summary>One page of lapsed customers.</summary>
    public const string LapsedPage =
        LapsedCte + Nl +
        """
        SELECT d.customer_id, c.display_name, d.due_on, d.last_day, d.visits, d.median_gap, d.allowed_days
        FROM due d
        JOIN customers c ON c.id = d.customer_id
        """ + Nl + LapsedWhere + Paging;

    /// <summary>
    /// Lapsed customers due back inside a span, for the reminder rule: with the pet the message
    /// names (living ones, by name, joined) and the branch of their latest visit.
    /// </summary>
    public const string LapsedSubjects =
        LapsedCte + Nl +
        """
        SELECT d.customer_id, c.display_name, d.due_on,
               (SELECT p.id FROM pet_ownerships o JOIN pets p ON p.id = o.pet_id
                WHERE o.customer_id = c.id AND o.revoked_at IS NULL AND p.deceased_at IS NULL
                ORDER BY p.name, p.id LIMIT 1) AS pet_id,
               (SELECT string_agg(p.name, ', ' ORDER BY p.name, p.id) FROM pet_ownerships o JOIN pets p ON p.id = o.pet_id
                WHERE o.customer_id = c.id AND o.revoked_at IS NULL AND p.deceased_at IS NULL) AS pet_names,
               (SELECT vd.branch_id FROM visit_days vd WHERE vd.customer_id = d.customer_id
                ORDER BY vd.day DESC, vd.branch_id LIMIT 1) AS branch_id
        FROM due d
        JOIN customers c ON c.id = d.customer_id
        """ + Nl + LapsedWhere;

    /// <summary>The latest dose of each vaccine per living pet, overdue and with nothing booked, as <c>overdue</c>.</summary>
    private const string OverdueCte =
        VisitDays +
        """
        ,
        latest AS (
            SELECT DISTINCT ON (v.pet_id, lower(v.vaccine_name)) v.pet_id, v.vaccine_name, v.next_due_on
            FROM pet_vaccinations v
            WHERE v.pet_id IN (SELECT o.pet_id FROM pet_vaccinations o WHERE o.next_due_on < @today)
            ORDER BY v.pet_id, lower(v.vaccine_name), v.administered_at DESC, v.id DESC),
        overdue AS (
            SELECT p.primary_customer_id AS customer_id, p.id AS pet_id, p.name AS pet_name, p.species,
                   l.vaccine_name, l.next_due_on
            FROM latest l
            JOIN pets p ON p.id = l.pet_id
            WHERE l.next_due_on < @today
              AND p.deceased_at IS NULL
              AND NOT EXISTS (SELECT 1 FROM appointments a
                              WHERE a.pet_id = p.id AND a.starts_at >= @now AND a.status IN ('Booked', 'Confirmed'))),
        due AS (
            SELECT o.customer_id, MIN(o.next_due_on) AS due_on
            FROM overdue o
            GROUP BY o.customer_id)
        """;

    /// <summary>One page of owners with an overdue dose.</summary>
    public const string OverduePage =
        OverdueCte + Nl +
        """
        SELECT d.customer_id, c.display_name, d.due_on, lv.last_day
        FROM due d
        JOIN customers c ON c.id = d.customer_id
        LEFT JOIN last_visits lv ON lv.customer_id = d.customer_id
        WHERE c.is_active AND c.merged_into_customer_id IS NULL
          AND (@all_branches OR lv.last_day IS NOT NULL)
        """ + Paging;

    /// <summary>The overdue doses of these customers' pets.</summary>
    public const string OverduePets =
        OverdueCte + Nl +
        """
        SELECT o.customer_id, o.pet_id, o.pet_name, o.species, o.vaccine_name, o.next_due_on
        FROM overdue o
        WHERE o.customer_id = ANY(@ids)
        """;

    /// <summary>The living pets of these customers.</summary>
    public const string LivingPets =
        """
        SELECT o.customer_id, p.id, p.name, p.species, NULL::text, NULL::date
        FROM pet_ownerships o
        JOIN pets p ON p.id = o.pet_id
        WHERE o.customer_id = ANY(@ids) AND o.revoked_at IS NULL AND p.deceased_at IS NULL
        """;

    /// <summary>Every parameter the statements above read, so each one can bind the same list.</summary>
    public static List<NpgsqlParameter> Parameters(
        string timeZone,
        DateOnly today,
        DateTimeOffset now,
        IReadOnlyCollection<string>? branchIds,
        DateOnly? dueFrom = null,
        DateOnly? dueTo = null,
        string? species = null,
        int? limit = null,
        DateOnly? afterDue = null,
        string? afterId = null,
        IReadOnlyCollection<string>? customerIds = null) =>
    [
        new("tz", NpgsqlDbType.Text) { Value = timeZone },
        new("today", NpgsqlDbType.Date) { Value = today },
        new("now", NpgsqlDbType.TimestampTz) { Value = now.ToUniversalTime() },
        new("all_branches", NpgsqlDbType.Boolean) { Value = branchIds is null },
        new("branches", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = branchIds?.ToArray() ?? [] },
        new("min_visits", NpgsqlDbType.Integer) { Value = LapsedRule.MinVisits },
        new("factor", NpgsqlDbType.Double) { Value = LapsedRule.Factor },
        new("slack", NpgsqlDbType.Integer) { Value = LapsedRule.SlackDays },
        NullableDate("due_from", dueFrom),
        new("due_to", NpgsqlDbType.Date) { Value = dueTo ?? today.AddDays(-1) },
        new("species", NpgsqlDbType.Text) { Value = (object?)species ?? DBNull.Value },
        new("limit", NpgsqlDbType.Integer) { Value = (object?)limit ?? DBNull.Value },
        NullableDate("after_due", afterDue),
        new("after_id", NpgsqlDbType.Text) { Value = (object?)afterId ?? DBNull.Value },
        new("ids", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = customerIds?.ToArray() ?? [] },
    ];

    /// <summary>The roster's vets: a vet by type, or anyone in the veterinary department.</summary>
    public const string VetCondition =
        $"(m.staff_type = 'veterinarian' OR m.department = '{Departments.Veterinary}')";

    private static NpgsqlParameter NullableDate(string name, DateOnly? value) =>
        new(name, NpgsqlDbType.Date) { Value = value.HasValue ? value.Value : DBNull.Value };
}
