using HappyPaws.Application.Clinics;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.ServiceCatalog.Dtos;

/// <summary>One line of a price list: a kind of animal, a weight band, or both.</summary>
public sealed record PriceTierDto(string? Species, decimal? MinWeightKg, decimal? MaxWeightKg, long PriceSatang)
{
    public static PriceTierDto From(PriceTier tier) =>
        new(tier.Species, tier.MinWeightKg, tier.MaxWeightKg, tier.PriceSatang);
}

/// <summary>
/// A price list and the day it takes effect. <c>EffectiveFrom</c> is absent on the price a service
/// was created with, which has been in force since the beginning.
/// </summary>
public sealed record ServicePricingDto(
    DateOnly? EffectiveFrom,
    long BasePriceSatang,
    IReadOnlyList<PriceTierDto> Tiers)
{
    public static ServicePricingDto From(ServicePriceVersion version) =>
        new(
            version.EffectiveFrom == DateOnly.MinValue ? null : version.EffectiveFrom,
            version.BasePriceSatang,
            [.. version.Tiers.Select(PriceTierDto.From)]);
}

/// <summary>A weekly window for online booking — <c>{ "day": "monday", "from": "09:00", "to": "12:00" }</c>.</summary>
public sealed record BookingWindowDto(string Day, string From, string To)
{
    public static BookingWindowDto FromWindow(BookingWindow window) =>
        new(WireTime.Format(window.Day), WireTime.Format(window.From), WireTime.Format(window.To));
}

/// <summary>
/// A service as staff manage it — what the three <c>service:manage</c> endpoints return.
/// <para>
/// <c>PriceSatang</c> is the base price in force today and <c>Pricing</c> is the whole list in
/// force today; <c>UpcomingPricing</c> holds the lists entered for a later date, soonest first.
/// </para>
/// </summary>
public sealed record ServiceDto(
    string Id,
    uint Version,
    string Code,
    string Name,
    string CategoryCode,
    int DurationMinutes,
    long PriceSatang,
    bool OnlineBookingEnabled,
    bool IsActive,
    IReadOnlyList<string> EligibleStaffIds,
    IReadOnlyList<string> BranchIds,
    IReadOnlyList<BookingWindowDto> BookingWindows,
    ServicePricingDto Pricing,
    IReadOnlyList<ServicePricingDto> UpcomingPricing)
{
    public static ServiceDto From(ClinicService service, DateOnly today)
    {
        var current = service.PricingOn(today);

        return new ServiceDto(
            service.Id,
            service.Version,
            service.Code,
            service.Name,
            service.CategoryCode,
            service.DurationMinutes,
            current.BasePriceSatang,
            service.OnlineBookingEnabled,
            service.IsActive,
            service.EligibleStaffIds,
            service.BranchIds,
            [.. service.BookingWindows.Select(BookingWindowDto.FromWindow)],
            ServicePricingDto.From(current),
            [.. service.PricingAfter(today).Select(ServicePricingDto.From)]);
    }
}

/// <summary>
/// A service on the public menu — <c>GET /services</c>.
/// <para>
/// Narrower than <see cref="ServiceDto"/> on purpose. An unauthenticated caller is shown what a
/// booking page needs — what it is, how long it takes, what it costs, where and when it can be
/// booked — and not the staff who may perform it, next month's prices, or a version to write with.
/// </para>
/// </summary>
public sealed record PublicServiceDto(
    string Id,
    string Code,
    string Name,
    string CategoryCode,
    int DurationMinutes,
    long PriceSatang,
    IReadOnlyList<PriceTierDto> PriceTiers,
    IReadOnlyList<string> BranchIds,
    IReadOnlyList<BookingWindowDto> BookingWindows)
{
    public static PublicServiceDto From(ClinicService service, DateOnly today)
    {
        var current = service.PricingOn(today);

        return new PublicServiceDto(
            service.Id,
            service.Code,
            service.Name,
            service.CategoryCode,
            service.DurationMinutes,
            current.BasePriceSatang,
            [.. current.Tiers.Select(PriceTierDto.From)],
            service.BranchIds,
            [.. service.BookingWindows.Select(BookingWindowDto.FromWindow)]);
    }
}

/// <summary>One heading — <c>GET /services/categories</c>.</summary>
public sealed record ServiceCategoryDto(string Code, string Name, string NameEn)
{
    public static ServiceCategoryDto From(ServiceCategory category) =>
        new(category.Code, category.Name, category.NameEn);
}
