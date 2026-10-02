using HappyPaws.Domain.Common;
using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.Domain.Consultations;

/// <summary>
/// An open case — <c>con_771</c>. The aggregate root of the examination room.
/// <para>
/// Everything a vet enters during a visit hangs off this root, and every write goes through a
/// method here that first asks whether the case is still open. That single choke point is what
/// makes "เวชระเบียนเป็น append-only หลัง sign" true in practice rather than by convention: once
/// <see cref="Close"/> has run there is no path back into the aggregate, and the caller is told to
/// use an amendment.
/// </para>
/// <para>
/// The parts are separate child collections rather than one notes field because the contract
/// addresses them separately — vitals may be filled by an assistant under <c>vitals:write</c>,
/// prescriptions require <c>rx:write</c>, and treatments become invoice lines. Collapsing them
/// into free text would make those distinctions unenforceable.
/// </para>
/// </summary>
public sealed class Consultation : Entity
{
    private readonly List<VitalsReading> _vitals = [];
    private readonly List<ExaminationFinding> _findings = [];
    private readonly List<Diagnosis> _diagnoses = [];
    private readonly List<Treatment> _treatments = [];
    private readonly List<Prescription> _prescriptions = [];

    private Consultation()
    {
    }

    public string BranchId { get; private set; } = string.Empty;

    public string PetId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>The visit this case was opened from. Null when opened straight from a walk-in ticket.</summary>
    public string? AppointmentId { get; private set; }

    /// <summary>The queue ticket that was called. Null when the case was opened from the calendar.</summary>
    public string? QueueTicketId { get; private set; }

    /// <summary>The vet responsible for the case and, at close, for the signature.</summary>
    public string VetStaffId { get; private set; } = string.Empty;

    public ConsultationStatus Status { get; private set; } = ConsultationStatus.Draft;

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>The signed record this case produced. Null while the case is open.</summary>
    public string? MedicalRecordId { get; private set; }

    /// <summary>What the owner reported — carried over from the appointment's "อาการที่แจ้ง".</summary>
    public string? ChiefComplaint { get; private set; }

    /// <summary>History as told by the owner.</summary>
    public string? Subjective { get; private set; }

    /// <summary>What the vet observed, beyond the structured examination rows.</summary>
    public string? Objective { get; private set; }

    public string? Assessment { get; private set; }

    /// <summary>Treatment plan, follow-up, owner advice.</summary>
    public string? Plan { get; private set; }

    public IReadOnlyList<VitalsReading> Vitals => _vitals;

    public IReadOnlyList<ExaminationFinding> Findings => _findings;

    public IReadOnlyList<Diagnosis> Diagnoses => _diagnoses;

    public IReadOnlyList<Treatment> Treatments => _treatments;

    public IReadOnlyList<Prescription> Prescriptions => _prescriptions;

    public bool IsOpen => Status == ConsultationStatus.Draft;

    /// <summary>The most recent set of vitals, which is what "current weight" means.</summary>
    public VitalsReading? LatestVitals => _vitals.OrderByDescending(reading => reading.RecordedAt).FirstOrDefault();

    public Diagnosis? PrimaryDiagnosis => _diagnoses.FirstOrDefault(diagnosis => diagnosis.IsPrimary);

    /// <summary>What the treatments on this case will bill for — "เข้าบิลอัตโนมัติ".</summary>
    public Money TreatmentCharges => _treatments.Aggregate(Money.Zero, (total, line) => total + line.LineTotal);

    /// <summary><c>POST /staff/consultations</c> — "เปิดเคสจากคิวหรือจากนัด".</summary>
    public static Consultation Open(
        string branchId,
        string petId,
        string customerId,
        string vetStaffId,
        DateTimeOffset now,
        string? appointmentId = null,
        string? queueTicketId = null,
        string? chiefComplaint = null) =>
        new()
        {
            Id = EntityId.New(EntityId.Consultation),
            BranchId = branchId,
            PetId = petId,
            CustomerId = customerId,
            VetStaffId = vetStaffId,
            AppointmentId = appointmentId,
            QueueTicketId = queueTicketId,
            OpenedAt = now,
            ChiefComplaint = Trimmed(chiefComplaint),
        };

    /// <summary>
    /// <c>PATCH /staff/consultations/{id}</c> — "autosave ฉบับร่างทุก 10 วินาที".
    /// <para>
    /// A null argument leaves that field alone; an empty string clears it. Autosave sends whatever
    /// the vet has touched, and a partial save that blanked the fields it did not mention would
    /// erase the examination notes every time the weight box changed.
    /// </para>
    /// </summary>
    public void UpdateDraft(
        string? chiefComplaint = null,
        string? subjective = null,
        string? objective = null,
        string? assessment = null,
        string? plan = null)
    {
        RequireOpen();

        ChiefComplaint = chiefComplaint is null ? ChiefComplaint : Trimmed(chiefComplaint);
        Subjective = subjective is null ? Subjective : Trimmed(subjective);
        Objective = objective is null ? Objective : Trimmed(objective);
        Assessment = assessment is null ? Assessment : Trimmed(assessment);
        Plan = plan is null ? Plan : Trimmed(plan);
    }

