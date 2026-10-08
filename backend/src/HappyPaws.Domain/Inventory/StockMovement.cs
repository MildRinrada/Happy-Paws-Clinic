using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Inventory;

/// <summary>Why stock moved — one value per kind of ledger row.</summary>
public enum StockMovementKind
{
    /// <summary>"รับเข้า" — goods arrived from a supplier, against a purchase order or not.</summary>
    Receive,

    /// <summary>"จ่ายออก" — taken off the shelf by hand: used in a procedure, broken, thrown away.</summary>
    Issue,

    /// <summary>"โอนสาขา", the sending side.</summary>
    TransferOut,

    /// <summary>"โอนสาขา", the receiving side. Paired with its <see cref="TransferOut"/> by transfer id.</summary>
    TransferIn,

    /// <summary>"ปรับยอดหลังนับสต๊อก" — the difference between the books and a physical count.</summary>
    Adjustment,

    /// <summary>Handed to an owner by <c>POST /staff/prescriptions/{id}/dispense</c>.</summary>
    Dispense,
}

/// <summary>
/// One row of the stock ledger: this many units of this lot, in or out, and why.
/// <para>
/// <b>Append-only.</b> Nothing updates or deletes a movement; a mistake is corrected by a further
/// movement (an adjustment, an issue), which leaves both the error and its correction on the record.
/// The lot's balance is the running sum of these rows; the factories here are the only way a
/// balance changes, so the two cannot drift.
/// </para>
/// </summary>
public sealed class StockMovement : Entity
{
    public const int ReferenceMax = 120;
    public const int NoteMax = 500;

    private StockMovement()
    {
    }

    public string ItemId { get; private set; } = string.Empty;

    public string BranchId { get; private set; } = string.Empty;

    public string LotId { get; private set; } = string.Empty;

    public StockMovementKind Kind { get; private set; }

    /// <summary>Signed: positive in, negative out. Zero only for a count that matched the books.</summary>
    public int Quantity { get; private set; }

    /// <summary>The lot's balance after this row — the ledger can be read without summing it.</summary>
    public int BalanceAfter { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string RecordedByStaffId { get; private set; } = string.Empty;

    /// <summary>"อ้างอิงเอกสารต้นทาง" — the delivery note, invoice or form this movement came from.</summary>
    public string? Reference { get; private set; }

    /// <summary>Free text: why it was issued, what the count found. Required on an adjustment.</summary>
    public string? Note { get; private set; }

    /// <summary>What one unit cost the clinic, on a receipt. The beginning of a real margin.</summary>
    public long? UnitCostSatang { get; private set; }

    public string? PurchaseOrderId { get; private set; }

    public string? PrescriptionId { get; private set; }

    /// <summary>Pairs the two sides of a transfer.</summary>
    public string? TransferId { get; private set; }

    public static StockMovement Receive(
        StockLot lot,
        int quantity,
        string staffId,
        DateTimeOffset at,
        long? unitCostSatang = null,
        string? purchaseOrderId = null,
        string? reference = null,
        string? note = null)
    {
        if (unitCostSatang is < 0)
        {
            throw new DomainException("invalid_movement", "ต้นทุนต่อหน่วยติดลบไม่ได้");
        }

        lot.Add(quantity);

        var movement = Create(lot, StockMovementKind.Receive, quantity, staffId, at, reference, note);
        movement.UnitCostSatang = unitCostSatang;
        movement.PurchaseOrderId = purchaseOrderId;

        return movement;
    }

    public static StockMovement Issue(
        StockLot lot,
        int quantity,
        string staffId,
        DateTimeOffset at,
        string? reference = null,
        string? note = null)
    {
        lot.Take(quantity);
        return Create(lot, StockMovementKind.Issue, -quantity, staffId, at, reference, note);
    }

    public static StockMovement Dispense(StockLot lot, int quantity, string prescriptionId, string staffId, DateTimeOffset at)
    {
        lot.Take(quantity);

        var movement = Create(lot, StockMovementKind.Dispense, -quantity, staffId, at, reference: prescriptionId, note: null);
        movement.PrescriptionId = prescriptionId;

        return movement;
    }

    /// <summary>One side of a transfer. Out takes from <paramref name="lot"/>; in adds to it.</summary>
    public static StockMovement Transfer(
        StockLot lot,
        int quantity,
        bool outgoing,
        string transferId,
        string staffId,
        DateTimeOffset at,
        string? reference = null,
        string? note = null)
    {
        if (outgoing)
        {
            lot.Take(quantity);
        }
        else
        {
            lot.Add(quantity);
        }

        var movement = Create(
            lot,
            outgoing ? StockMovementKind.TransferOut : StockMovementKind.TransferIn,
            outgoing ? -quantity : quantity,
            staffId,
            at,
            reference,
            note);
        movement.TransferId = transferId;

        return movement;
    }

    /// <summary>"ปรับยอดหลังนับสต๊อก · ต้องระบุเหตุผล" — sets the lot to what was counted.</summary>
    public static StockMovement Adjust(StockLot lot, int counted, string reason, string staffId, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("invalid_adjustment", "ต้องระบุเหตุผลในการปรับยอด");
        }

        var delta = lot.SetCounted(counted);
        return Create(lot, StockMovementKind.Adjustment, delta, staffId, at, reference: null, note: reason);
    }

    private static StockMovement Create(
        StockLot lot,
        StockMovementKind kind,
        int quantity,
        string staffId,
        DateTimeOffset at,
        string? reference,
        string? note)
    {
        var cleanReference = Clean(reference);
        var cleanNote = Clean(note);

        if (cleanReference is { Length: > ReferenceMax } || cleanNote is { Length: > NoteMax })
        {
            throw new DomainException("invalid_movement", "เลขอ้างอิงหรือหมายเหตุยาวเกินไป");
        }

        return new StockMovement
        {
            Id = EntityId.New(EntityId.StockMovement),
            ItemId = lot.ItemId,
            BranchId = lot.BranchId,
            LotId = lot.Id,
            Kind = kind,
            Quantity = quantity,
            BalanceAfter = lot.QuantityOnHand,
            OccurredAt = at,
            RecordedByStaffId = staffId,
            Reference = cleanReference,
            Note = cleanNote,
        };
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
