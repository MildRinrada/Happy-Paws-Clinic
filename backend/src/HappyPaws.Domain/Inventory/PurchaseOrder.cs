using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Inventory;

/// <summary>Where an order stands. Derived from what has been received; never set from outside.</summary>
public enum PurchaseOrderStatus
{
    /// <summary>Sent to the supplier; nothing has arrived.</summary>
    Ordered,

    /// <summary>"รับของบางส่วนได้" — some lines, or part of a line, have arrived.</summary>
    PartiallyReceived,

    /// <summary>Everything ordered has arrived.</summary>
    Received,

    /// <summary>
    /// The rest will not come: whatever was outstanding when the order was cancelled is off the
    /// order. What had arrived stays received. Not in the API reference:
    /// <c>docs/contract-extensions.md § 25</c>.
    /// </summary>
    Cancelled,
}

/// <summary>
/// An order to a supplier — <c>POST /staff/inventory/purchase-orders</c>, "สั่งซื้อจากซัพพลายเออร์ ·
/// รับของบางส่วนได้".
/// <para>
/// <b>An order is not stock.</b> Raising one changes nothing on the shelf. Goods arrive through
/// <c>POST /staff/inventory/movements</c> as a receipt that names the order, which is how "รับของ
/// บางส่วนได้" works without an endpoint of its own: each receipt adds to a line's
/// <see cref="PurchaseOrderLine.ReceivedQuantity"/>, and <see cref="Status"/> follows.
/// </para>
/// <para>
/// Like an invoice, the status is recomputed from the lines after every receipt, so it can never say
/// <c>received</c> over a line that is still outstanding.
/// </para>
/// </summary>
public sealed class PurchaseOrder : Entity
{
    public const int SupplierMax = 200;
    public const int NoteMax = 1_000;
    public const int MaxLines = 50;

    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder()
    {
    }

    /// <summary>The running document number, e.g. <c>PO-202610-0007</c>. Unique across the clinic.</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>The branch the goods are for, and the only branch a receipt against it may land in.</summary>
    public string BranchId { get; private set; } = string.Empty;

    public string SupplierName { get; private set; } = string.Empty;

    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Ordered;

    public DateOnly? ExpectedOn { get; private set; }

    public string? Note { get; private set; }

    public string OrderedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset OrderedAt { get; private set; }

    public IReadOnlyList<PurchaseOrderLine> Lines => _lines;

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancelledByStaffId { get; private set; }

    public string? CancelReason { get; private set; }

    public bool IsCancelled => Status == PurchaseOrderStatus.Cancelled;

    public Money TotalCost => _lines.Aggregate(Money.Zero, (total, line) => total + line.LineTotal);

    public static PurchaseOrder Raise(
        string number,
        string branchId,
        string supplierName,
        string orderedByStaffId,
        DateTimeOffset orderedAt,
        DateOnly? expectedOn = null,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(supplierName) || supplierName.Trim().Length > SupplierMax)
        {
            throw new DomainException("invalid_purchase_order", $"ชื่อซัพพลายเออร์ต้องมี 1 ถึง {SupplierMax} ตัวอักษร");
        }

        return new PurchaseOrder
        {
            Id = EntityId.New(EntityId.PurchaseOrder),
            Number = number,
            BranchId = branchId,
            SupplierName = supplierName.Trim(),
            OrderedByStaffId = orderedByStaffId,
            OrderedAt = orderedAt,
            ExpectedOn = expectedOn,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
    }

    /// <summary>One item on the order. An item appears on an order once; a second line for it is refused.</summary>
    public PurchaseOrderLine AddLine(string itemId, int quantity, long unitCostSatang)
    {
        if (_lines.Count >= MaxLines)
        {
            throw new DomainException("invalid_purchase_order", $"ใบสั่งซื้อหนึ่งใบมีได้ไม่เกิน {MaxLines} รายการ");
        }

        if (_lines.Any(line => line.ItemId == itemId))
        {
            throw new DomainException("invalid_purchase_order", "รายการเดียวกันอยู่ในใบสั่งซื้อซ้ำ");
        }

        var line = PurchaseOrderLine.Create(Id, itemId, quantity, unitCostSatang);
        _lines.Add(line);

        return line;
    }

