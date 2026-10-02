namespace HappyPaws.Api.Contracts.MedicalRecords;

/// <summary>
/// <c>POST /staff/medical-records/{id}/amendments</c> — "แก้ไขแบบมีร่องรอย".
/// <para>
/// <c>changes</c> maps field name to new value, over the narrative fields only
/// (<c>chief_complaint</c>, <c>subjective</c>, <c>objective</c>, <c>assessment</c>, <c>plan</c>).
/// A field outside that set is refused: treatments have been billed and prescriptions may have been
/// dispensed, so those are corrected through billing or a new consultation rather than by editing
/// what the record says happened.
/// </para>
/// <para>
/// <c>reason</c> is required. An amendment with no stated reason is indistinguishable from
/// tampering when it is read back years later, which is the only time anyone reads it.
/// </para>
/// </summary>
public sealed record AmendRecordRequest(string Reason, IReadOnlyDictionary<string, string?> Changes);
