using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Crm;

/// <summary>What moved a customer's points.</summary>
public enum LoyaltyEntryKind
{
    /// <summary>Points for a bill that reached <c>paid</c>.</summary>
    Earn,

    /// <summary>Points taken back because money on that bill was refunded.</summary>
    Reverse,

    /// <summary>Points spent as a discount on a bill.</summary>
    Redeem,

    /// <summary>Spent points given back because the bill they were spent on was voided.</summary>
    Restore,
}

/// <summary>
/// One row of the points ledger — "แต้มสะสม ... และประวัติแลก".
/// <para>
/// <b>Append-only.</b> The balance is the sum of the rows; nothing is ever updated. A stored
/// balance would have to be locked by every payment of every customer at once, and a payment must
/// not fail because a loyalty counter was busy. Earning and reversing only insert; spending takes a
/// per-customer lock so two bills cannot spend the same points.
/// </para>
/// </summary>
public sealed class LoyaltyEntry : Entity
{
    private LoyaltyEntry()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    public LoyaltyEntryKind Kind { get; private set; }

    /// <summary>Signed: positive for earn and restore, negative for reverse and redeem.</summary>
    public int Points { get; private set; }

    public string? InvoiceId { get; private set; }

    /// <summary>The discount a redemption became.</summary>
    public string? DiscountId { get; private set; }

    public string? Note { get; private set; }

    public string? StaffId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static LoyaltyEntry Create(
        string customerId,
        LoyaltyEntryKind kind,
        int points,
        DateTimeOffset occurredAt,
        string? invoiceId = null,
        string? discountId = null,
        string? note = null,
        string? staffId = null)
    {
        var signed = kind is LoyaltyEntryKind.Earn or LoyaltyEntryKind.Restore ? Math.Abs(points) : -Math.Abs(points);

        if (signed == 0)
        {
            throw new DomainException("invalid_points", "จำนวนแต้มต้องไม่เป็นศูนย์");
        }

        return new LoyaltyEntry
        {
            Id = EntityId.New(EntityId.LoyaltyEntry),
            CustomerId = customerId,
            Kind = kind,
            Points = signed,
            InvoiceId = invoiceId,
            DiscountId = discountId,
            Note = note,
            StaffId = staffId,
            OccurredAt = occurredAt,
        };
    }
}
