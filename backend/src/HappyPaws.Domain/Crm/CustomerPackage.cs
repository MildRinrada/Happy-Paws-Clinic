using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Crm;

/// <summary>
/// A package a customer has bought — "แพ็กเกจตรวจ 4 ครั้ง · ตัดสิทธิ์อัตโนมัติเมื่อใช้" — and what is
/// left of it.
/// <para>
/// Read by <c>GET /portal/packages</c>, "สิทธิ์คงเหลือของลูกค้าและวันหมดอายุ". Selling one is
/// <c>POST /staff/packages</c> and using one is the billing step that deducts a use; both belong to
/// the Tier-3 loyalty module and are not built yet, so this is the record they will write and the
/// portal can already read.
/// </para>
/// </summary>
public sealed class CustomerPackage : Entity
{
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

    public DateTimeOffset PurchasedAt { get; private set; }

    /// <summary>The last clinic day it can be used, or null for no expiry.</summary>
    public DateOnly? ExpiresOn { get; private set; }

    public int Remaining => TotalUses - UsedCount;

    public static CustomerPackage Sell(
        string customerId,
        string name,
        IEnumerable<string> serviceIds,
        int totalUses,
        DateTimeOffset purchasedAt,
        DateOnly? expiresOn = null,
        string? petId = null)
    {
        if (string.IsNullOrWhiteSpace(name) || totalUses is < 1 or > 1_000)
        {
            throw new DomainException("invalid_package", "แพ็กเกจต้องมีชื่อและจำนวนครั้ง 1 ถึง 1,000 ครั้ง");
        }

        var package = new CustomerPackage
        {
            Id = EntityId.New(EntityId.Package),
            CustomerId = customerId,
            PetId = petId,
            Name = name.Trim(),
            TotalUses = totalUses,
            PurchasedAt = purchasedAt,
            ExpiresOn = expiresOn,
        };

        package._serviceIds.AddRange(serviceIds.Distinct(StringComparer.Ordinal));

        return package;
    }

    /// <summary>Spends one use. Refused when none is left or the package has expired.</summary>
    public void Use(DateOnly on)
    {
        if (Remaining <= 0 || IsExpiredOn(on))
        {
            throw new DomainException("package_unavailable", "แพ็กเกจนี้ใช้ครบแล้วหรือหมดอายุแล้ว");
        }

        UsedCount++;
    }

    public bool IsExpiredOn(DateOnly day) => ExpiresOn is { } last && day > last;
}
