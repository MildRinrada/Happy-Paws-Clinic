using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.ServiceCatalog;

/// <summary>
/// A bookable service — <c>svc_checkup</c>. "หมวด ราคา ระยะเวลา และการเปิดให้จองออนไลน์".
/// <para>
/// <see cref="DurationMinutes"/> is what turns a slot start into a slot end, so it is the single
/// input that decides whether two appointments collide. <see cref="OnlineBookingEnabled"/> is the
/// contract's "เปิด/ปิดจองออนไลน์": it gates the public availability feed and customer booking, and
/// deliberately does not gate staff booking, who take these by phone.
/// </para>
/// <para>
/// Named <c>ClinicService</c> rather than <c>Service</c> so it cannot be confused with an
/// application service in a file list — this is a priced catalogue item, not a class of logic.
/// </para>
/// </summary>
public sealed class ClinicService : Entity
{
    private readonly List<string> _eligibleStaffIds = [];
    private readonly List<string> _branchIds = [];
    private readonly List<BookingWindow> _bookingWindows = [];
    private readonly List<ServicePriceVersion> _priceSchedule = [];

    private ClinicService()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>ตรวจ · วัคซีน · ทันตกรรม · ผ่าตัด · แล็บ · กรูมมิ่ง.</summary>
    public string CategoryCode { get; private set; } = string.Empty;

    public int DurationMinutes { get; private set; }

    /// <summary>
    /// The price the service was created with, in satang. What a customer is quoted on a given day
    /// comes from <see cref="PricingOn"/>, which falls back to this when no dated price applies.
    /// </summary>
    public long PriceSatang { get; private set; }

    public bool OnlineBookingEnabled { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Staff who may perform this service ("ผู้ให้บริการที่ทำได้"). Empty means any vet on shift —
    /// which is the common case, and avoids forcing every clinic to maintain a matrix on day one.
    /// </summary>
    public IReadOnlyList<string> EligibleStaffIds => _eligibleStaffIds;

    /// <summary>
    /// The branches that offer this service ("จำกัดสาขา"). Empty means every branch, for the same
    /// reason <see cref="EligibleStaffIds"/> does.
    /// </summary>
    public IReadOnlyList<string> BranchIds => _branchIds;

    /// <summary>
    /// When the service may be booked <b>online</b> ("จำกัด…ช่วงเวลา"). Empty means any time a vet
    /// is on shift. Like the online switch itself, this does not bind the counter.
    /// </summary>
    public IReadOnlyList<BookingWindow> BookingWindows => _bookingWindows;

    /// <summary>Dated price lists, oldest first. See <see cref="ServicePriceVersion"/>.</summary>
    public IReadOnlyList<ServicePriceVersion> PriceSchedule => _priceSchedule;

    public static ClinicService Create(
        string code,
        string name,
        string categoryCode,
        int durationMinutes,
        long priceSatang,
        bool onlineBookingEnabled = true,
        IEnumerable<string>? eligibleStaffIds = null,
        string? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("invalid_service", "ต้องระบุชื่อบริการ");
        }

        if (durationMinutes is < 5 or > 480)
        {
            throw new DomainException("invalid_service", "ระยะเวลาบริการต้องอยู่ระหว่าง 5–480 นาที");
        }

        if (priceSatang < 0)
        {
            throw new DomainException("invalid_service", "ราคาต้องไม่ติดลบ");
        }

        var service = new ClinicService
        {
            Id = id ?? EntityId.New(EntityId.Service),
            Code = code.Trim(),
            Name = name.Trim(),
            CategoryCode = categoryCode.Trim(),
            DurationMinutes = durationMinutes,
            PriceSatang = priceSatang,
            OnlineBookingEnabled = onlineBookingEnabled,
        };

        if (eligibleStaffIds is not null)
        {
            service._eligibleStaffIds.AddRange(eligibleStaffIds.Distinct(StringComparer.Ordinal));
        }

        return service;
    }

    /// <summary>True when the given staff member may perform this service.</summary>
    public bool CanBePerformedBy(string staffId) =>
        _eligibleStaffIds.Count == 0 || _eligibleStaffIds.Contains(staffId);

    /// <summary>True when the given branch offers this service.</summary>
    public bool IsOfferedAt(string branchId) =>
        _branchIds.Count == 0 || _branchIds.Contains(branchId);

    /// <summary>
    /// True when a slot starting at this clinic-local time may be taken online. Judged on the
    /// start: a window of 09:00–12:00 means "mornings", and a customer reads an 11:45 slot as a
    /// morning one even though the visit runs past noon.
    /// </summary>
    public bool IsBookableOnlineAt(DateTime clinicLocalStart) =>
        _bookingWindows.Count == 0 || _bookingWindows.Any(window => window.Contains(clinicLocalStart));

    public void SetOnlineBooking(bool enabled) => OnlineBookingEnabled = enabled;

    public void RestrictToBranches(IEnumerable<string> branchIds)
    {
        _branchIds.Clear();
        _branchIds.AddRange(branchIds.Distinct(StringComparer.Ordinal));
    }

    public void SetBookingWindows(IEnumerable<BookingWindow> windows)
    {
        var replacement = windows.ToList();

        if (replacement.Any(window => window.To <= window.From))
        {
            throw new DomainException("invalid_service_availability", "เวลาสิ้นสุดของช่วงเวลาจองต้องอยู่หลังเวลาเริ่ม");
        }

        _bookingWindows.Clear();
        _bookingWindows.AddRange(replacement
            .Distinct()
            .OrderBy(window => ((int)window.Day + 6) % 7)
            .ThenBy(window => window.From));
    }

    /// <summary>
    /// <c>PATCH /staff/services/{id}/pricing</c> — "ราคาตามน้ำหนัก/ชนิดสัตว์ · ราคามีผลวันที่".
    /// <para>
    /// Adds a dated price list. Re-sending the same date replaces that list rather than stacking a
    /// second one, so correcting a typo in next month's prices does not leave the typo in force for
    /// whoever reads the schedule top-down.
    /// </para>
    /// </summary>
    public void SchedulePricing(DateOnly effectiveFrom, long basePriceSatang, IEnumerable<PriceTier> tiers)
    {
        if (basePriceSatang < 0)
        {
            throw new DomainException("invalid_pricing", "ราคาต้องไม่ติดลบ");
        }

        var lines = tiers
            .Select(tier => tier with { Species = string.IsNullOrWhiteSpace(tier.Species) ? null : tier.Species.Trim() })
            .ToList();

        foreach (var line in lines)
        {
            line.EnsureValid();
        }

        _priceSchedule.RemoveAll(version => version.EffectiveFrom == effectiveFrom);
        _priceSchedule.Add(new ServicePriceVersion(effectiveFrom, basePriceSatang, lines));
        _priceSchedule.Sort((first, second) => first.EffectiveFrom.CompareTo(second.EffectiveFrom));
    }

    /// <summary>
    /// The price list in force on a clinic-local day: the newest dated list that has started, or
    /// the creation price when none has.
    /// </summary>
    public ServicePriceVersion PricingOn(DateOnly date) =>
        _priceSchedule.LastOrDefault(version => version.EffectiveFrom <= date)
        ?? new ServicePriceVersion(DateOnly.MinValue, PriceSatang, []);

    /// <summary>Price lists entered for the future, soonest first.</summary>
    public IReadOnlyList<ServicePriceVersion> PricingAfter(DateOnly date) =>
        [.. _priceSchedule.Where(version => version.EffectiveFrom > date)];
}
