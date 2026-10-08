using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Customers;

/// <summary>
/// A pet owner as a clinic record — <c>cus_1180</c>.
/// <para>
/// Deliberately separate from <c>UserAccount</c>. The counter creates customers who have never
/// signed in ("สร้างจากเคาน์เตอร์"), and an invitation later attaches a portal account
/// (<c>POST /staff/customers/{id}/invite</c>). Folding the two together would make a login a
/// precondition for being treated, which is not how a clinic works.
/// </para>
/// <para>
/// Owned by the Customer module. Defined here because an appointment is always for a customer;
/// the module's own endpoints land with that module.
/// </para>
/// </summary>
public sealed class Customer : Entity
{
    private Customer()
    {
    }

    /// <summary>The portal account, once the customer has accepted an invitation. Null until then.</summary>
    public string? UserId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Primary contact and the clinic's duplicate check ("ตรวจซ้ำด้วยเบอร์โทร").</summary>
    public string Phone { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Where LINE messages go — the user id LINE gives this clinic's Official Account for this
    /// person. <b>Delivery information, like <see cref="Email"/>.</b> It never signs anyone in,
    /// never links an account, and is not a key: see <c>docs/assumptions.md § A</c>.
    /// </summary>
    public string? LineUserId { get; private set; }

    /// <summary>Set when the person blocks or unfollows the account; LINE then refuses pushes to them.</summary>
    public DateTimeOffset? LineUnfollowedAt { get; private set; }

    /// <summary>True when a LINE push can reach this person.</summary>
    public bool CanReceiveLine => LineUserId is not null && LineUnfollowedAt is null;

    /// <summary>
    /// Set on the record that was absorbed by <c>POST /staff/customers/{id}/merge</c>.
    /// <para>
    /// The duplicate is kept as a tombstone rather than deleted, so an id printed on an old receipt
    /// or bookmarked in someone's browser still resolves — and resolves to an answer that says where
    /// the record went, instead of a 404 that says it never existed. See
    /// <c>docs/assumptions.md § 12</c>.
    /// </para>
    /// </summary>
    public string? MergedIntoCustomerId { get; private set; }

    public bool IsMerged => MergedIntoCustomerId is not null;

    public static Customer Create(string displayName, string phone, string? email = null, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("invalid_customer", "ต้องระบุชื่อลูกค้า");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("invalid_customer", "ต้องระบุเบอร์โทรศัพท์");
        }

        return new Customer
        {
            Id = EntityId.New(EntityId.Customer),
            DisplayName = displayName.Trim(),
            Phone = phone.Trim(),
            Email = email?.Trim(),
            UserId = userId,
        };
    }

    /// <summary>Attaches the portal account created when the customer accepts their invitation.</summary>
    public void LinkAccount(string userId) => UserId = userId;

    /// <summary>
    /// Records the LINE address to send to, or clears it with null. Nothing in the contract sets it
    /// yet (<c>docs/features/line.md</c>); the method exists so the rule lives on the record.
    /// </summary>
    public void SetLineAddress(string? lineUserId)
    {
        var value = string.IsNullOrWhiteSpace(lineUserId) ? null : lineUserId.Trim();

        if (value is { Length: > 64 })
        {
            throw new DomainException("invalid_customer", "LINE user id ยาวเกินไป");
        }

        LineUserId = value;
        LineUnfollowedAt = null;
    }

    /// <summary>The person blocked the account. Pushes stop until they follow it again.</summary>
    public void MarkLineUnfollowed(DateTimeOffset at) => LineUnfollowedAt ??= at;

    /// <summary>The person followed the account again.</summary>
    public void MarkLineFollowed() => LineUnfollowedAt = null;

    /// <summary>
    /// Marks this record as the duplicate absorbed into <paramref name="survivingCustomerId"/>.
    /// <para>
    /// A merged record is inactive: it must not appear in search, must not be chosen at the counter,
    /// and must not be merged a second time. What it keeps is its identity and a pointer, because
    /// the rows that moved still refer to it in every audit entry written before today.
    /// </para>
    /// </summary>
    public void MergeInto(string survivingCustomerId)
    {
        if (IsMerged)
        {
            throw new DomainException("already_merged", "เรกคอร์ดนี้ถูกรวมไปแล้ว");
        }

        if (survivingCustomerId == Id)
        {
            throw new DomainException("invalid_merge", "รวมเรกคอร์ดเข้ากับตัวเองไม่ได้");
        }

        MergedIntoCustomerId = survivingCustomerId;
        IsActive = false;
    }

    /// <summary>
    /// Takes over the duplicate's portal account when the surviving record has none.
    /// <para>
    /// Only ever called in that direction. Two records that <b>both</b> carry an account are two
    /// real logins for one person, and choosing which one to discard is not a decision an API
    /// should make silently — the merge is refused instead.
    /// </para>
    /// </summary>
    public void AdoptAccountFrom(Customer duplicate)
    {
        if (UserId is null && duplicate.UserId is not null)
        {
            UserId = duplicate.UserId;
            duplicate.UserId = null;
        }
    }
}
