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
}
