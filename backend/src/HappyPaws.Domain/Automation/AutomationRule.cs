using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Automation;

/// <summary>
/// A reminder rule — <c>POST /staff/automations</c>, "rule = trigger + condition + action ทำงานฝั่งเซิร์ฟเวอร์",
/// for example "ครบกำหนดวัคซีน 14 วัน → ส่ง LINE".
/// <list type="bullet">
/// <item><b>Trigger</b>: <see cref="Trigger"/> and its <see cref="Timing"/>.</item>
/// <item><b>Condition</b>: which subjects count, narrowed by <see cref="BranchId"/>,
/// <see cref="Species"/>, <see cref="VaccineName"/>, <see cref="ServiceId"/> and
/// <see cref="UnconfirmedOnly"/>. Every one is optional.</item>
/// <item><b>Action</b>: send <see cref="TemplateKey"/> on <see cref="Channel"/>, through the same
/// consent gate as every other message.</item>
/// </list>
/// <para>
/// A rule never stores who it will reach. That is computed from the clinic's own records each
/// time it is evaluated, by one evaluator for the real run, the simulation and the upcoming list,
/// so the three cannot disagree.
/// </para>
/// </summary>
public sealed class AutomationRule : Entity
{
    public const int NameMax = 120;
    public const int FilterMax = 200;

    /// <summary>Three months ahead covers every vaccine reminder a clinic sends.</summary>
    public const int MaxOffsetDays = 90;

    /// <summary>A month after the due day, for a follow-up on an overdue dose.</summary>
    public const int MinVaccineOffsetDays = -30;

    /// <summary>A visit reminder further ahead than a month is forgotten before the day.</summary>
    public const int MaxVisitOffsetDays = 30;

    /// <summary>A week, the longest "T-minus hours" anyone writes.</summary>
    public const int MaxOffsetHours = 168;

    private AutomationRule()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public AutomationTrigger Trigger { get; private set; }

    /// <summary>Days before the subject's clinic day; null for an hour-based rule.</summary>
    public int? OffsetDays { get; private set; }

    /// <summary>Hours before the subject's instant; null for a day-based rule.</summary>
    public int? OffsetHours { get; private set; }

    /// <summary>Clinic time of day a day-based rule sends at.</summary>
    public TimeOnly? SendTime { get; private set; }

    /// <summary>
    /// The branch whose subjects the rule covers, and the branch it belongs to; null for the whole
    /// clinic.
    /// </summary>
    public string? BranchId { get; private set; }

    /// <summary>Only pets of this species, as the clinic writes it ("สุนัข").</summary>
    public string? Species { get; private set; }

    /// <summary>Only doses of this vaccine, compared without case ("rabies" matches "Rabies").</summary>
    public string? VaccineName { get; private set; }

    /// <summary>Only appointments or plans of this service.</summary>
    public string? ServiceId { get; private set; }

    /// <summary>Appointments only: skip the ones the owner has already confirmed.</summary>
    public bool UnconfirmedOnly { get; private set; }

    public AutomationChannel Channel { get; private set; }

    public string TemplateKey { get; private set; } = string.Empty;

    public bool Enabled { get; private set; }

    /// <summary>Null for the clinic's starter rules, which were seeded, not written by anyone.</summary>
    public string? CreatedByStaffId { get; private set; }

    public AutomationTiming Timing => new(OffsetDays, OffsetHours, SendTime);

    /// <summary>The kind of record this rule's reminders are about.</summary>
    public AutomationSubjectKind SubjectKind => Trigger switch
    {
        AutomationTrigger.VaccineDue => AutomationSubjectKind.Vaccination,
        AutomationTrigger.AppointmentUpcoming => AutomationSubjectKind.Appointment,
        _ => AutomationSubjectKind.TreatmentPlanSession,
    };

    public static AutomationRule Create(
        string name,
        AutomationTrigger trigger,
        int? offsetDays,
        int? offsetHours,
        TimeOnly? sendTime,
        AutomationChannel channel,
        string templateKey,
        bool enabled,
        string? createdByStaffId,
        string? branchId = null,
        string? species = null,
        string? vaccineName = null,
        string? serviceId = null,
        bool unconfirmedOnly = false)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMax)
        {
            throw Invalid($"ชื่อกติกาต้องมี 1 ถึง {NameMax} ตัวอักษร");
        }

        if (string.IsNullOrWhiteSpace(templateKey))
        {
            throw Invalid("ต้องระบุเทมเพลตของข้อความ");
        }

        EnsureTiming(trigger, offsetDays, offsetHours, sendTime);

        if (vaccineName is not null && trigger != AutomationTrigger.VaccineDue)
        {
            throw Invalid("เงื่อนไขชื่อวัคซีนใช้ได้กับการเตือนวัคซีนเท่านั้น");
        }

        if (serviceId is not null && trigger == AutomationTrigger.VaccineDue)
        {
            throw Invalid("เงื่อนไขบริการใช้ได้กับนัดและแผนการรักษาเท่านั้น");
        }

        if (unconfirmedOnly && trigger != AutomationTrigger.AppointmentUpcoming)
        {
            throw Invalid("เงื่อนไขเฉพาะนัดที่ยังไม่ยืนยันใช้ได้กับการเตือนนัดเท่านั้น");
        }

        return new AutomationRule
        {
            Id = EntityId.New(EntityId.Automation),
            Name = name.Trim(),
            Trigger = trigger,
            OffsetDays = offsetDays,
            OffsetHours = offsetHours,
            SendTime = offsetHours is null ? sendTime ?? AutomationTiming.DefaultSendTime : null,
            Channel = channel,
            TemplateKey = templateKey,
            Enabled = enabled,
            CreatedByStaffId = createdByStaffId,
            BranchId = Trimmed(branchId),
            Species = Trimmed(species),
            VaccineName = Trimmed(vaccineName),
            ServiceId = Trimmed(serviceId),
            UnconfirmedOnly = unconfirmedOnly,
        };
    }

    private static void EnsureTiming(AutomationTrigger trigger, int? offsetDays, int? offsetHours, TimeOnly? sendTime)
    {
        if ((offsetDays is null) == (offsetHours is null))
        {
            throw Invalid("ต้องระบุ offset_days หรือ offset_hours อย่างใดอย่างหนึ่ง");
        }

        if (offsetHours is { } hours)
        {
            if (trigger == AutomationTrigger.VaccineDue)
            {
                // A due date has no time of day, so "24 hours before" it has no meaning.
                throw Invalid("การเตือนวัคซีนนับเป็นวัน ใช้ offset_days");
            }

            if (hours is < 1 or > MaxOffsetHours)
            {
                throw Invalid($"offset_hours ต้องอยู่ระหว่าง 1 ถึง {MaxOffsetHours}");
            }

            if (sendTime is not null)
            {
                throw Invalid("send_time ใช้คู่กับ offset_days เท่านั้น");
            }

            return;
        }

        var days = offsetDays!.Value;
        var (min, max) = trigger == AutomationTrigger.VaccineDue
            ? (MinVaccineOffsetDays, MaxOffsetDays)
            : (0, MaxVisitOffsetDays);

        if (days < min || days > max)
        {
            throw Invalid($"offset_days ต้องอยู่ระหว่าง {min} ถึง {max}");
        }
    }

    private static DomainException Invalid(string message) => new("invalid_automation", message);

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
