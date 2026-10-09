using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Crm;

/// <summary>Where a package stands on a given clinic day.</summary>
public enum PackageStatus
{
    /// <summary>Sold, but the bill it was sold on is not fully paid yet. Not usable.</summary>
    PendingPayment,

    Active,

    UsedUp,

    Expired,

    /// <summary>The sale was voided or refunded. Uses already spent stay spent.</summary>
    Cancelled,
}

/// <summary>
/// A package a customer has bought — "แพ็กเกจตรวจ 4 ครั้ง · ตัดสิทธิ์อัตโนมัติเมื่อใช้" — and what is
/// left of it.
/// <para>
/// <b>Sold on a bill, usable once that bill is paid.</b> <c>POST /staff/packages</c> raises an
/// invoice for the price in the same transaction and links it here. Until the invoice reaches
/// <c>paid</c> the package is <see cref="PackageStatus.PendingPayment"/>: a package that could be
/// spent before it was paid for would be credit the clinic never decided to give.
/// </para>
/// <para>
/// <b>Used automatically.</b> When a bill line for one of <see cref="ServiceIds"/> is raised for
/// this customer, the use is spent and the line is charged at zero (see
/// <c>Application/Crm/InvoiceRewards</c>). Each use is a <see cref="PackageUse"/> row, so a voided
/// bill can give its uses back.
/// </para>
/// </summary>
public sealed class CustomerPackage : Entity
{
    public const int MaxUses = 1_000;

    private readonly List<string> _serviceIds = [];

    private CustomerPackage()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>The pet it is for, or null for any of the customer's pets.</summary>
    public string? PetId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>The services a use can be spent on.</summary>
    public IReadOnlyList<string> ServiceIds => _serviceIds;

    public int TotalUses { get; private set; }

    public int UsedCount { get; private set; }

    /// <summary>What the customer is charged for it, on <see cref="InvoiceId"/>.</summary>
    public long PriceSatang { get; private set; }

    /// <summary>The branch that sold it. Null only on rows written before packages were sold through the API.</summary>
    public string? BranchId { get; private set; }

    /// <summary>The bill it was sold on. Null only on rows written before packages were sold through the API.</summary>
    public string? InvoiceId { get; private set; }

    public string? SoldByStaffId { get; private set; }

    public DateTimeOffset PurchasedAt { get; private set; }

    /// <summary>The last clinic day it can be used, or null for no expiry.</summary>
    public DateOnly? ExpiresOn { get; private set; }

    /// <summary>When its bill was paid in full. Null while payment is pending.</summary>
    public DateTimeOffset? ActivatedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancelReason { get; private set; }

    public int Remaining => TotalUses - UsedCount;

    /// <summary><c>POST /staff/packages</c>. The package starts unpaid; see <see cref="Activate"/>.</summary>
    public static CustomerPackage Sell(
        string customerId,
        string name,
        IEnumerable<string> serviceIds,
        int totalUses,
        long priceSatang,
        string branchId,
        string soldByStaffId,
        DateTimeOffset purchasedAt,
        DateOnly? expiresOn = null,
        string? petId = null)
    {
        var services = serviceIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList();

        if (string.IsNullOrWhiteSpace(name) || totalUses is < 1 or > MaxUses)
        {
            throw new DomainException("invalid_package", "แพ็กเกจต้องมีชื่อและจำนวนครั้ง 1 ถึง 1,000 ครั้ง");
        }

        if (services.Count == 0)
        {
            throw new DomainException("invalid_package", "แพ็กเกจต้องระบุบริการที่ใช้สิทธิ์ได้อย่างน้อยหนึ่งบริการ");
        }

        if (priceSatang < 0)
        {
            throw new DomainException("invalid_package", "ราคาแพ็กเกจติดลบไม่ได้");
        }

        var package = new CustomerPackage
        {
            Id = EntityId.New(EntityId.Package),
            CustomerId = customerId,
            PetId = string.IsNullOrWhiteSpace(petId) ? null : petId,
            Name = name.Trim(),
            TotalUses = totalUses,
            PriceSatang = priceSatang,
            BranchId = branchId,
            SoldByStaffId = soldByStaffId,
            PurchasedAt = purchasedAt,
            ExpiresOn = expiresOn,
        };

        package._serviceIds.AddRange(services);

        return package;
    }

    /// <summary>Links the bill the package was sold on. Called once, by the sale.</summary>
    public void AttachInvoice(string invoiceId) => InvoiceId = invoiceId;

    /// <summary>The sale bill reached <c>paid</c>. Idempotent; a cancelled package stays cancelled.</summary>
    public void Activate(DateTimeOffset now)
    {
        if (ActivatedAt is null && CancelledAt is null)
        {
            ActivatedAt = now;
        }
    }

    /// <summary>
    /// The sale was voided or refunded. What is left can no longer be used; uses already spent are
    /// not taken back, because the services were performed. Idempotent.
    /// </summary>
    public void Cancel(string reason, DateTimeOffset now)
    {
        if (CancelledAt is not null)
        {
            return;
        }

        CancelledAt = now;
        CancelReason = reason;
    }

    /// <summary>True when this package pays for <paramref name="serviceId"/> on a bill for <paramref name="petId"/>.</summary>
    public bool Covers(string serviceId, string? petId) =>
        _serviceIds.Contains(serviceId, StringComparer.Ordinal) && (PetId is null || PetId == petId);

    public bool IsUsableOn(DateOnly day) => StatusOn(day) == PackageStatus.Active;

    /// <summary>Spends uses. Refused when the package is not active or has fewer left.</summary>
    public void Use(int quantity, DateOnly on)
    {
        if (quantity < 1 || !IsUsableOn(on) || quantity > Remaining)
        {
            throw new DomainException("package_unavailable", "แพ็กเกจนี้ใช้ครบแล้ว หมดอายุ หรือยังไม่ได้ชำระเงิน");
        }

        UsedCount += quantity;
    }

    /// <summary>Gives uses back, when the bill that spent them is voided.</summary>
    public void Release(int quantity) => UsedCount = Math.Max(0, UsedCount - quantity);

    public bool IsExpiredOn(DateOnly day) => ExpiresOn is { } last && day > last;

    /// <summary>Cancelled, then expired, then unpaid, then used up, then active.</summary>
    public PackageStatus StatusOn(DateOnly day) =>
        CancelledAt is not null ? PackageStatus.Cancelled
            : IsExpiredOn(day) ? PackageStatus.Expired
            : ActivatedAt is null ? PackageStatus.PendingPayment
            : Remaining <= 0 ? PackageStatus.UsedUp
            : PackageStatus.Active;
}
