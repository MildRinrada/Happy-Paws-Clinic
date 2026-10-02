namespace HappyPaws.Domain.Common;

/// <summary>
/// An attempt to modify a medical record that has already been signed.
/// <para>
/// Surfaces as <c>423 record_locked</c>. Signed records are never edited in place and never
/// deleted; the caller must post an amendment through
/// <c>POST /staff/medical-records/{id}/amendments</c>, which keeps the original and its signer.
/// </para>
/// </summary>
public sealed class RecordLockedException(string recordId)
    : DomainException("record_locked", "เวชระเบียนถูกลงนามแล้ว ต้องแก้ไขผ่าน amendment")
{
    public string RecordId { get; } = recordId;
}
