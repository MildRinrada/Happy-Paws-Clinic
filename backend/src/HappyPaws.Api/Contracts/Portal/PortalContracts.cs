namespace HappyPaws.Api.Contracts.Portal;

/// <summary>
/// <c>PATCH /portal/pets/{id}</c> — "แก้ได้เฉพาะรูป ชื่อเล่น อาหาร พฤติกรรม". Send only what
/// changes; an empty string clears it. There is no field here for anything else, so nothing else
/// can be changed. <c>photo_document_id</c> is an image uploaded with <c>POST /portal/documents</c>.
/// </summary>
public sealed record UpdateMyPetRequest(
    string? Nickname = null,
    string? PhotoDocumentId = null,
    string? Diet = null,
    string? Behaviour = null);

/// <summary><c>POST /portal/notifications/read</c> — the ids to mark read, or none for all.</summary>
public sealed record MarkNotificationsReadRequest(IReadOnlyList<string>? Ids = null);

/// <summary>
/// <c>POST /portal/documents</c> — "อัปโหลดรูปอาการหรือผลจากคลินิกอื่น". Describes the file to
/// come; the bytes go to the URL in the answer.
/// </summary>
public sealed record UploadOwnerDocumentRequest(
    string PetId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Kind = null,
    string? Title = null);
