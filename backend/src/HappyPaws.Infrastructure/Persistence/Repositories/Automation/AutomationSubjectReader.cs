using System.Data.Common;
using HappyPaws.Application.Automation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Automation;
using HappyPaws.Infrastructure.Common;
using HappyPaws.Infrastructure.Persistence.Repositories.Analytics;
using HappyPaws.Infrastructure.Persistence.Repositories.Forecasting;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Automation;

/// <summary>
/// The subjects of each trigger, in hand-written SQL over the clinic's own tables.
/// <para>
/// <b>Vaccines</b>: the latest dose of each vaccine per pet (a newer dose of the same vaccine,
/// compared without case, replaces the older one's due date, as the vaccination card does), whose
/// <c>next_due_on</c> falls in the span. Only pets with a dose due in the span are looked at, which
/// is what the partial index on <c>next_due_on</c> is for.
/// </para>
/// <para>
/// <b>Appointments</b>: booked or confirmed visits starting in the span, walk-ins excluded (they
/// are already in the building).
/// </para>
/// <para>
/// <b>Plan sessions</b>: each session of each plan, at its appointment's start when it was booked
/// and its planned time when not. A session whose appointment was cancelled, missed or already
/// seen is left out.
/// </para>
/// <para>
/// <b>Lapsed customers</b>: <c>DueBackSql.LapsedSubjects</c>, the statement behind
/// <c>GET /staff/customers/due-back?reason=lapsed</c> (<c>docs/contract-extensions.md § 33</c>), for
/// customers whose due-back day falls in the span. A rule with a branch counts visits at that
/// branch only.
/// </para>
/// <para>
/// Every query drops deceased patients and merged customer records, and applies the rule's
/// condition (branch, species, vaccine, service) in SQL. Statuses compare as the text EF writes.
/// </para>
/// <para>
/// Doses and sessions carry <c>pets.deceased_undone_at</c> as <see cref="AutomationSubject.QuietUntil"/>,
/// so a reminder that fell due while a death was recorded by mistake is not sent once it is undone.
/// </para>
/// </summary>
internal sealed class AutomationSubjectReader(HappyPawsDbContext db, IClock clock, IOptions<ClinicOptions> clinic)
    : IAutomationSubjectReader
{
    /// <summary>The branch a pet was last booked at: where its owner is likely to be reminded from.</summary>
    private const string LatestBranch =
        "(SELECT la.branch_id FROM appointments la WHERE la.pet_id = p.id ORDER BY la.starts_at DESC, la.id DESC LIMIT 1)";

    private const string VaccinesSql =
        $"""
        SELECT * FROM (
            SELECT l.id, l.next_due_on, c.id AS customer_id, c.display_name, p.id AS pet_id, p.name,
                   COALESCE(con.branch_id, {LatestBranch}) AS branch_id, l.vaccine_name,
                   p.deceased_undone_at
            FROM (
                SELECT DISTINCT ON (v.pet_id, lower(v.vaccine_name))
                       v.id, v.pet_id, v.vaccine_name, v.next_due_on, v.consultation_id
                FROM pet_vaccinations v
                WHERE v.pet_id IN (SELECT d.pet_id FROM pet_vaccinations d
                                   WHERE d.next_due_on BETWEEN @first AND @last)
                ORDER BY v.pet_id, lower(v.vaccine_name), v.administered_at DESC, v.id DESC
            ) l
            JOIN pets p ON p.id = l.pet_id
            JOIN customers c ON c.id = p.primary_customer_id
            LEFT JOIN consultations con ON con.id = l.consultation_id
            WHERE l.next_due_on BETWEEN @first AND @last
              AND p.deceased_at IS NULL
              AND c.is_active AND c.merged_into_customer_id IS NULL
              AND (@species IS NULL OR lower(p.species) = lower(@species))
              AND (@vaccine IS NULL OR lower(l.vaccine_name) = lower(@vaccine))
        ) s
        WHERE (@branch IS NULL OR s.branch_id = @branch)
        """;

    private const string AppointmentsSql =
        """
        SELECT a.id, a.starts_at, c.id, c.display_name, p.id, p.name, a.branch_id, u.display_name
        FROM appointments a
        JOIN pets p ON p.id = a.pet_id
        JOIN customers c ON c.id = a.customer_id
        LEFT JOIN staff_memberships m ON m.id = a.staff_id
        LEFT JOIN user_accounts u ON u.id = m.user_id
        WHERE a.starts_at BETWEEN @from AND @to
          AND (a.status = 'Booked' OR (a.status = 'Confirmed' AND NOT @unconfirmed_only))
          AND NOT a.is_walk_in
          AND p.deceased_at IS NULL
          AND c.is_active AND c.merged_into_customer_id IS NULL
          AND (@branch IS NULL OR a.branch_id = @branch)
          AND (@service IS NULL OR a.service_id = @service)
          AND (@species IS NULL OR lower(p.species) = lower(@species))
        """;

    private const string SessionsSql =
        $"""
        SELECT * FROM (
            SELECT t.id, (s.item->>'number')::int AS number,
                   COALESCE(a.starts_at, (s.item->>'plannedAt')::timestamptz) AS starts_at,
                   c.id AS customer_id, c.display_name, p.id AS pet_id, p.name,
                   COALESCE(a.branch_id, con.branch_id, {LatestBranch}) AS branch_id,
                   t.title, a.id AS appointment_id, a.status AS appointment_status, t.service_id,
                   p.deceased_undone_at
            FROM treatment_plans t
            CROSS JOIN LATERAL jsonb_array_elements(t.sessions) AS s(item)
            JOIN pets p ON p.id = t.pet_id
            JOIN customers c ON c.id = p.primary_customer_id
            LEFT JOIN appointments a ON a.id = s.item->>'appointmentId'
            LEFT JOIN consultations con ON con.id = t.consultation_id
            WHERE p.deceased_at IS NULL
              AND c.is_active AND c.merged_into_customer_id IS NULL
              AND (@species IS NULL OR lower(p.species) = lower(@species))
              AND (@service IS NULL OR t.service_id = @service)
        ) x
        WHERE x.starts_at BETWEEN @from AND @to
          AND (x.appointment_id IS NULL OR x.appointment_status IN ('Booked', 'Confirmed'))
          AND (@branch IS NULL OR x.branch_id = @branch)
        """;

    public async Task<IReadOnlyList<AutomationSubject>> FindAsync(
        AutomationTrigger trigger,
        SubjectQuery query,
        CancellationToken cancellationToken = default)
    {
        var zone = clock.ClinicTimeZone;

        return trigger switch
        {
            AutomationTrigger.VaccineDue => await AnalyticsSql.QueryAsync(
                db, VaccinesSql, Parameters(query), Vaccine, cancellationToken),

            AutomationTrigger.AppointmentUpcoming => await AnalyticsSql.QueryAsync(
                db, AppointmentsSql, Parameters(query), reader => Appointment(reader, zone), cancellationToken),

            AutomationTrigger.CustomerLapsed => await AnalyticsSql.QueryAsync(
                db,
                DueBackSql.LapsedSubjects,
                DueBackSql.Parameters(
                    clinic.Value.TimeZone,
                    DateOnly.FromDateTime(clock.NowInClinicTime().DateTime),
                    clock.UtcNow,
                    query.BranchId is { } branchId ? [branchId] : null,
                    dueFrom: query.FirstDay,
                    dueTo: query.LastDay,
                    species: query.Species),
                reader => Lapsed(reader, query.BranchId),
                cancellationToken),

            _ => await AnalyticsSql.QueryAsync(
                db, SessionsSql, Parameters(query), reader => Session(reader, zone), cancellationToken),
        };
    }

    private static AutomationSubject Vaccine(DbDataReader reader) => new(
        AutomationSubjectKind.Vaccination,
        reader.GetString(0),
        StartsAt: null,
        reader.GetFieldValue<DateOnly>(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.NullableString(6),
        Label: reader.GetString(7),
        QuietUntil: NullableInstant(reader, 8));

    /// <summary>
    /// A lapsed customer: the subject is the customer, due on the day they would usually have been
    /// back by, so the "already sent" key allows one message per absence; a visit moves the day.
    /// The message names their living pets; the branch is the rule's, or of their latest visit.
    /// </summary>
    private static AutomationSubject Lapsed(DbDataReader reader, string? ruleBranchId) => new(
        AutomationSubjectKind.Customer,
        reader.GetString(0),
        StartsAt: null,
        reader.GetFieldValue<DateOnly>(2),
        reader.GetString(0),
        reader.GetString(1),
        reader.GetString(3),
        reader.GetString(4),
        ruleBranchId ?? reader.NullableString(5));

    private static AutomationSubject Appointment(DbDataReader reader, TimeZoneInfo zone)
    {
        var startsAt = reader.GetFieldValue<DateTimeOffset>(1);

        return new AutomationSubject(
            AutomationSubjectKind.Appointment,
            reader.GetString(0),
            startsAt,
            DayOf(startsAt, zone),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.NullableString(6),
            AppointmentId: reader.GetString(0),
            StaffName: reader.NullableString(7));
    }

    private static AutomationSubject Session(DbDataReader reader, TimeZoneInfo zone)
    {
        var number = reader.GetInt32(1);
        var startsAt = reader.GetFieldValue<DateTimeOffset>(2);

        return new AutomationSubject(
            AutomationSubjectKind.TreatmentPlanSession,
            $"{reader.GetString(0)}:{number}",
            startsAt,
            DayOf(startsAt, zone),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.NullableString(7),
            AppointmentId: reader.NullableString(9),
            Label: reader.GetString(8),
            SessionNumber: number,
            QuietUntil: NullableInstant(reader, 12));
    }

    private static DateTimeOffset? NullableInstant(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);

    private static DateOnly DayOf(DateTimeOffset instant, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

    private static List<NpgsqlParameter> Parameters(SubjectQuery query) =>
    [
        new("from", NpgsqlDbType.TimestampTz) { Value = query.From.ToUniversalTime() },
        new("to", NpgsqlDbType.TimestampTz) { Value = query.To.ToUniversalTime() },
        AnalyticsSql.Date("first", query.FirstDay),
        AnalyticsSql.Date("last", query.LastDay),
        AnalyticsSql.Text("branch", query.BranchId),
        AnalyticsSql.Text("species", query.Species),
        AnalyticsSql.Text("vaccine", query.VaccineName),
        AnalyticsSql.Text("service", query.ServiceId),
        new("unconfirmed_only", NpgsqlDbType.Boolean) { Value = query.UnconfirmedOnly },
    ];
}
