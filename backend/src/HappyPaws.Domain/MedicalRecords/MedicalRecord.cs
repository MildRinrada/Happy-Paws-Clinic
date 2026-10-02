using System.Security.Cryptography;
using System.Text;
using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.MedicalRecords;

/// <summary>
/// The signed clinical document a consultation produces — <c>GET /staff/medical-records/{id}</c>,
/// "ฉบับที่ sign แล้วพร้อมลายเซ็นและเวลา".
/// <para>
/// A record holds a <b>snapshot</b>, not a pointer. <see cref="ContentJson"/> is the case as it
/// read at the moment of signing, serialised whole. Rendering a signed record by re-reading the
/// consultation would mean the document silently changed whenever anything it referenced did —
/// a renamed service, a corrected pet name — and a signature over a moving target signs nothing.
/// </para>
/// <para>
/// <see cref="ContentHash"/> makes that concrete. It covers the content, the signer and the
/// signing time, so a row altered directly in the database no longer verifies. It is not a
/// cryptographic signature and does not claim to be — an operator with database access can
/// recompute it — but it turns a silent edit into a visible inconsistency, which is what
/// "ลบไม่ได้ · ทุกฉบับเก็บถาวรพร้อมผู้ลงนาม" asks for at this stage.
/// </para>
/// </summary>
public sealed class MedicalRecord : Entity
{
    private readonly List<MedicalRecordAmendment> _amendments = [];

    private MedicalRecord()
    {
    }

    public string ConsultationId { get; private set; } = string.Empty;

    public string PetId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    public string BranchId { get; private set; } = string.Empty;

    public MedicalRecordStatus Status { get; private set; } = MedicalRecordStatus.Draft;

    /// <summary>The case as it stood when it was signed. Never modified.</summary>
    public string ContentJson { get; private set; } = string.Empty;

    public DateTimeOffset SignedAt { get; private set; }

    /// <summary>The vet who signed. A record has exactly one signer, whatever happens after.</summary>
    public string SignedByStaffId { get; private set; } = string.Empty;

    public string ContentHash { get; private set; } = string.Empty;

    public IReadOnlyList<MedicalRecordAmendment> Amendments => _amendments;

    /// <summary>
    /// What the record says <i>now</i> — the newest amendment, or the original if there are none.
    /// The original always remains readable underneath ("เก็บฉบับเดิมไว้เสมอ").
    /// </summary>
    public string CurrentContentJson =>
        _amendments.Count == 0
            ? ContentJson
            : _amendments.OrderByDescending(amendment => amendment.CreatedAt).First().ContentJson;

    /// <summary>
    /// <c>POST /staff/consultations/{id}/close</c> signs here. There is no unsigned constructor:
    /// a medical record in this system exists only as a signed document.
    /// </summary>
    public static MedicalRecord Sign(
        string consultationId,
        string petId,
        string customerId,
        string branchId,
        string contentJson,
        string signedByStaffId,
        DateTimeOffset signedAt)
    {
        if (string.IsNullOrWhiteSpace(contentJson))
        {
            throw new DomainException("invalid_record", "ไม่มีเนื้อหาเวชระเบียนให้ลงนาม");
        }

        return new MedicalRecord
        {
            Id = EntityId.New(EntityId.MedicalRecord),
            ConsultationId = consultationId,
            PetId = petId,
            CustomerId = customerId,
            BranchId = branchId,
            ContentJson = contentJson,
            Status = MedicalRecordStatus.Signed,
            SignedAt = signedAt,
            SignedByStaffId = signedByStaffId,
            ContentHash = Hash(contentJson, signedByStaffId, signedAt),
        };
    }

    /// <summary>
    /// <c>POST /staff/medical-records/{id}/amendments</c> — "แก้ไขแบบมีร่องรอย · เก็บฉบับเดิมไว้เสมอ".
    /// <para>
    /// Appends a new version and leaves everything before it intact. The record's status becomes
    /// <c>amended</c> and stays there: a document corrected twice is not more amended than one
    /// corrected once, and the count is in the list.
    /// </para>
    /// </summary>
    public MedicalRecordAmendment Amend(
        string contentJson,
        string changesJson,
        string reason,
        string byStaffId,
        DateTimeOffset at)
    {
        if (Status == MedicalRecordStatus.Draft)
        {
            throw new StateConflictException(nameof(MedicalRecord), Status.ToString(), nameof(Amend));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            // An amendment without a stated reason is indistinguishable from tampering when it is
            // read back years later, which is the only time anyone reads it.
            throw new DomainException("invalid_amendment", "ต้องระบุเหตุผลในการแก้ไข");
        }

        var amendment = MedicalRecordAmendment.Create(Id, contentJson, changesJson, reason, byStaffId, at);

        _amendments.Add(amendment);
        Status = MedicalRecordStatus.Amended;

        return amendment;
    }

    /// <summary>True when the stored content still matches the hash written at signing.</summary>
    public bool VerifyIntegrity() => ContentHash == Hash(ContentJson, SignedByStaffId, SignedAt);

    /// <summary>
    /// The hash covers the content, the signer and the signing time — the three things a
    /// signature is meant to bind together.
    /// <para>
    /// The timestamp is reduced to milliseconds before hashing, and that is not cosmetic.
    /// A <see cref="DateTimeOffset"/> carries 100-nanosecond ticks while PostgreSQL
    /// <c>timestamptz</c> stores microseconds, so a value hashed in memory and re-hashed after a
    /// round trip would differ in its last digits and every stored record would report itself as
    /// tampered with. Milliseconds survive any storage precision this system will meet.
    /// </para>
    /// </summary>
    private static string Hash(string content, string signedByStaffId, DateTimeOffset signedAt) =>
        Convert.ToHexStringLower(
            SHA256.HashData(
                Encoding.UTF8.GetBytes($"{content}\n{signedByStaffId}\n{signedAt.ToUnixTimeMilliseconds()}")));
}
