using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Patients;

/// <summary>
/// A patient — <c>pet_luna</c>.
/// <para>
/// Care is many-to-many and revocable, so ownership is <b>not</b> a column here; it lives in
/// <see cref="PetOwnership"/>. <see cref="PrimaryCustomerId"/> records who the record was opened
/// under ("ผูกเจ้าของหลัก") and is who an appointment is billed to by default — it is not the
/// access-control answer.
/// </para>
/// </summary>
public sealed class Pet : Entity
{
    private Pet()
    {
    }

    /// <summary>Hospital number issued at registration ("ออก HN อัตโนมัติ").</summary>
    public string HospitalNumber { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Species { get; private set; } = string.Empty;

    public string? Breed { get; private set; }

    public string PrimaryCustomerId { get; private set; } = string.Empty;

    /// <summary>
    /// The implanted chip's number — one of the four things <c>GET /staff/pets/{id}</c> is specified
    /// to return ("สายพันธุ์ น้ำหนักล่าสุด แพ้ยา ไมโครชิป").
    /// <para>
    /// Nullable because most patients do not have one, and unique across the clinic where present:
    /// a chip number identifies exactly one animal in the world, so two patients sharing one is a
    /// data-entry error rather than a coincidence.
    /// </para>
    /// </summary>
    public string? MicrochipNumber { get; private set; }

    /// <summary>
    /// Set by <c>POST /staff/pets/{id}/deceased</c> — "ปิดเรกคอร์ดอย่างสุภาพ · หยุดแจ้งเตือนทุกชนิด".
    /// Booking refuses; the history stays readable.
    /// </summary>
    public DateTimeOffset? DeceasedAt { get; private set; }

    public bool IsDeceased => DeceasedAt is not null;

    /// <summary>
    /// When a recorded death was last undone as a mistake — <c>POST /staff/pets/{id}/deceased/undo</c>.
    /// Reminders about vaccines and planned sessions whose send time fell before this are not sent
    /// late. See <c>docs/contract-extensions.md § 29</c>.
    /// </summary>
    public DateTimeOffset? DeceasedUndoneAt { get; private set; }

    /// <summary>Recorded by staff. Not in the API reference; see <c>docs/contract-extensions.md § 28</c>.</summary>
    public PetSex Sex { get; private set; }

    /// <summary>The day it was born, or the clinic's best guess when <see cref="BirthDateEstimated"/>.</summary>
    public DateOnly? BirthDate { get; private set; }

    /// <summary>True when <see cref="BirthDate"/> is a guess, e.g. from the teeth of an adopted adult.</summary>
    public bool BirthDateEstimated { get; private set; }

    /// <summary>Neutered or spayed. Null means nobody knows yet, which is not the same as "no".</summary>
    public bool? Neutered { get; private set; }

    /// <summary>The earliest birth date taken, as a guard against a mistyped year.</summary>
    public static readonly DateOnly EarliestBirthDate = new(1900, 1, 1);

    /// <summary>What the family calls it — "น้องลูลู่". The owner's, not the clinic's.</summary>
    public string? Nickname { get; private set; }

    /// <summary>The document holding the pet's photo, one of its own record's images.</summary>
    public string? PhotoDocumentId { get; private set; }

    /// <summary>What it eats, as the owner describes it.</summary>
    public string? Diet { get; private set; }

    /// <summary>How it behaves — "กลัวเสียงดัง", "กัดเวลาตัดเล็บ". What the nurse wants to know before opening the carrier.</summary>
    public string? Behaviour { get; private set; }

    /// <summary>
    /// <c>PATCH /portal/pets/{id}</c> — "แก้ได้เฉพาะรูป ชื่อเล่น อาหาร พฤติกรรม". A null argument leaves
    /// that field alone and an empty one clears it. Nothing clinical can be reached from here.
    /// </summary>
    public void UpdateOwnerProfile(string? nickname = null, string? photoDocumentId = null, string? diet = null, string? behaviour = null)
    {
        if (IsDeceased)
        {
            throw new StateConflictException(nameof(Pet), "deceased", nameof(UpdateOwnerProfile));
        }

        Nickname = nickname is null ? Nickname : Bounded(nickname, 60, "ชื่อเล่น");
        PhotoDocumentId = photoDocumentId is null ? PhotoDocumentId : Cleared(photoDocumentId);
        Diet = diet is null ? Diet : Bounded(diet, 500, "อาหาร");
        Behaviour = behaviour is null ? Behaviour : Bounded(behaviour, 1_000, "พฤติกรรม");
    }

    private static string? Cleared(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Bounded(string value, int max, string label) =>
        Cleared(value) is { } text && text.Length > max
            ? throw new DomainException("invalid_pet", $"{label}ยาวได้ไม่เกิน {max} ตัวอักษร")
            : Cleared(value);

    public static Pet Create(
        string hospitalNumber,
        string name,
        string species,
        string primaryCustomerId,
        string? breed = null,
        string? microchipNumber = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("invalid_pet", "ต้องระบุชื่อสัตว์เลี้ยง");
        }

        if (string.IsNullOrWhiteSpace(species))
        {
            throw new DomainException("invalid_pet", "ต้องระบุชนิดสัตว์");
        }

        return new Pet
        {
            Id = EntityId.New(EntityId.Pet),
            HospitalNumber = hospitalNumber.Trim(),
            Name = name.Trim(),
            Species = species.Trim(),
            Breed = breed?.Trim(),
            MicrochipNumber = string.IsNullOrWhiteSpace(microchipNumber) ? null : microchipNumber.Trim(),
            PrimaryCustomerId = primaryCustomerId,
        };
    }

    public void MarkDeceased(DateTimeOffset at) => DeceasedAt ??= at;

    /// <summary>
    /// Takes back a death recorded by mistake — <c>POST /staff/pets/{id}/deceased/undo</c>. The
    /// record is open again from <paramref name="at"/>; what the death cancelled stays cancelled.
    /// Returns the date of death that was undone. See <c>docs/contract-extensions.md § 29</c>.
    /// </summary>
    public DateTimeOffset UndoDeceased(DateTimeOffset at)
    {
        var was = DeceasedAt ?? throw new StateConflictException(nameof(Pet), "alive", "undo_deceased");

        DeceasedAt = null;
        DeceasedUndoneAt = at;

        return was;
    }

    /// <summary>
    /// Staff's correction of who this patient is — <c>PATCH /staff/pets/{id}</c>. Not in the API
    /// reference; see <c>docs/contract-extensions.md § 28</c>.
    /// <para>
    /// A closed record can still have its name, species, breed, chip, sex and birth date put right,
    /// because the history it carries is read and printed under those. Its neuter status cannot
    /// change: after death no procedure happens, and a clinical fact on a closed record is
    /// corrected by an amendment to the signed medical record, not here. The owner's four fields
    /// are not reachable from this method at all.
    /// </para>
    /// </summary>
    /// <param name="revision">What to change; null fields are left alone.</param>
    /// <param name="today">The clinic's today, against which a birth date may not be in the future.</param>
    public void Revise(PetRevision revision, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(revision);

        if (revision.BirthDate is not null && revision.ClearBirthDate)
        {
            throw new DomainException("invalid_pet", "ส่งวันเกิดพร้อมกับลบวันเกิดในคำขอเดียวกันไม่ได้");
        }

        if (revision.Neutered is not null && revision.ClearNeutered)
        {
            throw new DomainException("invalid_pet", "ส่งสถานะทำหมันพร้อมกับลบสถานะทำหมันในคำขอเดียวกันไม่ได้");
        }

        var neutered = revision.ClearNeutered ? null : revision.Neutered ?? Neutered;

        if (IsDeceased && neutered != Neutered)
        {
            throw new StateConflictException(nameof(Pet), "deceased", "change_neutered");
        }

        if (revision.BirthDate is { } born && (born > today || born < EarliestBirthDate))
        {
            throw new DomainException(
                "invalid_pet",
                born > today ? "วันเกิดต้องไม่อยู่ในอนาคต" : "วันเกิดต้องไม่ก่อนปี 1900");
        }

        var birthDate = revision.ClearBirthDate ? null : revision.BirthDate ?? BirthDate;
        var estimated = birthDate is null ? false : revision.BirthDateEstimated ?? (revision.BirthDate is null && BirthDateEstimated);

        if (birthDate is null && revision.BirthDateEstimated is true)
        {
            throw new DomainException("invalid_pet", "ระบุว่าเป็นวันเกิดโดยประมาณได้เมื่อมีวันเกิดเท่านั้น");
        }

        // Every value is worked out before any is assigned, so a refusal changes nothing.
        var name = revision.Name is null ? Name : Required(revision.Name, 120, "ชื่อสัตว์เลี้ยง");
        var species = revision.Species is null ? Species : Required(revision.Species, 60, "ชนิดสัตว์");
        var breed = revision.Breed is null ? Breed : Bounded(revision.Breed, 120, "สายพันธุ์");
        var microchip = revision.MicrochipNumber is null ? MicrochipNumber : Bounded(revision.MicrochipNumber, 40, "เลขไมโครชิป");

        Name = name;
        Species = species;
        Breed = breed;
        MicrochipNumber = microchip;
        Sex = revision.Sex ?? Sex;
        BirthDate = birthDate;
        BirthDateEstimated = estimated;
        Neutered = neutered;
    }

    private static string Required(string value, int max, string label) =>
        Bounded(value, max, label) ?? throw new DomainException("invalid_pet", $"ต้องระบุ{label}");
}
