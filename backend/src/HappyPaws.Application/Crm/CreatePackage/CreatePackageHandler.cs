using FluentValidation;
using HappyPaws.Application.Appointments;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Billing;
using HappyPaws.Application.Billing.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Patients;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Crm;

namespace HappyPaws.Application.Crm.CreatePackage;

/// <summary><c>POST /staff/packages</c> — "แพ็กเกจตรวจ 4 ครั้ง · ตัดสิทธิ์อัตโนมัติเมื่อใช้".</summary>
public sealed record CreatePackageCommand(
    string CustomerId,
    string Name,
    IReadOnlyList<string> ServiceIds,
    int TotalUses,
    long PriceSatang,
    DateOnly? ExpiresOn = null,
    string? PetId = null,
    string? BranchId = null,
    string? Note = null);

public sealed class CreatePackageValidator : AbstractValidator<CreatePackageCommand>
{
    public const int MaxServices = 20;

    public CreatePackageValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty().MaximumLength(40);
        RuleFor(command => command.Name).NotEmpty().WithMessage("ต้องระบุชื่อแพ็กเกจ").MaximumLength(200);

        RuleFor(command => command.ServiceIds)
            .NotEmpty().WithMessage("ต้องเลือกบริการที่ใช้สิทธิ์ได้อย่างน้อยหนึ่งบริการ")
            .Must(ids => ids is null || ids.Count <= MaxServices)
            .WithMessage($"เลือกบริการได้ไม่เกิน {MaxServices} บริการ");
        RuleForEach(command => command.ServiceIds).NotEmpty().MaximumLength(40);

        RuleFor(command => command.TotalUses)
            .InclusiveBetween(1, CustomerPackage.MaxUses)
            .WithMessage("จำนวนครั้งต้องอยู่ระหว่าง 1 ถึง 1,000");

        RuleFor(command => command.PriceSatang).GreaterThanOrEqualTo(0).WithMessage("ต้องระบุราคา และราคาติดลบไม่ได้");
        RuleFor(command => command.PetId).MaximumLength(40);
        RuleFor(command => command.Note).MaximumLength(1_000);
    }
}

/// <summary>
/// Sells a package: the package and the bill for it, in one transaction.
/// <para>
/// The package is <c>pending_payment</c> until the bill reaches <c>paid</c>, which the cash desk
/// does through the ordinary payment endpoint; a free package is paid the moment it is raised.
/// Requires <c>Idempotency-Key</c> — "ทุก POST ที่สร้างเงิน" — because a retried sale would charge the
/// customer twice.
/// </para>
/// </summary>
public sealed class CreatePackageHandler(
    ICustomerPackageRepository packages,
    ICustomerRepository customers,
    IPetRepository pets,
    IBookingCatalog catalog,
    SaleBilling saleBilling,
    BranchGuard branchGuard,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    ClinicDays clinicDays,
    IClock clock)
{
    public async Task<PackageSaleDto> HandleAsync(
        CreatePackageCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var now = clock.UtcNow;
        var today = clinicDays.DateOf(now);

        if (command.ExpiresOn is { } expires && expires < today)
        {
            throw Invalid("expires_on", "วันหมดอายุต้องไม่ก่อนวันนี้");
        }

        var branchId = await CrmBranches.ResolveAsync(catalog, command.BranchId, cancellationToken);
        branchGuard.EnsureCanAccess(branchId);

        var customer = await customers.FindByIdAsync(command.CustomerId, cancellationToken)
            ?? throw new NotFoundException("customer", command.CustomerId);

        await EnsurePetBelongsAsync(command.PetId, customer.Id, cancellationToken);

        var serviceIds = command.ServiceIds.Select(id => id.Trim()).Distinct(StringComparer.Ordinal).ToList();
        await CrmServices.EnsureExistAsync(catalog, serviceIds, "service_ids", cancellationToken);

        CustomerPackage? package = null;
        Invoice? invoice = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            package = CustomerPackage.Sell(
                customer.Id,
                command.Name,
                serviceIds,
                command.TotalUses,
                command.PriceSatang,
                branchId,
                staffId,
                now,
                command.ExpiresOn,
                command.PetId);

            invoice = await saleBilling.RaiseAsync(
                new SaleBillRequest(
                    branchId,
                    customer.Id,
                    package.PetId,
                    staffId,
                    $"แพ็กเกจ {package.Name} ({package.TotalUses:N0} ครั้ง)",
                    package.PriceSatang,
                    package.Id,
                    "package_sale",
                    command.Note),
                now,
                token);

            package.AttachInvoice(invoice.Id);

            // A free package is a bill of zero, which is paid the moment it exists.
            if (invoice.Status == InvoiceStatus.Paid)
            {
                package.Activate(now);
            }

            packages.Add(package);

            audit.Record(new AuditEvent(
                AuditActions.PackageSold,
                AuditResources.Package,
                package.Id,
                package.PetId,
                branchId,
                new Dictionary<string, object?>
                {
                    ["customer_id"] = customer.Id,
                    ["invoice_id"] = invoice.Id,
                    ["name"] = package.Name,
                    ["total_uses"] = package.TotalUses,
                    ["price_satang"] = package.PriceSatang,
                    ["service_ids"] = package.ServiceIds,
                    ["expires_on"] = package.ExpiresOn?.ToString("yyyy-MM-dd"),
                }));
        }, cancellationToken);

        return new PackageSaleDto(PackageDto.From(package!, today), InvoiceDto.From(invoice!));
    }

    private async Task EnsurePetBelongsAsync(string? petId, string customerId, CancellationToken cancellationToken)
    {
        if (petId is not { Length: > 0 })
        {
            return;
        }

        var owned = await pets.ForCustomerAsync(customerId, cancellationToken);

        if (owned.All(pet => pet.Id != petId))
        {
            throw Invalid("pet_id", "สัตว์ตัวนี้ไม่ได้อยู่ในความดูแลของลูกค้าคนนี้");
        }
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