    /// <summary>The line for an item, or null when the order does not include it.</summary>
    public PurchaseOrderLine? LineFor(string itemId) => _lines.FirstOrDefault(line => line.ItemId == itemId);

    /// <summary>
    /// Records goods arriving against the order. Refuses more than is outstanding on the line: a
    /// delivery bigger than the order is a different conversation with the supplier, and recording
    /// it here would leave the order claiming it asked for what it did not.
    /// </summary>
    public PurchaseOrderLine Receive(string itemId, int quantity)
    {
        if (IsCancelled)
        {
            throw new StateConflictException(nameof(PurchaseOrder), "cancelled", nameof(Receive));
        }

        var line = LineFor(itemId)
            ?? throw new DomainException("invalid_movement", "ใบสั่งซื้อนี้ไม่มีรายการนี้");

        line.Receive(quantity);
        Recompute();

        return line;
    }

    /// <summary>
    /// Calls off what has not arrived. Allowed while anything is outstanding, whether nothing or part
    /// has been received; a fully received order has nothing left to cancel. Lines keep their received
    /// quantities, and receipts already booked stay in stock.
    /// </summary>
    public void Cancel(string staffId, string? reason, DateTimeOffset now)
    {
        if (Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
        {
            throw new StateConflictException(nameof(PurchaseOrder), Status.ToString(), nameof(Cancel));
        }

        Status = PurchaseOrderStatus.Cancelled;
        CancelledAt = now;
        CancelledByStaffId = staffId;
        CancelReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    private void Recompute() =>
        Status = _lines.All(line => line.Outstanding == 0) ? PurchaseOrderStatus.Received
            : _lines.Any(line => line.ReceivedQuantity > 0) ? PurchaseOrderStatus.PartiallyReceived
            : PurchaseOrderStatus.Ordered;
}

/// <summary>One item on a purchase order: how many were asked for, at what cost, and how many came.</summary>
public sealed class PurchaseOrderLine
{
    public const int MaxQuantity = 1_000_000;

    private PurchaseOrderLine()
    {
    }

    public string Id { get; private set; } = string.Empty;

    public string PurchaseOrderId { get; private set; } = string.Empty;

    public string ItemId { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public long UnitCostSatang { get; private set; }

    public int ReceivedQuantity { get; private set; }

    public int Outstanding => Quantity - ReceivedQuantity;

    public Money LineTotal => new(UnitCostSatang * Quantity);

    internal static PurchaseOrderLine Create(string purchaseOrderId, string itemId, int quantity, long unitCostSatang)
    {
        if (quantity is <= 0 or > MaxQuantity)
        {
            throw new DomainException("invalid_purchase_order", "จำนวนสั่งซื้อต้องมากกว่าศูนย์");
        }

        if (unitCostSatang is < 0 or > InventoryItem.MaxUnitPriceSatang)
        {
            throw new DomainException("invalid_purchase_order", "ต้นทุนต่อหน่วยไม่ถูกต้อง");
        }

        return new PurchaseOrderLine
        {
            Id = EntityId.New(EntityId.PurchaseOrderLine),
            PurchaseOrderId = purchaseOrderId,
            ItemId = itemId,
            Quantity = quantity,
            UnitCostSatang = unitCostSatang,
        };
    }

    internal void Receive(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("invalid_movement", "จำนวนต้องมากกว่าศูนย์");
        }

        if (quantity > Outstanding)
        {
            throw new DomainException("over_receipt", $"รับเกินจำนวนที่ค้างในใบสั่งซื้อ (ค้างอยู่ {Outstanding})");
        }

        ReceivedQuantity += quantity;
    }
}
