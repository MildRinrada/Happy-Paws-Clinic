using System.Text.Json;
using HappyPaws.Domain.MedicalRecords;

namespace HappyPaws.Application.MedicalRecords.Dtos;

/// <summary>
/// A signed record — <c>GET /staff/medical-records/{id}</c>,
/// "ฉบับที่ sign แล้วพร้อมลายเซ็นและเวลา".
/// <para>
/// <see cref="Content"/> is what the record says <i>now</i>; <see cref="OriginalContent"/> is what
/// was signed. They differ only once an amendment exists, and both are returned because a reader
/// reviewing a correction needs to see what it corrected.
/// </para>
/// <para>
/// <see cref="IntegrityVerified"/> reports whether the stored content still matches the hash taken
/// at signing. Returning it rather than hiding it is deliberate: a record whose hash no longer
/// verifies has been altered outside this application, and the person reading it should be told.
/// </para>
/// </summary>
public sealed record MedicalRecordDto(
    string Id,
    uint Version,
    string Status,
    string ConsultationId,
    string PetId,
    string CustomerId,
    string BranchId,
    DateTimeOffset SignedAt,
    string SignedByStaffId,
    string ContentHash,
    bool IntegrityVerified,
    JsonElement Content,
    JsonElement OriginalContent,
    IReadOnlyList<AmendmentDto> Amendments)
{
    public static MedicalRecordDto From(MedicalRecord record) =>
        new(
            record.Id,
            record.Version,
            ToWireStatus(record.Status),
            record.ConsultationId,
            record.PetId,
            record.CustomerId,
            record.BranchId,
            record.SignedAt,
            record.SignedByStaffId,
            record.ContentHash,
            record.VerifyIntegrity(),
            Parse(record.CurrentContentJson),
            Parse(record.ContentJson),
            [.. record.Amendments.OrderBy(amendment => amendment.CreatedAt).Select(AmendmentDto.From)]);

    public static string ToWireStatus(MedicalRecordStatus status) => status switch
    {
        MedicalRecordStatus.Draft => "draft",
        MedicalRecordStatus.Signed => "signed",
        MedicalRecordStatus.Amended => "amended",
        _ => status.ToString().ToLowerInvariant(),
    };

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();
}

/// <summary>
/// One correction, with its field-level diff — the shape
/// <c>GET /staff/medical-records/{id}/audit</c> renders as "ใครแก้อะไรเมื่อไหร่ · diff รายฟิลด์".
/// </summary>
public sealed record AmendmentDto(
    string Id,
    DateTimeOffset CreatedAt,
    string ByStaffId,
    string Reason,
    JsonElement Changes)
{
    public static AmendmentDto From(MedicalRecordAmendment amendment) =>
        new(
            amendment.Id,
            amendment.CreatedAt,
            amendment.ByStaffId,
            amendment.Reason,
            JsonDocument.Parse(amendment.ChangesJson).RootElement.Clone());
}
