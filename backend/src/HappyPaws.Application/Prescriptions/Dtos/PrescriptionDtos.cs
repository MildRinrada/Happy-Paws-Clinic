using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.Application.Prescriptions.Dtos;

/// <summary>
/// One prescription in a patient's medication history — <c>GET /staff/prescriptions?pet_id=</c>,
/// "ยาที่เคยจ่ายทั้งหมด · ตรวจซ้ำซ้อน".
/// <para>
/// <c>active</c> is true while the course is still running. <c>duplicate_of</c> is "ตรวจซ้ำซ้อน":
/// the other prescriptions of the same drug whose courses overlap this one, empty when there are
/// none. Matched on the drug's first word, so two brand names for one molecule are missed.
/// </para>
/// </summary>
public sealed record PrescriptionItemDto(
    string Id,
    uint Version,
    string PetId,
    string ConsultationId,
    string DrugName,
    string Dose,
    string Frequency,
    int DurationDays,
    int Quantity,
    string? Instructions,
    string? AllergyAcknowledgement,
    DateTimeOffset PrescribedAt,
    string PrescribedByStaffId,
    DateTimeOffset CourseEndsAt,
    bool Active,
    DateTimeOffset? DispensedAt,
    string? DispensedByStaffId,
    IReadOnlyList<string> DuplicateOf)
{
    public static PrescriptionItemDto From(
        Prescription prescription,
        DateTimeOffset now,
        IReadOnlyList<string>? duplicateOf = null) =>
        new(
            prescription.Id,
            prescription.Version,
            prescription.PetId,
            prescription.ConsultationId,
            prescription.DrugName,
            prescription.Dose,
            prescription.Frequency,
            prescription.DurationDays,
            prescription.Quantity,
            prescription.Instructions,
            prescription.AllergyAcknowledgement,
            prescription.PrescribedAt,
            prescription.PrescribedByStaffId,
            prescription.CourseEndsAt,
            prescription.IsActiveAt(now),
            prescription.DispensedAt,
            prescription.DispensedByStaffId,
            duplicateOf ?? []);
}

/// <summary>
/// "พิมพ์ฉลาก" — what goes on the label stuck to the bag, already in the order a label prints it.
/// <para>
/// Data, not a picture: the counter's label printer, a PDF and a screen all lay out the same lines
/// differently, and the API should not pick a printer.
/// </para>
/// </summary>
public sealed record DispenseLabelDto(
    string ClinicName,
    string? ClinicLicenseNo,
    string BranchName,
    string PetName,
    string HospitalNumber,
    string OwnerName,
    string DrugName,
    string Dose,
    string Frequency,
    int DurationDays,
    int Quantity,
    string? Instructions,
    string PrescribedBy,
    string? PrescriberLicenseNo,
    DateOnly DispensedOn,
    string PrescriptionId);

/// <summary>
/// <c>POST /staff/prescriptions/{id}/dispense</c> — the prescription now dispensed, and its label.
/// <para>
/// <c>stock_deducted</c> is false until the Inventory module exists. "ตัดสต๊อก" needs a stock to
/// cut from; saying so in the answer is better than a dispense that silently did half of what it
/// was asked.
/// </para>
/// </summary>
public sealed record DispenseResultDto(PrescriptionItemDto Prescription, DispenseLabelDto Label, bool StockDeducted);
