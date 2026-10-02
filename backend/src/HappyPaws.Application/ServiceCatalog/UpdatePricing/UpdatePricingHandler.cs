using FluentValidation;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.ServiceCatalog.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.ServiceCatalog.UpdatePricing;

/// <summary>
/// <c>PATCH /staff/services/{id}/pricing</c> — "ราคาตามน้ำหนัก/ชนิดสัตว์ · ราคามีผลวันที่".
/// </summary>
/// <param name="ServiceId">The service being priced.</param>
/// <param name="BasePriceSatang">What applies when no tier matches.</param>
/// <param name="Tiers">Prices by kind of animal and weight band, first match wins.</param>
/// <param name="EffectiveFrom">The clinic-local day this list starts. Omitted means today.</param>
/// <param name="IfMatchVersion">From <c>If-Match</c>.</param>
public sealed record UpdatePricingCommand(
    string ServiceId,
    long BasePriceSatang,
    IReadOnlyList<PriceTierInput>? Tiers = null,
    DateOnly? EffectiveFrom = null,
    uint? IfMatchVersion = null);

/// <summary>One tier as the client sends it.</summary>
public sealed record PriceTierInput(
    long PriceSatang,
    string? Species = null,
    decimal? MinWeightKg = null,
    decimal? MaxWeightKg = null);

public sealed class UpdatePricingValidator : AbstractValidator<UpdatePricingCommand>
{
    public UpdatePricingValidator()
    {
        RuleFor(command => command.BasePriceSatang).GreaterThanOrEqualTo(0);

        RuleFor(command => command.Tiers)
            .Must(tiers => tiers is null || tiers.Count <= 50)
            .WithMessage("กำหนดราคาได้ไม่เกิน 50 ขั้น");

        RuleForEach(command => command.Tiers).ChildRules(tier =>
        {
            tier.RuleFor(input => input.PriceSatang).GreaterThanOrEqualTo(0);
            tier.RuleFor(input => input.Species).MaximumLength(60);
            tier.RuleFor(input => input.MinWeightKg).GreaterThanOrEqualTo(0);
            tier.RuleFor(input => input.MaxWeightKg).GreaterThan(0);

            tier.RuleFor(input => input)
                .Must(input => input.MinWeightKg is null
                    || input.MaxWeightKg is null
                    || input.MinWeightKg < input.MaxWeightKg)
                .OverridePropertyName(nameof(PriceTierInput.MaxWeightKg))
                .WithMessage("น้ำหนักสูงสุดต้องมากกว่าน้ำหนักต่ำสุด");

            tier.RuleFor(input => input)
                .Must(input => !string.IsNullOrWhiteSpace(input.Species)
                    || input.MinWeightKg is not null
                    || input.MaxWeightKg is not null)
                .OverridePropertyName(nameof(PriceTierInput.Species))
                .WithMessage("ราคาแต่ละขั้นต้องระบุชนิดสัตว์หรือช่วงน้ำหนักอย่างน้อยหนึ่งอย่าง");
        });
    }
}

/// <summary>
/// Sets a service's price list from a given day.
/// <para>
/// <b>Prices are dated, never overwritten.</b> Today's list stays in force until the new one's day
/// arrives, so next month's prices can be entered now without changing what tomorrow's customer is
/// quoted. Re-sending the same date replaces that list, which is how a typo in a scheduled price is
/// corrected.
/// </para>
/// <para>
/// <b>A price cannot be backdated.</b> A list that started last week would say visits already
/// billed were charged something they were not. Bills are their own record of what was charged —
/// an invoice line carries its price and does not read it back from here — so nothing would break,
/// but the catalogue's history would stop agreeing with them.
/// </para>
/// <para>
/// Nothing consumes the tiers automatically yet. A treatment is entered on a case with the price
/// the vet gives it, and this is the list the counter reads that price from. Applying a tier to a
/// patient's recorded weight belongs with the bill, once medicine reaches it too.
/// </para>
/// </summary>
public sealed class UpdatePricingHandler(
    IServiceRepository services,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<ServiceDto> HandleAsync(
        UpdatePricingCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var today = DateOnly.FromDateTime(clock.NowInClinicTime().DateTime);
        var effectiveFrom = command.EffectiveFrom ?? today;

        if (effectiveFrom < today)
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["effective_from"] = ["ราคาต้องมีผลตั้งแต่วันนี้เป็นต้นไป"] });
        }

        ClinicService? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var service = await services.FindTrackedByIdAsync(command.ServiceId, token)
                ?? throw new NotFoundException("service", command.ServiceId);

            ConcurrencyGuard.Ensure(service, command.IfMatchVersion);

            var before = service.PricingOn(effectiveFrom).BasePriceSatang;

            service.SchedulePricing(
                effectiveFrom,
                command.BasePriceSatang,
                (command.Tiers ?? []).Select(tier =>
                    new PriceTier(tier.Species, tier.MinWeightKg, tier.MaxWeightKg, tier.PriceSatang)));

            audit.Record(new AuditEvent(
                AuditActions.ServicePricingChanged,
                AuditResources.Service,
                service.Id,
                Details: new Dictionary<string, object?>
                {
                    ["effective_from"] = effectiveFrom,
                    ["base_price_satang_before"] = before,
                    ["base_price_satang_after"] = command.BasePriceSatang,
                    ["tiers"] = command.Tiers?.Count ?? 0,
                }));

            updated = service;
        }, cancellationToken);

        return ServiceDto.From(updated!, today);
    }
}
