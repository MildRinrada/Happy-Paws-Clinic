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

    /// <summary>
    /// Points given or taken by hand, with a reason (<c>docs/contract-extensions.md § 25</c>). Signed
    /// as entered. Moves the balance, never the lifetime points the level is judged on.
    /// </summary>
    Adjust,

    /// <summary>
    /// Points for a referral (<c>docs/contract-extensions.md § 39</c>): to the person whose code a
    /// new customer used, and to that customer, when the newcomer's first bill was paid. Always
    /// positive. Moves the balance, never the lifetime points the level is judged on, like
    /// <see cref="Adjust"/>: a level reflects money the customer paid.
    /// </summary>
    Referral,
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

    /// <summary>Signed: positive for earn and restore, negative for reverse and redeem, either for adjust.</summary>
    public int Points { get; private set; }

    public string? InvoiceId { get; private set; }

    /// <summary>The discount a redemption became.</summary>
    public string? DiscountId { get; private set; }

    public string? Note { get; private set; }

    public string? StaffId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// When the points this row added stop counting, or null for never. Set when the row is written,
    /// from the clinic's loyalty settings at that moment, so a later change to the settings never
    /// reaches back to points already given. Only rows that add points carry one.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// On an earn row: how many satang bought one point when it was earned. A refund takes points
    /// back at this rate, not today's. Null on rows written before the rate became a setting, which
    /// were all earned at <see cref="LoyaltyRules.SatangPerPoint"/>.
    /// </summary>
    public long? SatangPerPoint { get; private set; }

    /// <summary>
    /// On a <see cref="LoyaltyEntryKind.Referral"/> row: the referral it pays. With the customer, unique
    /// among referral rows, which is what makes a referral pay each person once.
    /// </summary>
    public string? ReferralId { get; private set; }

    public static LoyaltyEntry Create(
        string customerId,
        LoyaltyEntryKind kind,
        int points,
        DateTimeOffset occurredAt,
        string? invoiceId = null,
        string? discountId = null,
        string? note = null,
        string? staffId = null,
        DateTimeOffset? expiresAt = null,
        long? satangPerPoint = null,
        string? referralId = null)
    {
        var signed = kind switch
        {
            LoyaltyEntryKind.Earn or LoyaltyEntryKind.Restore or LoyaltyEntryKind.Referral => Math.Abs(points),
            LoyaltyEntryKind.Adjust => points,
            _ => -Math.Abs(points),
        };

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
            ExpiresAt = signed > 0 ? expiresAt : null,
            SatangPerPoint = kind == LoyaltyEntryKind.Earn ? satangPerPoint : null,
            ReferralId = kind == LoyaltyEntryKind.Referral ? referralId : null,
        };
    }
}
