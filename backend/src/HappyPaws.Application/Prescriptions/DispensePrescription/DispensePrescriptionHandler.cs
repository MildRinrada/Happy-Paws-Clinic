using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Prescriptions.Dtos;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Prescriptions;

namespace HappyPaws.Application.Prescriptions.DispensePrescription;

/// <summary><c>POST /staff/prescriptions/{id}/dispense</c> — "จ่ายยาจริง · ตัดสต๊อกและพิมพ์ฉลาก".</summary>
public sealed record DispensePrescriptionCommand(string PrescriptionId);

/// <summary>
/// Hands a written prescription over to the owner.
/// <para>
/// <b>Two of the three things in the contract's line happen.</b> The prescription is marked as
/// dispensed, by whom and when, and the answer carries the label. The third, "ตัดสต๊อก", needs the
/// Inventory module, which does not exist yet: there is no stock to cut from and no item a drug name
/// maps to. The answer says <c>stock_deducted: false</c> rather than pretending, and the drug is not
/// billed either, for the same reason — its price lives in the inventory that is not there.
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
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<DispenseResultDto> HandleAsync(
        DispensePrescriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        var staffId = currentUser.RequireStaffId();
        PrescriptionInContext? dispensed = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var found = await prescriptions.FindTrackedAsync(command.PrescriptionId, token);

            if (found is null || !branchGuard.CanAccess(found.BranchId))
            {
                throw new NotFoundException("prescription", command.PrescriptionId);
            }

            found.Prescription.Dispense(staffId, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.PrescriptionDispensed,
                AuditResources.Prescription,
                found.Prescription.Id,
                found.Prescription.PetId,
                found.BranchId,
                new Dictionary<string, object?>
                {
                    ["drug_name"] = found.Prescription.DrugName,
                    ["quantity"] = found.Prescription.Quantity,
                }));

            dispensed = found;
        }, cancellationToken);

        var label = await LabelAsync(dispensed!, cancellationToken);

        return new DispenseResultDto(
            PrescriptionItemDto.From(dispensed!.Prescription, clock.UtcNow),
            label,
            StockDeducted: false);
    }

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
