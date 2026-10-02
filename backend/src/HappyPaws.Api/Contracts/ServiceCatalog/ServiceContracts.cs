namespace HappyPaws.Api.Contracts.ServiceCatalog;

/// <summary>
/// <c>POST /staff/services</c> — "ชื่อ ราคา ระยะเวลา ผู้ให้บริการที่ทำได้".
/// <para>
/// <c>online_booking_enabled</c> defaults to false: a new service is bookable at the counter at
/// once and appears on the public menu only when someone opens it.
/// </para>
/// </summary>
public sealed record CreateServiceRequest(
    string Name,
    string CategoryCode,
    int DurationMinutes,
    long PriceSatang,
    string? Code = null,
    IReadOnlyList<string>? EligibleStaffIds = null,
    bool OnlineBookingEnabled = false);

/// <summary>
/// <c>PATCH /staff/services/{id}/pricing</c> — "ราคาตามน้ำหนัก/ชนิดสัตว์ · ราคามีผลวันที่".
/// <c>effective_from</c> defaults to today and may not be in the past.
/// </summary>
public sealed record UpdatePricingRequest(
    long BasePriceSatang,
    IReadOnlyList<PriceTierRequest>? Tiers = null,
    DateOnly? EffectiveFrom = null);

/// <summary>
/// One price tier. At least one of <c>species</c> and the weight bounds must be given;
/// <c>max_weight_kg</c> is exclusive.
/// </summary>
public sealed record PriceTierRequest(
    long PriceSatang,
    string? Species = null,
    decimal? MinWeightKg = null,
    decimal? MaxWeightKg = null);

/// <summary>
/// <c>PATCH /staff/services/{id}/availability</c> — "เปิด/ปิดจองออนไลน์ · จำกัดสาขาหรือช่วงเวลา".
/// An absent field is left alone; an empty list lifts that restriction.
/// </summary>
public sealed record UpdateAvailabilityRequest(
    bool? OnlineBookingEnabled = null,
    IReadOnlyList<string>? BranchIds = null,
    IReadOnlyList<BookingWindowRequest>? BookingWindows = null);

/// <summary>One weekly window: <c>{ "day": "saturday", "from": "09:00", "to": "12:00" }</c>.</summary>
public sealed record BookingWindowRequest(string Day, string From, string To);
