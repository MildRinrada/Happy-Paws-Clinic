using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Application.Inventory;
using HappyPaws.Application.Inventory.Dispensing;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Prescriptions.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Integrations;
using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.Application.Prescriptions.DispensePrescription;

/// <summary><c>POST /staff/prescriptions/{id}/dispense</c> — "จ่ายยาจริง · ตัดสต๊อกและพิมพ์ฉลาก".</summary>
public sealed record DispensePrescriptionCommand(string PrescriptionId);

/// <summary>
/// Hands a written prescription over to the owner.
/// <para>
/// <b>All three things in the contract's line, in one transaction.</b> The prescription is marked
/// as dispensed, by whom and when; "ตัดสต๊อก" takes the quantity off the dispensing branch's stock
/// (<see cref="StockDispenser"/>); and the answer carries the label to print. The medicine is charged
/// at the matched item's price, on the case's bill (<see cref="MedicineBilling"/>). Either all of it
/// commits or none of it does.
/// </para>
/// <para>
/// <b>Stock problems never block the hand-over.</b> A drug that matches no inventory item is
/// dispensed with <c>stock_deducted: false</c>, <c>stock.status: not_matched</c> and no charge; one
/// whose branch shows too few usable units is dispensed and charged with <c>stock.status:
/// insufficient</c> and stock untouched. The contract does not say a dispense may be refused for
/// stock, and the drug is already in the owner's hand. See <c>docs/assumptions.md § 42</c>.
/// </para>
/// <para>
/// A prescription is dispensed once. A second press answers <c>409 state_conflict</c>, which is
/// what stops one bag of tablets being handed out twice from two counters.
/// </para>
/// </summary>
public sealed class DispensePrescriptionHandler(
    IPrescriptionRepository prescriptions,
    IPetRepository pets,
    ICustomerRepository customers,
    IStaffRepository staff,
    IBranchRepository branches,
    IClinicSettingsStore settings,
    BranchGuard branchGuard,
    ClinicDays clinicDays,
    StockDispenser dispenser,
    MedicineBilling billing,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IWebhookOutbox webhooks,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<DispenseResultDto> HandleAsync(
        DispensePrescriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        PrescriptionInContext? dispensed = null;
        DispenseStockResult? stock = null;
        MedicineCharge? charge = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;
            var found = await prescriptions.FindTrackedAsync(command.PrescriptionId, token);

            if (found is null || !branchGuard.CanAccess(found.BranchId))
            {
                throw new NotFoundException("prescription", command.PrescriptionId);
            }

            var prescription = found.Prescription;
            prescription.Dispense(staffId, now);

            stock = await dispenser.TakeAsync(prescription, found.BranchId, staffId, now, token);
            prescription.RecordDispenseOutcome(stock.Item?.Id, stock.Deducted, stock.Item?.UnitPriceSatang);
            charge = await billing.ChargeAsync(found, staffId, now, token);

            webhooks.Enqueue(WebhookEventTypes.PrescriptionDispensed, WebhookPayloads.Dispensed(prescription, found.BranchId));

            audit.Record(new AuditEvent(
                AuditActions.PrescriptionDispensed,
                AuditResources.Prescription,
                prescription.Id,
                prescription.PetId,
                found.BranchId,
                new Dictionary<string, object?>
                {
                    ["drug_name"] = prescription.DrugName,
                    ["quantity"] = prescription.Quantity,
                    ["inventory_item_id"] = stock.Item?.Id,
                    ["stock"] = WireEnum.Of(stock.Status),
                    ["charged_satang"] = charge?.AmountSatang,
                    ["invoice_id"] = charge?.Invoice?.Id,
                }));

            dispensed = found;
        }, cancellationToken);

        var label = await LabelAsync(dispensed!, cancellationToken);

        return new DispenseResultDto(
            PrescriptionItemDto.From(dispensed!.Prescription, clock.UtcNow),
            label,
            stock!.Deducted,
            StockOf(stock),
            charge is null
                ? null
                : new DispenseChargeDto(
                    WireEnum.Of(charge.Placement),
                    charge.UnitPriceSatang,
                    charge.AmountSatang,
                    charge.Invoice?.Id,
                    charge.Invoice?.Number));
    }

    private static DispenseStockDto StockOf(DispenseStockResult stock) =>
        stock.Item is not { } item
            ? new DispenseStockDto(WireEnum.Of(stock.Status))
            : new DispenseStockDto(
                WireEnum.Of(stock.Status),
                item.Id,
                item.Name,
                item.Unit,
                stock.Available,
                [.. stock.Taken.Select(row => new DispensedLotDto(row.Lot.Id, row.Lot.LotNumber, row.Lot.ExpiresOn, -row.Movement.Quantity))]);

    private async Task<DispenseLabelDto> LabelAsync(PrescriptionInContext found, CancellationToken cancellationToken)
    {
        var prescription = found.Prescription;

        var pet = await pets.FindByIdAsync(prescription.PetId, cancellationToken);
        var owner = await customers.FindByIdAsync(found.CustomerId, cancellationToken);
        var prescriber = await staff.FindRowAsync(prescription.PrescribedByStaffId, cancellationToken);
        var branch = (await branches.ListActiveAsync(cancellationToken)).FirstOrDefault(item => item.Id == found.BranchId);
        var clinic = await settings.ProfileAsync(cancellationToken);

        return new DispenseLabelDto(
            clinic.Name,
            clinic.LicenseNo,
            branch?.Name ?? found.BranchId,
            pet?.Name ?? prescription.PetId,
            pet?.HospitalNumber ?? string.Empty,
            owner?.DisplayName ?? found.CustomerId,
            prescription.DrugName,
            prescription.Dose,
            prescription.Frequency,
            prescription.DurationDays,
            prescription.Quantity,
            prescription.Instructions,
            prescriber?.Account.DisplayName ?? prescription.PrescribedByStaffId,
            prescriber?.Membership.LicenseNo,
            clinicDays.DateOf(prescription.DispensedAt!.Value),
            prescription.Id);
    }
}
