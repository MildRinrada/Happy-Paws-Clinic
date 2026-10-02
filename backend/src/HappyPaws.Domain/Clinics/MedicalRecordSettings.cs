using HappyPaws.Domain.Common;
using HappyPaws.Domain.Consultations;

namespace HappyPaws.Domain.Clinics;

/// <summary>
/// What a finished record must contain and who may put their name to it —
/// <c>PATCH /clinic/settings/medical-record</c>, "ฟิลด์บังคับ แบบฟอร์มตรวจ การ sign".
/// <para>
/// Three settings for the three things that line names, and each one is enforced somewhere rather
/// than merely stored:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="RequiredFields"/> — checked when a case is closed. A diagnosis is always
///     required by the case itself; these are what this clinic asks for on top.
///   </description></item>
///   <item><description>
///     <see cref="ExaminationSystems"/> — the body systems on the examination form. When set, a
///     finding against a system that is not on the form is refused.
///   </description></item>
///   <item><description>
///     <see cref="SignerMustBeCaseVet"/> — whether a record may only be signed by the vet
///     responsible for the case, or by anyone holding <c>record:sign</c>.
///   </description></item>
/// </list>
/// <para>
/// <see cref="OwnerVisibleSections"/> is the fourth, and the one not yet consumed: it is what
/// <c>GET /portal/pets/{id}/records</c> will project by ("เฉพาะส่วนที่คลินิกตั้งค่าให้เจ้าของเห็น"),
/// and that endpoint is not built. The default is empty, so nothing clinical reaches an owner
/// until the clinic says it should.
/// </para>
/// </summary>
public sealed class MedicalRecordSettings : Entity
{
    public const string SingletonId = "set_medical_record";

    /// <summary>The record sections a clinic may open to owners. See <c>docs/assumptions.md § 10</c>.</summary>
    public static readonly IReadOnlyList<string> RecordSections =
    [
        "vitals", "examination", "diagnosis", "treatment", "prescription", "lab_result", "attachments",
    ];

    private readonly List<string> _requiredFields = [];
    private readonly List<string> _examinationSystems = [];
    private readonly List<string> _ownerVisibleSections = [];

    private MedicalRecordSettings()
    {
    }

    /// <summary>Values from <see cref="ConsultationRequirements.All"/>.</summary>
    public IReadOnlyList<string> RequiredFields => _requiredFields;

    /// <summary>Empty means a free-form examination: any body system is accepted.</summary>
    public IReadOnlyList<string> ExaminationSystems => _examinationSystems;

    public IReadOnlyList<string> OwnerVisibleSections => _ownerVisibleSections;

    public bool SignerMustBeCaseVet { get; private set; }

    public static MedicalRecordSettings CreateDefault() => new() { Id = SingletonId };

    /// <summary>True when the examination form has this system on it, or has no fixed list.</summary>
    public bool AllowsExaminationSystem(string bodySystem) =>
        _examinationSystems.Count == 0
        || _examinationSystems.Contains(bodySystem.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Applies a partial update. A null list leaves that setting alone; an empty list clears it.
    /// </summary>
    public void Update(
        IEnumerable<string>? requiredFields = null,
        IEnumerable<string>? examinationSystems = null,
        IEnumerable<string>? ownerVisibleSections = null,
        bool? signerMustBeCaseVet = null)
    {
        if (requiredFields is not null)
        {
            Replace(_requiredFields, requiredFields, ConsultationRequirements.All, "ฟิลด์บังคับ");
        }

        if (examinationSystems is not null)
        {
            var systems = Normalised(examinationSystems);

            if (systems.Any(system => system.Length > 60))
            {
                throw new DomainException("invalid_medical_record_settings", "ชื่อระบบที่ตรวจยาวเกิน 60 ตัวอักษร");
            }

            _examinationSystems.Clear();
            _examinationSystems.AddRange(systems);
        }

        if (ownerVisibleSections is not null)
        {
            Replace(_ownerVisibleSections, ownerVisibleSections, RecordSections, "ส่วนที่เจ้าของเห็นได้");
        }

        SignerMustBeCaseVet = signerMustBeCaseVet ?? SignerMustBeCaseVet;
    }

    private static void Replace(
        List<string> target,
        IEnumerable<string> values,
        IReadOnlyList<string> allowed,
        string label)
    {
        var replacement = Normalised(values);
        var unknown = replacement.Except(allowed, StringComparer.Ordinal).ToList();

        if (unknown.Count > 0)
        {
            throw new DomainException(
                "invalid_medical_record_settings",
                $"{label}ไม่รู้จัก: {string.Join(", ", unknown)}");
        }

        target.Clear();
        target.AddRange(replacement);
    }

    private static List<string> Normalised(IEnumerable<string> values) =>
        [.. values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)];
}
