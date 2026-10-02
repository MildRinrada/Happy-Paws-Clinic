using FluentValidation;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.ServiceCatalog.Dtos;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.ServiceCatalog.CreateService;

/// <summary>
/// <c>POST /staff/services</c> — "ชื่อ ราคา ระยะเวลา ผู้ให้บริการที่ทำได้".
/// </summary>
/// <param name="Name">The service as it appears on the menu and on a bill.</param>
/// <param name="CategoryCode">One of the six from <c>GET /services/categories</c>.</param>
/// <param name="DurationMinutes">How long one visit takes. Decides where the next slot can start.</param>
/// <param name="PriceSatang">Base price.</param>
/// <param name="Code">Optional readable code — <c>checkup</c>. Becomes the id, <c>svc_checkup</c>.</param>
/// <param name="EligibleStaffIds">Who may perform it; empty for any vet on shift.</param>
/// <param name="OnlineBookingEnabled">Whether it goes on the public menu now.</param>
public sealed record CreateServiceCommand(
    string Name,
    string CategoryCode,
    int DurationMinutes,
    long PriceSatang,
    string? Code = null,
    IReadOnlyList<string>? EligibleStaffIds = null,
    bool OnlineBookingEnabled = false);

public sealed class CreateServiceValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);

        RuleFor(command => command.CategoryCode)
            .Must(ServiceCategories.IsKnown)
            .WithMessage($"หมวดต้องเป็นหนึ่งใน: {string.Join(", ", ServiceCategories.All.Select(c => c.Code))}");

        RuleFor(command => command.DurationMinutes).InclusiveBetween(5, 480);
        RuleFor(command => command.PriceSatang).GreaterThanOrEqualTo(0);

        RuleFor(command => command.Code)
            .Matches("^[a-z0-9][a-z0-9_-]{1,31}$")
            .WithMessage("รหัสบริการต้องเป็น a–z, 0–9, _ หรือ - ยาว 2–32 ตัวอักษร")
            .When(command => command.Code is not null);
    }
}

/// <summary>
/// Adds a service to the catalogue.
/// <para>
/// <b>A new service is not on the public menu unless the request says so.</b> The contract gives
/// online booking its own endpoint (<c>PATCH …/availability</c>), and a service that appeared on
/// the clinic's website the instant it was typed — before its price tiers or its eligible vets
/// were set — would be bookable in a half-configured state. Staff can book it by phone from the
/// moment it exists; the public sees it when someone opens it.
/// </para>
/// <para>
/// When a code is given the id is derived from it, as the contract's own example does
/// (<c>svc_checkup</c>).
/// </para>
/// </summary>
public sealed class CreateServiceHandler(
    IServiceRepository services,
    IStaffRepository staff,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<ServiceDto> HandleAsync(
        CreateServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var eligible = command.EligibleStaffIds?.Distinct(StringComparer.Ordinal).ToArray() ?? [];

        ClinicService? created = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (eligible.Length > 0 && (await staff.MissingAsync(eligible, token)) is { Count: > 0 } missing)
            {
                throw new ValidationFailedException(new Dictionary<string, string[]>
                {
                    ["eligible_staff_ids"] = [$"ไม่พบพนักงานที่ใช้งานอยู่: {string.Join(", ", missing)}"],
                });
            }

            var id = command.Code is { } code
                ? $"{EntityId.Service}_{code}"
                : EntityId.New(EntityId.Service);

            // With no code of its own, a service is known by its id. The column is unique and
            // required, and inventing a slug from a Thai name would be a guess.
            var serviceCode = command.Code ?? id;

            if (await services.CodeExistsAsync(serviceCode, token))
            {
                throw new DuplicateServiceException(serviceCode);
            }

            created = ClinicService.Create(
                serviceCode,
                command.Name,
                command.CategoryCode,
                command.DurationMinutes,
                command.PriceSatang,
                command.OnlineBookingEnabled,
                eligible,
                id);

            services.Add(created);
        }, cancellationToken);

        return ServiceDto.From(created!, DateOnly.FromDateTime(clock.NowInClinicTime().DateTime));
    }
}

/// <summary><c>409 duplicate_service</c> — the code is taken. Not in the API reference's error list.</summary>
public sealed class DuplicateServiceException : AppException
{
    public DuplicateServiceException(string code)
        : base("duplicate_service", "มีบริการที่ใช้รหัสนี้อยู่แล้ว", HttpStatus.Conflict)
    {
        MessageEn = $"A service with code '{code}' already exists.";
        Field = "code";
    }
}
