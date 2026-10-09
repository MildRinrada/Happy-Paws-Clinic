using System.Text;
using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Inventory;

/// <summary>What kind of thing an item is — "ยาและเวชภัณฑ์", medicines and supplies.</summary>
public enum InventoryItemKind
{
    /// <summary>A drug: what a prescription names and a dispense hands over.</summary>
    Medicine,

    /// <summary>A consumable — syringes, gauze, a bag of fluid. Stocked and counted, never prescribed.</summary>
    Supply,
}

/// <summary>
/// One entry in the clinic's catalogue of drugs and supplies — a row of
/// <c>GET /staff/inventory/items</c>, "ยาและเวชภัณฑ์ · คงเหลือรายสาขา".
/// <para>
/// <b>Clinic-wide, stocked per branch.</b> The item is the thing ("Amoxicillin 250mg tab", sold by
/// the tablet at ฿5); how many of it each branch holds lives in <see cref="StockLot"/> rows. One
/// catalogue means one price and one name to match a prescription against, wherever it is filled.
/// </para>
/// <para>
/// <b>How items come to exist.</b> The contract has no endpoint that creates an item. An item is
/// created the first time it is ordered or received — a purchase-order line or a receiving movement
/// may name a new item instead of an existing one. See <c>docs/assumptions.md § 42</c>.
/// </para>
/// <para>
/// <b>Matching a prescription.</b> A prescription records a drug as free text. It is matched to an
/// item by <see cref="NameKey"/>: the name with its spacing and case flattened, compared exactly.
/// No first-word or fuzzy match — a wrong match would take the wrong drug off the shelf and charge
/// its price, which is worse than taking nothing and saying so.
/// </para>
/// </summary>
public sealed class InventoryItem : Entity
{
    public const int NameMax = 200;
    public const int UnitMax = 40;
    public const int MaxReorderLevel = 1_000_000;

    /// <summary>One baht short of ten million: a ceiling that catches a price typed in satang twice.</summary>
    public const long MaxUnitPriceSatang = 999_999_900;

