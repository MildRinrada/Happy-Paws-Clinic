using HappyPaws.Application.Common;
using HappyPaws.Domain.Laboratory;

namespace HappyPaws.Application.Laboratory.Dtos;

/// <summary>A lab request and every result it has so far.</summary>
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
    int AbnormalCount,
    IReadOnlyList<LabResultDto> Results)
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
            request.AbnormalCount,
            [.. request.Results.OrderBy(result => result.RecordedAt).Select(LabResultDto.From)]);
}

public sealed record LabTestDto(string Name, string? Code);

/// <summary>One measured value with its range and flag.</summary>
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
    string RecordedByStaffId)
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
            result.RecordedByStaffId);
}
