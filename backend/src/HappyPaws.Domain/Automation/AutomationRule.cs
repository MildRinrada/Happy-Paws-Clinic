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

    /// <summary>
    /// A lapsed customer is written to at least a day after the day they would usually have been back
    /// (<c>offset_days</c> -1), and at most three months after (-90). Before that day they are not
    /// late, so a positive offset has nothing to say.
    /// </summary>
    public const int MinLapsedOffsetDays = -90;

    public const int MaxLapsedOffsetDays = -1;

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

    /// <summary>
    /// When the rule was deleted (<c>DELETE /staff/automations/{id}</c>, <c>docs/contract-extensions.md § 19</c>).
    /// <para>
    /// A deleted rule is kept, switched off, and left out of every list and every run. Its row stays
    /// because <c>automation_dispatches</c> point at it: the delivery log of a reminder sent last
    /// month must still say which rule sent it, under the name it had then.
    /// </para>
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Who deleted it.</summary>
    public string? DeletedByStaffId { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public AutomationTiming Timing => new(OffsetDays, OffsetHours, SendTime);

    /// <summary>The kind of record this rule's reminders are about.</summary>
    public AutomationSubjectKind SubjectKind => Trigger switch
    {
        AutomationTrigger.VaccineDue => AutomationSubjectKind.Vaccination,
        AutomationTrigger.AppointmentUpcoming => AutomationSubjectKind.Appointment,
        AutomationTrigger.CustomerLapsed => AutomationSubjectKind.Customer,
        _ => AutomationSubjectKind.TreatmentPlanSession,
    };

    /// <summary>
    /// The <c>offset_days</c> a trigger accepts. Vaccines may follow up after the due day; visits and
    /// sessions are reminded before; a lapsed customer only after (<c>docs/contract-extensions.md § 33</c>).
    /// </summary>
    public static (int Min, int Max) OffsetDaysRange(AutomationTrigger trigger) => trigger switch
    {
        AutomationTrigger.VaccineDue => (MinVaccineOffsetDays, MaxOffsetDays),
        AutomationTrigger.CustomerLapsed => (MinLapsedOffsetDays, MaxLapsedOffsetDays),
        _ => (0, MaxVisitOffsetDays),
    };

    /// <summary>
    /// False for the triggers whose subject is a day with no time in it (a due date, the day a
    /// customer was due back), where "24 hours before" has no meaning.
    /// </summary>
    public static bool AllowsHours(AutomationTrigger trigger) =>
        trigger is not (AutomationTrigger.VaccineDue or AutomationTrigger.CustomerLapsed);

    /// <summary>True for the triggers whose subjects belong to a service (a visit, a plan).</summary>
    public static bool AllowsService(AutomationTrigger trigger) =>
        trigger is AutomationTrigger.AppointmentUpcoming or AutomationTrigger.TreatmentPlanSession;

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
        if (string.IsNullOrWhiteSpace(templateKey))
        {
            throw Invalid("ต้องระบุเทมเพลตของข้อความ");
        }

        EnsureValid(name, trigger, offsetDays, offsetHours, sendTime, vaccineName, serviceId, unconfirmedOnly);

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

    /// <summary>
    /// Replaces everything about the rule that may be edited: its name, when it sends, which
    /// subjects count, and the channel. <c>PATCH /staff/automations/{id}</c>, not in the API
    /// reference; see <c>docs/contract-extensions.md § 19</c>.
    /// <para>
    /// <b>What it watches is fixed.</b> The trigger, and with it the template and the kind of record
    /// the "already sent" key names, never changes; a rule that should watch something else is a new
    /// rule. That is what keeps an edit from re-sending: the key is <c>(rule, subject, due date)</c>,
    /// none of which an edit touches, so a subject this rule has already reminded is already sent
    /// whatever the new timing says.
    /// </para>
    /// </summary>
    public void Revise(
        string name,
        int? offsetDays,
        int? offsetHours,
        TimeOnly? sendTime,
        AutomationChannel channel,
        string? branchId,
        string? species,
        string? vaccineName,
        string? serviceId,
        bool unconfirmedOnly)
    {
        EnsureLive();
        EnsureValid(name, Trigger, offsetDays, offsetHours, sendTime, vaccineName, serviceId, unconfirmedOnly);

        Name = name.Trim();
        OffsetDays = offsetDays;
        OffsetHours = offsetHours;
        SendTime = offsetHours is null ? sendTime ?? AutomationTiming.DefaultSendTime : null;
        Channel = channel;
        BranchId = Trimmed(branchId);
        Species = Trimmed(species);
        VaccineName = Trimmed(vaccineName);
        ServiceId = Trimmed(serviceId);
        UnconfirmedOnly = unconfirmedOnly;
    }

    /// <summary>Switches the rule on or off. Off, the scheduler skips it; its history stays.</summary>
    public void SetEnabled(bool enabled)
    {
        EnsureLive();
        Enabled = enabled;
    }

    /// <summary>
    /// Deletes the rule: off, out of every list and run, and kept so its history still names it.
    /// </summary>
    public void Delete(string? staffId, DateTimeOffset at)
    {
        EnsureLive();
        Enabled = false;
        DeletedAt = at;
        DeletedByStaffId = staffId;
    }

    private void EnsureLive()
    {
        if (IsDeleted)
        {
            throw new DomainException("automation_deleted", "กติกานี้ถูกลบแล้ว");
        }
    }

    private static void EnsureValid(
        string name,
        AutomationTrigger trigger,
        int? offsetDays,
        int? offsetHours,
        TimeOnly? sendTime,
        string? vaccineName,
        string? serviceId,
        bool unconfirmedOnly)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMax)
        {
            throw Invalid($"ชื่อกติกาต้องมี 1 ถึง {NameMax} ตัวอักษร");
        }

        EnsureTiming(trigger, offsetDays, offsetHours, sendTime);

        if (Trimmed(vaccineName) is not null && trigger != AutomationTrigger.VaccineDue)
        {
            throw Invalid("เงื่อนไขชื่อวัคซีนใช้ได้กับการเตือนวัคซีนเท่านั้น");
        }

        if (Trimmed(serviceId) is not null && !AllowsService(trigger))
        {
            throw Invalid("เงื่อนไขบริการใช้ได้กับนัดและแผนการรักษาเท่านั้น");
        }

        if (unconfirmedOnly && trigger != AutomationTrigger.AppointmentUpcoming)
        {
            throw Invalid("เงื่อนไขเฉพาะนัดที่ยังไม่ยืนยันใช้ได้กับการเตือนนัดเท่านั้น");
        }
    }

    private static void EnsureTiming(AutomationTrigger trigger, int? offsetDays, int? offsetHours, TimeOnly? sendTime)
    {
        if ((offsetDays is null) == (offsetHours is null))
        {
            throw Invalid("ต้องระบุ offset_days หรือ offset_hours อย่างใดอย่างหนึ่ง");
        }

        if (offsetHours is { } hours)
        {
            if (!AllowsHours(trigger))
            {
                // A due date has no time of day, so "24 hours before" it has no meaning.
                throw Invalid(trigger == AutomationTrigger.VaccineDue
                    ? "การเตือนวัคซีนนับเป็นวัน ใช้ offset_days"
                    : "การชวนลูกค้าที่หายไปนับเป็นวัน ใช้ offset_days");
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
        var (min, max) = OffsetDaysRange(trigger);

        if (days < min || days > max)
        {
            throw Invalid($"offset_days ต้องอยู่ระหว่าง {min} ถึง {max}");
        }
    }

    private static DomainException Invalid(string message) => new("invalid_automation", message);

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