    private InventoryItem()
    {
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// <see cref="Name"/> as matched — see <see cref="KeyOf"/>. Unique across the catalogue, so a
    /// prescription can match at most one item.
    /// </summary>
    public string NameKey { get; private set; } = string.Empty;

    public InventoryItemKind Kind { get; private set; }

    /// <summary>What one unit is called — <c>เม็ด</c>, <c>ขวด</c>, <c>ml</c>. A prescription's quantity counts these.</summary>
    public string Unit { get; private set; } = string.Empty;

    /// <summary>
    /// What one unit is sold for, in satang. Charged when it is dispensed; zero means it is given
    /// away and no line is written for it.
    /// </summary>
    public long UnitPriceSatang { get; private set; }

    /// <summary>
    /// "ใกล้หมด" — at or below this many usable units a branch is short of the item. Zero turns the
    /// low-stock warning off; running out is still reported.
    /// </summary>
    public int ReorderLevel { get; private set; }

    public string CreatedByStaffId { get; private set; } = string.Empty;

    /// <summary>
    /// When the item was retired, or null while it is in use. A retired item is not offered to
    /// prescribers, cannot be picked on a new prescription, received or ordered, and drops out of
    /// the low-stock and out-of-stock warnings. Units still on the shelf can be issued, moved,
    /// counted and dispensed, and every past record keeps pointing at it. Not in the API reference:
    /// see <c>docs/contract-extensions.md § 25</c>.
    /// </summary>
    public DateTimeOffset? RetiredAt { get; private set; }

    public bool IsRetired => RetiredAt is not null;

    public static InventoryItem Create(
        string name,
        InventoryItemKind kind,
        string unit,
        long unitPriceSatang,
        int reorderLevel,
        string createdByStaffId)
    {
        var trimmed = Collapse(name);

        if (trimmed.Length == 0 || trimmed.Length > NameMax)
        {
            throw new DomainException("invalid_inventory_item", $"ชื่อรายการต้องมี 1 ถึง {NameMax} ตัวอักษร");
        }

        if (string.IsNullOrWhiteSpace(unit) || unit.Trim().Length > UnitMax)
        {
            throw new DomainException("invalid_inventory_item", $"หน่วยต้องมี 1 ถึง {UnitMax} ตัวอักษร");
        }

        if (unitPriceSatang is < 0 or > MaxUnitPriceSatang)
        {
            throw new DomainException("invalid_inventory_item", "ราคาขายต่อหน่วยไม่ถูกต้อง");
        }

        if (reorderLevel is < 0 or > MaxReorderLevel)
        {
            throw new DomainException("invalid_inventory_item", "จุดสั่งซื้อต้องไม่ติดลบ");
        }

        return new InventoryItem
        {
            Id = EntityId.New(EntityId.InventoryItem),
            Name = trimmed,
            NameKey = KeyOf(trimmed),
            Kind = kind,
            Unit = unit.Trim(),
            UnitPriceSatang = unitPriceSatang,
            ReorderLevel = reorderLevel,
            CreatedByStaffId = createdByStaffId,
        };
    }

    /// <summary>
    /// The form a name is matched in: Unicode-normalised, runs of white space collapsed to one
    /// space, trimmed and lower-cased. "Amoxicillin  250mg" and "amoxicillin 250mg" are one key;
    /// "Amoxicillin 250 mg" is another, on purpose.
    /// </summary>
    public static string KeyOf(string name) => Collapse(name).ToLowerInvariant();

    /// <summary>Whether a drug name as written on a prescription names this item.</summary>
    public bool IsNamedBy(string drugName) => KeyOf(drugName) == NameKey;

    /// <summary>
    /// Renames the item. The match key follows, so a typed prescription not yet dispensed matches
    /// the new name, not the old one; prescriptions picked from stock carry the id and are unaffected.
    /// The caller checks the new key is free; the unique index is the backstop.
    /// </summary>
    public void Rename(string name)
    {
        var trimmed = Collapse(name);

        if (trimmed.Length == 0 || trimmed.Length > NameMax)
        {
            throw new DomainException("invalid_inventory_item", $"ชื่อรายการต้องมี 1 ถึง {NameMax} ตัวอักษร");
        }

        Name = trimmed;
        NameKey = KeyOf(trimmed);
    }

    /// <summary>
    /// Changes what a unit is called. A relabel, not a conversion: quantities on the books are not
    /// touched, so it is for correcting a unit, not for repacking.
    /// </summary>
    public void ChangeUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit) || unit.Trim().Length > UnitMax)
        {
            throw new DomainException("invalid_inventory_item", $"หน่วยต้องมี 1 ถึง {UnitMax} ตัวอักษร");
        }

        Unit = unit.Trim();
    }

    /// <summary>
    /// The selling price from now on. A dispense records the price it charged on the prescription,
    /// so what has been dispensed keeps its price.
    /// </summary>
    public void Reprice(long unitPriceSatang)
    {
        if (unitPriceSatang is < 0 or > MaxUnitPriceSatang)
        {
            throw new DomainException("invalid_inventory_item", "ราคาขายต่อหน่วยไม่ถูกต้อง");
        }

        UnitPriceSatang = unitPriceSatang;
    }

    public void ChangeReorderLevel(int reorderLevel)
    {
        if (reorderLevel is < 0 or > MaxReorderLevel)
        {
            throw new DomainException("invalid_inventory_item", "จุดสั่งซื้อต้องไม่ติดลบ");
        }

        ReorderLevel = reorderLevel;
    }

    public void Retire(DateTimeOffset now) => RetiredAt ??= now;

    public void Reactivate() => RetiredAt = null;

    /// <summary>Refuses what only an item in use may do: be received, ordered or newly prescribed.</summary>
    public void EnsureInUse()
    {
        if (IsRetired)
        {
            throw new DomainException("inventory_item_retired", $"{Name} เลิกใช้แล้ว เปิดใช้อีกครั้งก่อน");
        }
    }

    private static string Collapse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalised = value.Normalize(NormalizationForm.FormKC);
        return string.Join(' ', normalised.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