    public VitalsReading AddVitals(
        string recordedByStaffId,
        DateTimeOffset recordedAt,
        decimal? weightKg = null,
        decimal? temperatureC = null,
        int? heartRateBpm = null,
        int? respiratoryRateBpm = null,
        string? note = null)
    {
        RequireOpen();

        var reading = VitalsReading.Record(
            Id, PetId, recordedByStaffId, recordedAt, weightKg, temperatureC, heartRateBpm, respiratoryRateBpm, note);

        _vitals.Add(reading);

        return reading;
    }

    /// <summary>
    /// Replaces the whole examination. The contract describes one form covering every body system,
    /// so a second submission is the corrected form, not a second examination.
    /// </summary>
    public void ReplaceExamination(IEnumerable<(string BodySystem, bool IsNormal, string? Note)> findings)
    {
        RequireOpen();

        var replacement = findings
            .Select(finding => ExaminationFinding.Record(Id, finding.BodySystem, finding.IsNormal, finding.Note))
            .ToList();

        if (replacement.Count == 0)
        {
            throw new DomainException("invalid_examination", "ต้องมีผลตรวจอย่างน้อยหนึ่งระบบ");
        }

        if (replacement.Select(finding => finding.BodySystem).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != replacement.Count)
        {
            throw new DomainException("invalid_examination", "ระบบที่ตรวจซ้ำกัน");
        }

        _findings.Clear();
        _findings.AddRange(replacement);
    }

    /// <summary>
    /// Adds a diagnosis, demoting the previous primary when this one claims that place. A case with
    /// two primary diagnoses is a case nobody can summarise.
    /// </summary>
    public Diagnosis AddDiagnosis(string description, bool isPrimary, string? code = null, string? note = null)
    {
        RequireOpen();

        // The first diagnosis on a case is primary whether or not the caller said so — otherwise a
        // case can end up with secondary diagnoses and no principal one.
        var primary = isPrimary || _diagnoses.Count == 0;

        if (primary)
        {
            foreach (var existing in _diagnoses.Where(diagnosis => diagnosis.IsPrimary))
            {
                existing.Demote();
            }
        }

        var added = Diagnosis.Record(Id, description, primary, code, note);
        _diagnoses.Add(added);

        return added;
    }

    public Treatment AddTreatment(
        string name,
        int quantity,
        long unitPriceSatang,
        string performedByStaffId,
        DateTimeOffset performedAt,
        string? serviceId = null,
        string? note = null)
    {
        RequireOpen();

        var treatment = Treatment.Record(
            Id, name, quantity, unitPriceSatang, performedByStaffId, performedAt, serviceId, note);

        _treatments.Add(treatment);

        return treatment;
    }

    public Prescription AddPrescription(
        string prescribedByStaffId,
        DateTimeOffset prescribedAt,
        string drugName,
        string dose,
        string frequency,
        int durationDays,
        int quantity,
        string? instructions = null,
        string? allergyAcknowledgement = null)
    {
        RequireOpen();

        var prescription = Prescription.Write(
            Id,
            PetId,
            prescribedByStaffId,
            prescribedAt,
            drugName,
            dose,
            frequency,
            durationDays,
            quantity,
            instructions,
            allergyAcknowledgement);

        _prescriptions.Add(prescription);

        return prescription;
    }

    /// <summary>
    /// <c>POST /staff/consultations/{id}/close</c>. The record id is supplied by the caller because
    /// signing the record is the same transaction — see <c>CloseConsultationHandler</c>.
    /// </summary>
    public void Close(DateTimeOffset now, string medicalRecordId)
    {
        RequireOpen();

        if (_diagnoses.Count == 0)
        {
            // A signed record with no diagnosis is not a clinical document. This is the one
            // completeness rule worth enforcing here; everything else a vet may legitimately leave
            // blank.
            throw new DomainException("incomplete_consultation", "ต้องมีคำวินิจฉัยอย่างน้อยหนึ่งรายการก่อนปิดเคส");
        }

        Status = ConsultationStatus.Closed;
        ClosedAt = now;
        MedicalRecordId = medicalRecordId;
    }

    /// <summary>
    /// The choke point every mutation passes through.
    /// <para>
    /// Raises <c>423 record_locked</c> rather than <c>409 state_conflict</c>. The case is not in
    /// the wrong state for this action — it is finished and its record is signed, and the contract
    /// reserves <c>record_locked</c> for exactly that. The difference matters to the caller: 409
    /// invites a retry, 423 says post an amendment.
    /// </para>
    /// </summary>
    private void RequireOpen()
    {
        if (!IsOpen)
        {
            throw new RecordLockedException(MedicalRecordId ?? Id);
        }
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
