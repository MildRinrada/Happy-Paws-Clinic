using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.ServiceCatalog;

/// <summary>
/// One line of "ราคาตามน้ำหนัก/ชนิดสัตว์": the price that applies to a kind of animal, a weight
/// band, or both.
/// </summary>
/// <param name="Species">The kind of animal as the clinic records it on a pet — สุนัข, แมว. Null matches any.</param>
/// <param name="MinWeightKg">Inclusive lower bound. Null for no lower bound.</param>
/// <param name="MaxWeightKg">Exclusive upper bound, so 10 kg falls in "10 and over", not in both bands.</param>
/// <param name="PriceSatang">What this line charges.</param>
public sealed record PriceTier(string? Species, decimal? MinWeightKg, decimal? MaxWeightKg, long PriceSatang)
{
    public bool Matches(string? species, decimal? weightKg)
    {
        if (Species is not null
            && !string.Equals(Species, species?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (MinWeightKg is null && MaxWeightKg is null)
        {
            return true;
        }

        // A weight band cannot match an animal nobody has weighed. Falling through to the base
        // price is the honest answer; guessing a band is not.
        return weightKg is { } weight
            && (MinWeightKg is null || weight >= MinWeightKg)
            && (MaxWeightKg is null || weight < MaxWeightKg);
    }

    public void EnsureValid()
    {
        if (PriceSatang < 0)
        {
            throw new DomainException("invalid_pricing", "ราคาต้องไม่ติดลบ");
        }

        if (Species is null && MinWeightKg is null && MaxWeightKg is null)
        {
            throw new DomainException("invalid_pricing", "ราคาแต่ละขั้นต้องระบุชนิดสัตว์หรือช่วงน้ำหนักอย่างน้อยหนึ่งอย่าง");
        }

        if (MinWeightKg < 0 || MaxWeightKg <= 0 || MinWeightKg >= MaxWeightKg)
        {
            throw new DomainException("invalid_pricing", "ช่วงน้ำหนักไม่ถูกต้อง");
        }
    }
}

/// <summary>
/// A service's price list from one date onward — "ราคามีผลวันที่".
/// <para>
/// Dated rather than overwritten, so next month's price can be entered today without changing what
/// today's customer is quoted, and so last month's bill can still be explained.
/// </para>
/// </summary>
public sealed record ServicePriceVersion(
    DateOnly EffectiveFrom,
    long BasePriceSatang,
    IReadOnlyList<PriceTier> Tiers)
{
    /// <summary>The first tier that fits, else the base price. Order is the clinic's, so it decides ties.</summary>
    public long PriceFor(string? species, decimal? weightKg) =>
        Tiers.FirstOrDefault(tier => tier.Matches(species, weightKg))?.PriceSatang ?? BasePriceSatang;
}

/// <summary>
/// A weekly window in which a service may be booked online — the "ช่วงเวลา" half of
/// "จำกัดสาขาหรือช่วงเวลา". Clinic-local wall-clock time.
/// </summary>
/// <param name="Day">Day of the week.</param>
/// <param name="From">Inclusive.</param>
/// <param name="To">Exclusive: a window ending 12:00 does not offer a 12:00 start.</param>
public sealed record BookingWindow(DayOfWeek Day, TimeOnly From, TimeOnly To)
{
    public bool Contains(DateTime clinicLocal) =>
        clinicLocal.DayOfWeek == Day
        && TimeOnly.FromDateTime(clinicLocal) >= From
        && TimeOnly.FromDateTime(clinicLocal) < To;
}
