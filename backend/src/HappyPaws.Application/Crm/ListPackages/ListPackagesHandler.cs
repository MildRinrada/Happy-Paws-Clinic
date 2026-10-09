using FluentValidation;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Crm.Dtos;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Scheduling;

namespace HappyPaws.Application.Crm.ListPackages;

/// <summary>
/// <c>GET /staff/packages?customer_id=&amp;status=&amp;branch=</c>. Not in the API reference: see
/// <c>docs/contract-extensions.md § 25</c>.
/// </summary>
public sealed record ListPackagesQuery(string? CustomerId = null, string? Status = null, string? BranchId = null);

public sealed class ListPackagesValidator : AbstractValidator<ListPackagesQuery>
{
    public ListPackagesValidator()
    {
        RuleFor(query => query.CustomerId).NotEmpty().WithMessage("ต้องระบุ customer_id").MaximumLength(40);

        RuleFor(query => query.Status)
            .Must(status => CrmWire.ParsePackageStatus(status) is not null)
            .WithMessage("status ต้องเป็น pending_payment, active, used_up, expired หรือ cancelled")
            .When(query => !string.IsNullOrEmpty(query.Status));

        RuleFor(query => query.BranchId).MaximumLength(40);
    }
}

/// <summary>
/// One customer's packages: usable first, then waiting for payment, then the rest, soonest to expire
/// first within each, the order of the loyalty page.
/// <para>
/// <b>Clinic-wide, like the loyalty account.</b> A customer belongs to the clinic and a package sold
/// at one branch is spent at any, so every branch's staff see every package of the customer.
/// <c>branch</c> narrows to the packages sold at one branch; it is a filter, not a permission.
/// A customer has a handful of packages, so the answer is one list, not pages.
/// </para>
/// </summary>
public sealed class ListPackagesHandler(
    ICustomerRepository customers,
    ICustomerPackageRepository packages,
    RequestValidator validator,
    ClinicDays clinicDays)
{
    public async Task<IReadOnlyList<PackageDto>> HandleAsync(ListPackagesQuery query, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(query, cancellationToken);

        var customer = await customers.FindByIdAsync(query.CustomerId!.Trim(), cancellationToken)
            ?? throw new NotFoundException("customer", query.CustomerId);

        var owned = await packages.ForCustomerAsync(customer.Id, cancellationToken);

        if (query.BranchId is { Length: > 0 } branchId)
        {
            owned = [.. owned.Where(package => package.BranchId == branchId)];
        }

        var listed = PackageDto.Ordered(owned, clinicDays.Today);

        return CrmWire.ParsePackageStatus(query.Status) is { } status
            ? [.. listed.Where(package => package.Status == CrmWire.ToWire(status))]
            : listed;
    }
}
