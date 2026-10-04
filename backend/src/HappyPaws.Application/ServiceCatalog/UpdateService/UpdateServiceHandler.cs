using FluentValidation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.ServiceCatalog.Dtos;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.ServiceCatalog.UpdateService;

/// <summary>
/// <c>PATCH /staff/services/{id}</c>. Not in the API reference; see
/// <c>docs/contract-extensions.md § 17</c>. An absent field is left alone.
/// </summary>
/// <param name="ServiceId">The service.</param>
/// <param name="Name">The name on the menu and on a bill.</param>
/// <param name="CategoryCode">One of the six from <c>GET /services/categories</c>.</param>
/// <param name="DurationMinutes">How long one visit takes, for bookings made from now on.</param>
/// <param name="EligibleStaffIds">Who may perform it. Empty for any vet on shift.</param>
/// <param name="IsActive">False takes it off every booking path, counter included.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record UpdateServiceCommand(
    string ServiceId,
    string? Name = null,
    string? CategoryCode = null,
    int? DurationMinutes = null,
    IReadOnlyList<string>? EligibleStaffIds = null,
    bool? IsActive = null,
    uint? IfMatchVersion = null);

public sealed class UpdateServiceValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("ต้องระบุชื่อบริการ")
            .MaximumLength(200).WithMessage("ชื่อบริการยาวได้ไม่เกิน 200 ตัวอักษร")
            .When(command => command.Name is not null);

        RuleFor(command => command.CategoryCode)
            .Must(code => ServiceCategories.IsKnown(code!))
            .WithMessage($"หมวดต้องเป็นหนึ่งใน: {string.Join(", ", ServiceCategories.All.Select(c => c.Code))}")
            .When(command => command.CategoryCode is not null);

        RuleFor(command => command.DurationMinutes)
            .InclusiveBetween(5, 480).WithMessage("ระยะเวลาบริการต้องอยู่ระหว่าง 5–480 นาที")
            .When(command => command.DurationMinutes is not null);

        RuleFor(command => command.EligibleStaffIds)
            .Must(ids => ids is null || ids.Count <= 100)
            .WithMessage("ระบุผู้ให้บริการได้ไม่เกิน 100 คน");
    }
}

/// <summary>
/// Changes what a service is: its name, heading, length, who performs it, and whether it is on
/// sale at all. Price and online availability keep the contract's own endpoints.
/// <para>
/// Existing appointments are untouched. A shorter duration does not shrink a visit already booked,
/// and taking a service off sale cancels nothing; it stops new bookings at the counter and online.
/// </para>
/// </summary>
public sealed class UpdateServiceHandler(
    IServiceRepository services,
    IStaffRepository staff,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<ServiceDto> HandleAsync(
        UpdateServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        ClinicService? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var service = await services.FindTrackedByIdAsync(command.ServiceId, token)
                ?? throw new NotFoundException("service", command.ServiceId);

            ConcurrencyGuard.Ensure(service, command.IfMatchVersion);

            if (command.EligibleStaffIds is { } staffIds)
            {
                var ids = staffIds.Distinct(StringComparer.Ordinal).ToArray();

                if (ids.Length > 0 && (await staff.MissingAsync(ids, token)) is { Count: > 0 } missing)
                {
                    throw new ValidationFailedException(new Dictionary<string, string[]>
                    {
                        ["eligible_staff_ids"] = [$"ไม่พบพนักงานที่ใช้งานอยู่: {string.Join(", ", missing)}"],
                    });
                }

                service.SetEligibleStaff(ids);
            }

            service.Revise(command.Name, command.CategoryCode, command.DurationMinutes);

            if (command.IsActive is { } active)
            {
                service.SetActive(active);
            }

            updated = service;
        }, cancellationToken);

        return ServiceDto.From(updated!, DateOnly.FromDateTime(clock.NowInClinicTime().DateTime));
    }
}
