using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Patients;

/// <summary>Who put a document on the record.</summary>
public enum DocumentSource
{
    /// <summary><c>POST /staff/pets/{id}/documents</c>.</summary>
    Clinic,

    /// <summary><c>POST /portal/documents</c> — "อัปโหลดรูปอาการหรือผลจากคลินิกอื่น".</summary>
    Owner,
}

/// <summary>
/// A file on a patient's record: an X-ray, a referral letter, a photo the owner took of a rash.
/// <para>
/// The row is written when the upload URL is issued and marked uploaded when the bytes arrive. A
/// document whose file never came stays on the record as pending, which says honestly that someone
/// meant to attach something; it is not shown to the owner and has no download link.
/// </para>
/// <para>
/// <see cref="VisibleToOwner"/> is the clinic's choice for its own documents — "เลือกให้เจ้าของเห็นได้"
/// — and always true for what the owner sent: they have already seen it.
/// </para>
/// </summary>
public sealed class PetDocument : Entity
{
    /// <summary>A phone photo or a scanned report; larger is a video, which this is not for.</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/heic", "application/pdf",
    };

    private PetDocument()
    {
    }

    public string PetId { get; private set; } = string.Empty;

    public DocumentSource Source { get; private set; }

    /// <summary>The staff member or customer who added it.</summary>
    public string AddedBy { get; private set; } = string.Empty;

    /// <summary><c>photo</c>, <c>lab_result</c>, <c>referral</c>, <c>imaging</c>, <c>other</c>.</summary>
    public string Kind { get; private set; } = "other";

    public string Title { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    /// <summary>The size the uploader declared; the storage refuses anything larger.</summary>
    public long DeclaredBytes { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public bool VisibleToOwner { get; private set; }

    public string? ConsultationId { get; private set; }

    public DateTimeOffset? UploadedAt { get; private set; }

    public long? SizeBytes { get; private set; }

    public bool IsUploaded => UploadedAt is not null;

    public static readonly IReadOnlySet<string> Kinds = new HashSet<string>(StringComparer.Ordinal)
    {
        "photo", "lab_result", "referral", "imaging", "other",
    };

    public static PetDocument Start(
        string petId,
        DocumentSource source,
        string addedBy,
        string fileName,
        string contentType,
        long declaredBytes,
        string? kind = null,
        string? title = null,
        bool visibleToOwner = false,
        string? consultationId = null)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw Invalid("รับเฉพาะรูป JPEG PNG WEBP HEIC และไฟล์ PDF");
        }

        if (declaredBytes is <= 0 or > MaxBytes)
        {
            throw Invalid("ไฟล์ต้องมีขนาดไม่เกิน 10 MB");
        }

        var cleanName = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(cleanName) || cleanName.Length > 200)
        {
            throw Invalid("ชื่อไฟล์ไม่ถูกต้อง");
        }

        var resolvedKind = string.IsNullOrWhiteSpace(kind) ? "other" : kind.Trim();
        if (!Kinds.Contains(resolvedKind))
        {
            throw Invalid($"ประเภทเอกสารต้องเป็น {string.Join(", ", Kinds)}");
        }

        var id = EntityId.New(EntityId.Document);

        return new PetDocument
        {
            Id = id,
            PetId = petId,
            Source = source,
            AddedBy = addedBy,
            Kind = resolvedKind,
            Title = string.IsNullOrWhiteSpace(title) ? cleanName : title.Trim(),
            FileName = cleanName,
            ContentType = contentType.ToLowerInvariant(),
            DeclaredBytes = declaredBytes,

            // The file name is not part of the key. It came from a client, and a key made from it
            // would put the client's choice of characters into a path.
            StorageKey = $"pets/{petId}/{id}",
            VisibleToOwner = source == DocumentSource.Owner || visibleToOwner,
            ConsultationId = consultationId,
        };
    }

    public void MarkUploaded(long sizeBytes, DateTimeOffset at)
    {
        UploadedAt ??= at;
        SizeBytes = sizeBytes;
    }

    private static DomainException Invalid(string message) => new("invalid_document", message);
}
