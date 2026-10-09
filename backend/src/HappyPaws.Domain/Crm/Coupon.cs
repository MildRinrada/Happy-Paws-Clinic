using System.Security.Cryptography;
using System.Text.RegularExpressions;
using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Crm;

/// <summary>
/// A discount code — <c>POST /staff/coupons</c>, "โค้ดส่วนลด · จำกัดบริการ จำนวน และช่วงเวลา".
/// <para>
/// The three limits in the contract's line are the three guards here: which services it takes
/// money off (<see cref="ServiceIds"/>, empty for the whole bill), how many times it can be used
/// (<see cref="MaxRedemptions"/>, and <see cref="PerCustomerLimit"/> per customer), and when
/// (<see cref="ValidFrom"/> to <see cref="ValidUntil"/>, clinic days, both inclusive).
/// </para>
/// <para>
/// It is redeemed at the cash desk as a discount on a bill (<c>POST /staff/invoices/{id}/discounts</c>
/// with <c>coupon_code</c>). Whoever created the coupon is recorded as the discount's approver: the
/// approval was given when the code was issued.
/// </para>
/// </summary>
public sealed partial class Coupon : Entity
{
    public const int MaxServices = 50;

    private const string CodeAlphabet = "23456789ABCDEFGHJKMNPQRSTVWXYZ";

    private readonly List<string> _serviceIds = [];

    private Coupon()
    {
    }

    /// <summary>Upper case, letters, digits and hyphens. Unique across the clinic.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>A rate off the eligible amount, or null when the coupon is a fixed amount.</summary>
    public decimal? Percent { get; private set; }

    /// <summary>A fixed amount off, capped at the eligible amount, or null when the coupon is a rate.</summary>
    public long? AmountSatang { get; private set; }

    /// <summary>The services it applies to. Empty means the whole bill.</summary>
    public IReadOnlyList<string> ServiceIds => _serviceIds;

    /// <summary>How many times it can be used in total. Null for no limit.</summary>
    public int? MaxRedemptions { get; private set; }

    /// <summary>How many times one customer can use it. Null for no limit.</summary>
    public int? PerCustomerLimit { get; private set; }

    public DateOnly ValidFrom { get; private set; }

    public DateOnly? ValidUntil { get; private set; }

    /// <summary>The one branch it can be used at, or null for every branch.</summary>
    public string? BranchId { get; private set; }

    public string CreatedByStaffId { get; private set; } = string.Empty;

    /// <summary>Uses still standing. A voided bill gives its use back.</summary>
    public int RedeemedCount { get; private set; }

    public static Coupon Create(
        string code,
        string name,
        decimal? percent,
        long? amountSatang,
        IEnumerable<string> serviceIds,
        int? maxRedemptions,
        int? perCustomerLimit,
        DateOnly validFrom,
        DateOnly? validUntil,
        string? branchId,
        string createdByStaffId)
    {
        var normalized = NormalizeCode(code);

        if (!IsWellFormedCode(normalized))
        {
            throw new DomainException("invalid_coupon", "โค้ดต้องเป็นตัวอักษรอังกฤษ ตัวเลข หรือขีด 4 ถึง 20 ตัว");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("invalid_coupon", "คูปองต้องมีชื่อ");
        }

        if (percent.HasValue == amountSatang.HasValue)
        {
            throw new DomainException("invalid_coupon", "ระบุส่วนลดเป็นเปอร์เซ็นต์หรือจำนวนเงินอย่างใดอย่างหนึ่ง");
        }

        if (percent is <= 0 or > 100 || amountSatang is <= 0)
        {
            throw new DomainException("invalid_coupon", "ส่วนลดต้องมากกว่าศูนย์ และเปอร์เซ็นต์ไม่เกิน 100");
        }

        if (validUntil is { } until && until < validFrom)
        {
            throw new DomainException("invalid_coupon", "วันสิ้นสุดต้องไม่ก่อนวันเริ่ม");
        }

        if (maxRedemptions is < 1 || perCustomerLimit is < 1)
        {
            throw new DomainException("invalid_coupon", "จำนวนครั้งที่ใช้ได้ต้องอย่างน้อย 1");
        }

        var coupon = new Coupon
        {
            Id = EntityId.New(EntityId.Coupon),
            Code = normalized,
            Name = name.Trim(),
            Percent = percent,
            AmountSatang = amountSatang,
            MaxRedemptions = maxRedemptions,
            PerCustomerLimit = perCustomerLimit,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            BranchId = string.IsNullOrWhiteSpace(branchId) ? null : branchId,
            CreatedByStaffId = createdByStaffId,
        };

        coupon._serviceIds.AddRange(serviceIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal));

        return coupon;
    }

    /// <summary>Codes are matched without regard to case or surrounding spaces.</summary>
    public static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    public static bool IsWellFormedCode(string normalizedCode) => CodePattern().IsMatch(normalizedCode);

    /// <summary>
    /// Eight characters with no 0/O, 1/I/L or U, so a code read aloud at the counter is typed right.
    /// </summary>
    public static string GenerateCode()
    {
        Span<char> chars = stackalloc char[8];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        }

        return new string(chars);
    }

    /// <summary>
    /// Refuses a use that breaks one of the coupon's limits. Checked under the coupon's lock, so the
    /// count read here is the count every other redemption sees.
    /// </summary>
    public void EnsureRedeemable(DateOnly day, string branchId, int customerRedemptions)
    {
        if (day < ValidFrom)
        {
            throw new DomainException("coupon_not_started", $"คูปองนี้เริ่มใช้ได้วันที่ {ValidFrom:yyyy-MM-dd}");
        }

        if (ValidUntil is { } until && day > until)
        {
            throw new DomainException("coupon_expired", "คูปองนี้หมดอายุแล้ว");
        }

        if (MaxRedemptions is { } max && RedeemedCount >= max)
        {
            throw new DomainException("coupon_used_up", "คูปองนี้ถูกใช้ครบจำนวนแล้ว");
        }

        if (PerCustomerLimit is { } limit && customerRedemptions >= limit)
        {
            throw new DomainException("coupon_customer_limit", "ลูกค้าคนนี้ใช้คูปองนี้ครบจำนวนแล้ว");
        }

        if (BranchId is { } only && only != branchId)
        {
            throw new DomainException("coupon_not_applicable", "คูปองนี้ใช้ได้เฉพาะสาขาอื่น");
        }
    }

    /// <summary>
    /// What the coupon takes off an eligible amount. A rate rounds down to the satang, like a manual
    /// percentage discount; a fixed amount is capped at what it applies to.
    /// </summary>
    public long DiscountFor(long eligibleSatang)
    {
        if (eligibleSatang <= 0)
        {
            return 0;
        }

        return Percent is { } rate
            ? (long)Math.Floor(eligibleSatang * rate / 100m)
            : Math.Min(AmountSatang!.Value, eligibleSatang);
    }

    public void Redeem() => RedeemedCount++;

    public void Release() => RedeemedCount = Math.Max(0, RedeemedCount - 1);

    [GeneratedRegex("^[A-Z0-9-]{4,20}$")]
    private static partial Regex CodePattern();
}
