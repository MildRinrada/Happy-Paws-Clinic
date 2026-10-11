using HappyPaws.Application.Common;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.Dtos;

/// <summary>
/// A lab request and every result it has so far. <c>ReviewNote</c> (§ 53) and <c>Attachments</c>
/// (§ 54) are contract extensions; see <c>docs/contract-extensions.md</c>.
/// </summary>
public sealed record LabRequestDto(
    string Id,
    uint Version,
    string PetId,
    string BranchId,
    string? ConsultationId,
    string Destination,
    string? ExternalLabName,
    string Priority,
    IReadOnlyList<LabTestDto> Tests,
    string? ClinicalNotes,
    string RequestedByStaffId,
    DateTimeOffset RequestedAt,
    string Status,
    DateTimeOffset? ResultedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewedByStaffId,
    string? ReviewNote,
    int AbnormalCount,
    IReadOnlyList<LabResultDto> Results,
    IReadOnlyList<LabAttachmentDto> Attachments)
{
    public static LabRequestDto From(LabRequest request) =>
        new(
            request.Id,
            request.Version,
            request.PetId,
            request.BranchId,
            request.ConsultationId,
            WireEnum.Of(request.Destination),
            request.ExternalLabName,
            WireEnum.Of(request.Priority),
            [.. request.Tests.Select(test => new LabTestDto(test.Name, test.Code))],
            request.ClinicalNotes,
            request.RequestedByStaffId,
            request.RequestedAt,
            WireEnum.Of(request.Status),
            request.ResultedAt,
            request.ReviewedAt,
            request.ReviewedByStaffId,
            request.ReviewNote,
            request.AbnormalCount,
            [.. request.Results.OrderBy(result => result.RecordedAt).Select(LabResultDto.From)],
            [.. request.Attachments.OrderBy(attachment => attachment.AttachedAt).Select(LabAttachmentDto.From)]);
}

public sealed record LabTestDto(string Name, string? Code);

/// <summary>
/// One measured value with its range and flag. A corrected value says so (<c>Amended</c>) and
/// carries every value it replaced, oldest first; a voided one is still listed, with
/// <c>Voided</c> set (§ 52).
/// </summary>
public sealed record LabResultDto(
    string Id,
    string Analyte,
    decimal? ValueNumeric,
    string? ValueText,
    string? Unit,
    decimal? ReferenceLow,
    decimal? ReferenceHigh,
    string? ReferenceText,
    string? Flag,
    DateTimeOffset RecordedAt,
    string RecordedByStaffId,
    bool Amended,
    DateTimeOffset? AmendedAt,
    string? AmendedByStaffId,
    string? AmendReason,
    IReadOnlyList<LabResultRevisionDto> PreviousValues,
    bool Voided,
    DateTimeOffset? VoidedAt,
    string? VoidedByStaffId,
    string? VoidReason)
{
    public static LabResultDto From(LabResult result) =>
        new(
            result.Id,
            result.Analyte,
            result.ValueNumeric,
            result.ValueText,
            result.Unit,
            result.ReferenceLow,
            result.ReferenceHigh,
            result.ReferenceText,
            result.Flag is { } flag ? WireEnum.Of(flag) : null,
            result.RecordedAt,
            result.RecordedByStaffId,
            result.IsAmended,
            result.AmendedAt,
            result.AmendedByStaffId,
            result.AmendReason,
            [.. result.PreviousValues.Select(LabResultRevisionDto.From)],
            result.IsVoided,
            result.VoidedAt,
            result.VoidedByStaffId,
            result.VoidReason);
}

/// <summary>A value a correction replaced, with who replaced it, when and why.</summary>
public sealed record LabResultRevisionDto(
    string Analyte,
    decimal? ValueNumeric,
    string? ValueText,
    string? Unit,
    decimal? ReferenceLow,
    decimal? ReferenceHigh,
    string? ReferenceText,
    string? Flag,
    DateTimeOffset ReplacedAt,
    string ReplacedByStaffId,
    string Reason)
{
    public static LabResultRevisionDto From(LabResultRevision revision) =>
        new(
            revision.Analyte,
            revision.ValueNumeric,
            revision.ValueText,
            revision.Unit,
            revision.ReferenceLow,
            revision.ReferenceHigh,
            revision.ReferenceText,
            revision.Flag,
            revision.ReplacedAt,
            revision.ReplacedByStaffId,
            revision.Reason);
}

/// <summary>
/// A file on the request, without a link: opening one asks for a short-lived link of its own
/// (<c>GET /staff/lab-requests/{id}/attachments/{attachmentId}/link</c>), so a list of requests
/// never hands out working file addresses.
/// </summary>
public sealed record LabAttachmentDto(
    string Id,
    string FileName,
    string ContentType,
    long? SizeBytes,
    DateTimeOffset AttachedAt,
    string AttachedByStaffId)
{
    public static LabAttachmentDto From(LabAttachment attachment) =>
        new(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.AttachedAt,
            attachment.AttachedByStaffId);
}
