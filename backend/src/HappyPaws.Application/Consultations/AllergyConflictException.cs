using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Consultations;

/// <summary>
/// A prescription names a drug the patient has a recorded allergy to, and the prescriber has not
/// acknowledged it.
/// <para>
/// <b>409, not 422, and refusable rather than fatal.</b> The contract requires the prescription
/// endpoint to "ตรวจแพ้ยา" but does not say what checking should do, and both extreme answers are
/// wrong. Blocking outright would be a system overruling a vet who has decided the drug is
/// necessary anyway — which happens, and which a paper prescription pad would allow. Warning
/// silently would make the check decorative.
/// </para>
/// <para>
/// So the first attempt is refused, the allergy is named in <c>details</c>, and the caller may
/// repeat the request with an explicit acknowledgement, which is stored on the prescription and
/// written to the audit log. The decision stays with the vet; the record shows it was made
/// knowingly. Severe allergies are not overridable at all.
/// </para>
/// <para>
/// The error code is an extension — see <c>docs/assumptions.md § 14</c>.
/// </para>
/// </summary>
public sealed class AllergyConflictException : AppException
{
    public AllergyConflictException(string drugName, IReadOnlyList<string> substances, bool overridable)
        : base("allergy_conflict", $"คนไข้มีประวัติแพ้ {string.Join(", ", substances)}", HttpStatus.Conflict)
    {
        MessageEn = overridable
            ? "The patient has a recorded allergy to this drug. Resend with acknowledge_allergy to proceed."
            : "The patient has a recorded severe allergy to this drug. It cannot be overridden here.";
        Field = "drug_name";
        Details = new Dictionary<string, object>
        {
            ["drug_name"] = drugName,
            ["allergies"] = substances,
            ["overridable"] = overridable,
        };
    }
}
