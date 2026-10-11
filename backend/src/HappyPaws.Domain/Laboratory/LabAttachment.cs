using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Laboratory;

/// <summary>
/// A file on a lab request: the PDF an outside lab sent, a photo of a printed report
/// (<c>docs/contract-extensions.md § 54</c>).
/// <para>
/// Part of the request, not of the patient's documents. It belongs to the request the lab answered,
/// is read and written under the lab's scopes, and is staff-only: the owner sees results once a vet
/// has read them, never the raw report. The file itself is in the file storage under
/// <see cref="StorageKey"/>, a key <see cref="LabFile"/> made for this request.
/// </para>
/// </summary>
public sealed class LabAttachment
{
    private LabAttachment()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string LabRequestId { get; private set; } = string.Empty;

    /// <summary>The storage key, without the <c>storage:</c> marker.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    /// <summary>The name the file had on the uploader's computer. Shown, never used as a path.</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>Worked out from the key, which was made for exactly one type.</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>The size the uploader gave. The upload URL refused anything larger than they asked for.</summary>
    public long? SizeBytes { get; private set; }

    public DateTimeOffset AttachedAt { get; private set; }

    public string AttachedByStaffId { get; private set; } = string.Empty;

    internal static LabAttachment Create(
        string labRequestId,
        string storageKey,
        string fileName,
        long? sizeBytes,
        string staffId,
        DateTimeOffset at)
    {
        if (LabFile.ContentTypeOf(storageKey) is not { } contentType || LabFile.RequestOf(storageKey) != labRequestId)
        {
            throw Invalid("ไฟล์นี้ไม่ได้อัปโหลดให้คำขอตรวจนี้");
        }

        var cleanName = Path.GetFileName(fileName?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(cleanName) || cleanName.Length > LabFile.MaxFileNameLength)
        {
            throw Invalid("ชื่อไฟล์ไม่ถูกต้อง");
        }

        if (sizeBytes is <= 0 or > LabFile.MaxBytes)
        {
            throw Invalid("ไฟล์ต้องมีขนาดไม่เกิน 10 MB");
        }

        return new LabAttachment
        {
            Id = EntityId.New(EntityId.LabAttachment),
            LabRequestId = labRequestId,
            StorageKey = storageKey,
            FileName = cleanName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            AttachedAt = at,
            AttachedByStaffId = staffId,
        };
    }

    internal static DomainException Invalid(string message) => new("invalid_lab_attachment", message);
}
