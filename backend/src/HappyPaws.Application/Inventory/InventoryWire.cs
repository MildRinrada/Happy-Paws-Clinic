namespace HappyPaws.Application.Inventory;

/// <summary>
/// The three things <c>POST /staff/inventory/movements</c> records — "รับเข้า จ่ายออก โอนสาขา".
/// A transfer is one request but two ledger rows per lot, which is why this is not the ledger's own
/// <see cref="Domain.Inventory.StockMovementKind"/>.
/// </summary>
public enum MovementType
{
    Receive,
    Issue,
    Transfer,
}

/// <summary>The kinds of row on <c>GET /staff/inventory/alerts</c> — "ใกล้หมด ใกล้หมดอายุ ล็อตที่ต้องเรียกคืน".</summary>
public enum InventoryAlertKind
{
    /// <summary>A lot past its expiry with units still on the books: take it off the shelf.</summary>
    Expired,

    /// <summary>No usable units left at the branch.</summary>
    OutOfStock,

    /// <summary>A lot that expires within the window.</summary>
    NearExpiry,

    /// <summary>Usable units at or below the item's reorder level.</summary>
    LowStock,
}

/// <summary>What a dispense did to stock.</summary>
public enum DispenseStockStatus
{
    /// <summary>The quantity was taken off the branch's lots, first expiry first.</summary>
    Deducted,

    /// <summary>No inventory item has this drug's name. Nothing was taken, nothing was charged.</summary>
    NotMatched,

    /// <summary>The item matched, but the branch has fewer usable units than were handed over. Nothing was taken.</summary>
    Insufficient,
}
