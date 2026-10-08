using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Inventory;

/// <summary>
/// One lot of one item on one branch's shelf — a lot number, an expiry date and how many units of it
/// are left.
/// <para>
/// <b>Why lots.</b> <c>GET /staff/inventory/alerts</c> asks for "ใกล้หมดอายุ" and "ล็อตที่ต้องเรียกคืน".
/// Both are facts about a lot, not about an item: one box of amoxicillin expires next month while
/// the box behind it is good for two years. A single on-hand number per item could answer neither.
/// </para>
/// <para>
/// <b>The ledger is the history; this is the balance.</b> Every change to
/// <see cref="QuantityOnHand"/> is written together with a <see cref="StockMovement"/> for the same
/// lot and the same signed amount, inside one transaction holding the item's stock lock, so the
/// balance is always the sum of the ledger. The balance is kept rather than summed on every read
/// because issuing first-expiry-first needs it per lot, under the lock, every time. The table also
/// refuses a negative balance outright (<c>ck_stock_lots_quantity_not_negative</c>).
/// </para>
/// </summary>
public sealed class StockLot : Entity
{
    public const int LotNumberMax = 60;

    private StockLot()
    {
    }

    public string ItemId { get; private set; } = string.Empty;

    public string BranchId { get; private set; } = string.Empty;

    /// <summary>The supplier's lot number, when the box has one. Null for stock counted in without one.</summary>
    public string? LotNumber { get; private set; }

    /// <summary>The last clinic day the lot may be used. Null for items that do not expire.</summary>
    public DateOnly? ExpiresOn { get; private set; }

    public int QuantityOnHand { get; private set; }

    /// <summary>When the first units of this lot arrived at this branch.</summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>Expired lots are kept, and counted, but never issued on their own.</summary>
    public bool IsExpiredOn(DateOnly today) => ExpiresOn is { } expires && expires < today;

    public static StockLot Open(
        string itemId,
        string branchId,
        string? lotNumber,
        DateOnly? expiresOn,
        DateTimeOffset receivedAt)
    {
        var number = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim();

        if (number is { Length: > LotNumberMax })
        {
            throw new DomainException("invalid_movement", $"เลขล็อตยาวได้ไม่เกิน {LotNumberMax} ตัวอักษร");
        }

        return new StockLot
        {
            Id = EntityId.New(EntityId.StockLot),
            ItemId = itemId,
            BranchId = branchId,
            LotNumber = number,
            ExpiresOn = expiresOn,
            ReceivedAt = receivedAt,
        };
    }

    /// <summary>Whether a receipt of this lot number and expiry belongs in this lot.</summary>
    public bool Matches(string? lotNumber, DateOnly? expiresOn) =>
        string.Equals(LotNumber, string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim(), StringComparison.Ordinal)
        && ExpiresOn == expiresOn;

    /// <summary>Units in. Only ever called beside the ledger row that records them.</summary>
    internal void Add(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("invalid_movement", "จำนวนต้องมากกว่าศูนย์");
        }

        QuantityOnHand += quantity;
    }

    /// <summary>Units out. Refuses to go below zero: stock that is not there cannot be handed over.</summary>
    internal void Take(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("invalid_movement", "จำนวนต้องมากกว่าศูนย์");
        }

        if (quantity > QuantityOnHand)
        {
            throw new DomainException("insufficient_stock", "จำนวนในล็อตไม่พอ");
        }

        QuantityOnHand -= quantity;
    }

    /// <summary>A stock count: sets the balance to what was counted. Returns the signed difference.</summary>
    internal int SetCounted(int counted)
    {
        if (counted < 0)
        {
            throw new DomainException("invalid_adjustment", "จำนวนที่นับได้ต้องไม่ติดลบ");
        }

        var delta = counted - QuantityOnHand;
        QuantityOnHand = counted;

        return delta;
    }
}
