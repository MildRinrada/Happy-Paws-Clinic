namespace HappyPaws.Api.Contracts.Laboratory;

/// <summary>
/// <c>POST /staff/lab-requests</c> — "ส่งแล็บในคลินิกหรือแล็บภายนอก".
/// <para>
/// <c>destination</c> is <c>in_house</c> or <c>external</c>; an external request names the lab.
/// The branch comes from <c>consultation_id</c> when given, otherwise from <c>branch_id</c>,
/// otherwise from the caller's own branch.
/// </para>
/// </summary>
public sealed record CreateLabRequestRequest(
    string PetId,
    string Destination,
    IReadOnlyList<LabTestRequest> Tests,
    string? ConsultationId = null,
    string? BranchId = null,
    string? ExternalLabName = null,
    string? Priority = null,
    string? ClinicalNotes = null);

/// <summary>One test asked for — a name, and the lab's code when it has one.</summary>
public sealed record LabTestRequest(string Name, string? Code = null);

/// <summary>
/// <c>POST /staff/lab-requests/{id}/results</c> — "ค่าที่วัดได้ + ช่วงอ้างอิง · ธงค่าผิดปกติ".
/// </summary>
public sealed record RecordLabResultsRequest(IReadOnlyList<LabResultRequest> Results);

/// <summary>
/// One value. A number goes in <c>value</c>, anything else in <c>value_text</c>. The flag is worked
/// out from <c>value</c> and the reference range; <c>abnormal</c> is only read when it cannot be.
/// </summary>
public sealed record LabResultRequest(
    string Analyte,
    decimal? Value = null,
    string? ValueText = null,
    string? Unit = null,
    decimal? ReferenceLow = null,
    decimal? ReferenceHigh = null,
    string? ReferenceText = null,
    bool? Abnormal = null);
