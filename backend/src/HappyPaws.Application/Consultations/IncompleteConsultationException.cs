using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Consultations;

/// <summary>
/// <c>409 incomplete_consultation</c> — the case lacks something this clinic requires before a
/// record may be signed.
/// <para>
/// The same code the case itself raises when it has no diagnosis, because to the vet they are the
/// same event: "you cannot close this yet". What this adds is <c>details.missing_fields</c>, named
/// as the request bodies name them, so the screen can take the vet to the empty section instead of
/// reporting that something, somewhere, is missing.
/// </para>
/// <para>
/// A 409 rather than a 422: the close request itself is well-formed. It is the case that is not
/// ready — see <c>docs/assumptions.md § 24</c>.
/// </para>
/// </summary>
public sealed class IncompleteConsultationException : AppException
{
    public IncompleteConsultationException(IReadOnlyList<string> missingFields)
        : base("incomplete_consultation", "ยังกรอกข้อมูลที่คลินิกกำหนดให้บังคับไม่ครบ", HttpStatus.Conflict)
    {
        MessageEn = "The consultation is missing fields this clinic requires before signing.";
        Field = missingFields[0];
        Details = new Dictionary<string, object> { ["missing_fields"] = missingFields };
    }
}
