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
/// <para>
/// Once dispensed, <c>inventory_item_id</c>, <c>stock_deducted</c> and
/// <c>charged_unit_price_satang</c> say what the dispense did: which item the drug matched, whether
/// stock was taken, and the price charged.
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
    IReadOnlyList<string> DuplicateOf,
    string? InventoryItemId = null,
    bool? StockDeducted = null,
    long? ChargedUnitPriceSatang = null)
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
            duplicateOf ?? [],
            prescription.InventoryItemId,
            prescription.IsDispensed ? prescription.StockDeducted : null,
            prescription.ChargedUnitPriceSatang);
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
/// <c>POST /staff/prescriptions/{id}/dispense</c> — the prescription now dispensed, its label, and
/// what the dispense did to stock and to the bill.
/// <para>
/// <c>stock_deducted</c> is true only when the quantity came off the branch's stock. When it is
/// false, <c>stock.status</c> says why: <c>not_matched</c> (no inventory item has this drug's name;
/// nothing charged either) or <c>insufficient</c> (the item matched but the books show fewer usable
/// units; still charged, because the drug was handed over). Saying so is better than a dispense that
/// silently did half of what it was asked.
/// </para>
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>charge</c>: What was charged and on which bill. Absent when nothing was charged.</item>
/// </list>
/// </remarks>
public sealed record DispenseResultDto(
    PrescriptionItemDto Prescription,
    DispenseLabelDto Label,
    bool StockDeducted,
    DispenseStockDto Stock,
    DispenseChargeDto? Charge = null);

/// <summary>What the dispense did to stock.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>status</c>: <c>deducted</c>, <c>not_matched</c> or <c>insufficient</c>.</item>
/// <item><c>available</c>: Usable units at the branch before the dispense, when an item matched.</item>
/// <item><c>lots</c>: The lots taken from, first expiry first. Empty unless deducted.</item>
/// </list>
/// </remarks>
public sealed record DispenseStockDto(
    string Status,
    string? ItemId = null,
    string? ItemName = null,
    string? Unit = null,
    int? Available = null,
    IReadOnlyList<DispensedLotDto>? Lots = null);

public sealed record DispensedLotDto(string LotId, string? LotNumber, DateOnly? ExpiresOn, int Quantity);

/// <summary>The medicine charge.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>placement</c>: <c>invoiced</c> (on the case's bill), <c>on_close</c> (the case is open; it lands on the bill its close raises) or <c>separate_invoice</c> (the case's bill had already taken money, so a new bill).</item>
/// </list>
/// </remarks>
public sealed record DispenseChargeDto(
    string Placement,
    long UnitPriceSatang,
    long AmountSatang,
    string? InvoiceId = null,
    string? InvoiceNumber = null);
