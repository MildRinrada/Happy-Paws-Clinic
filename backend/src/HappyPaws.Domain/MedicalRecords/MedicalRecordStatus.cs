namespace HappyPaws.Domain.MedicalRecords;

/// <summary>
/// Medical record state machine from the contract: <c>draft → signed → amended</c>.
/// <para>
/// Records are append-only. A signed record can never be deleted or edited in place —
/// mutating attempts answer <c>423 record_locked</c> and the caller must post an amendment
/// through <c>POST /staff/medical-records/{id}/amendments</c>, which preserves the prior
/// version together with its signer.
/// </para>
/// </summary>
public enum MedicalRecordStatus
{
    Draft,
    Signed,
    Amended,
}
