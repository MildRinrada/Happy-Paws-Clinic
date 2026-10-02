using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Patients;

/// <summary>One visit in a plan: when it is planned, and the appointment that holds it, if booked.</summary>
public sealed record TreatmentPlanSession(int Number, DateTimeOffset PlannedAt, string? AppointmentId = null);

/// <summary>
/// A course of treatment over several visits — <c>POST /staff/pets/{id}/treatment-plans</c>,
/// "แผนหลายครั้ง · ผูกกับนัดและการเตือน".
/// <para>
/// The plan is the clinical intent: what is being treated, how many sessions, how far apart. The
/// appointments are the calendar. Each session points at the appointment that holds it, so the
/// plan can say which sessions are booked and the appointments carry the plan's series id back.
/// A plan made without booking has sessions with no appointment yet; that is the plan for a course
/// whose dates the owner still has to confirm.
/// </para>
/// </summary>
public sealed class TreatmentPlan : Entity
{
    public const int MaxSessions = 52;

    private readonly List<TreatmentPlanSession> _sessions = [];

    private TreatmentPlan()
    {
    }

    public string PetId { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>The visit the plan was made in, when there was one.</summary>
    public string? ConsultationId { get; private set; }

    /// <summary>The template it was started from, when there was one. Copied, not linked.</summary>
    public string? TemplateId { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>The series id the booked appointments share. Null when nothing was booked.</summary>
    public string? SeriesId { get; private set; }

    public string CreatedByStaffId { get; private set; } = string.Empty;

    public IReadOnlyList<TreatmentPlanSession> Sessions => _sessions;

    public static TreatmentPlan Create(
        string petId,
        string title,
        string serviceId,
        IReadOnlyList<DateTimeOffset> plannedAt,
        string createdByStaffId,
        string? consultationId = null,
        string? templateId = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw Invalid("ต้องระบุชื่อแผนการรักษา");
        }

        if (plannedAt.Count is < 2 or > MaxSessions)
        {
            throw Invalid($"แผนการรักษาต้องมี 2 ถึง {MaxSessions} ครั้ง");
        }

        var plan = new TreatmentPlan
        {
            Id = EntityId.New(EntityId.TreatmentPlan),
            PetId = petId,
            Title = title.Trim(),
            ServiceId = serviceId,
            ConsultationId = consultationId,
            TemplateId = templateId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedByStaffId = createdByStaffId,
        };

        plan._sessions.AddRange(plannedAt.Order().Select((at, index) => new TreatmentPlanSession(index + 1, at)));

        return plan;
    }

    /// <summary>
    /// Ties each session to the appointment booked for it. The appointments must be the same course,
    /// one per session, in time order.
    /// </summary>
    public void LinkAppointments(string seriesId, IReadOnlyList<(string AppointmentId, DateTimeOffset StartsAt)> appointments)
    {
        if (appointments.Count != _sessions.Count)
        {
            throw Invalid("จำนวนนัดไม่ตรงกับจำนวนครั้งในแผน");
        }

        var ordered = appointments.OrderBy(item => item.StartsAt).ToList();

        for (var i = 0; i < _sessions.Count; i++)
        {
            _sessions[i] = _sessions[i] with { AppointmentId = ordered[i].AppointmentId, PlannedAt = ordered[i].StartsAt };
        }

        SeriesId = seriesId;
    }

    private static DomainException Invalid(string message) => new("invalid_treatment_plan", message);
}
