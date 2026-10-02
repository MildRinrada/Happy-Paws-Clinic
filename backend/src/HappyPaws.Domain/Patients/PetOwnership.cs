using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Patients;

/// <summary>
/// The link that decides what a portal account may reach — created by
/// <c>POST /staff/pets/{id}/owners</c> and withdrawn by
/// <c>DELETE /staff/pets/{id}/owners/{cid}</c>.
/// <para>
/// Two details in the contract shape this type. First, withdrawal is
/// "ถอนสิทธิ์เข้าถึง ไม่ลบประวัติ" — revoke access, keep the history — so the row is closed with
/// <see cref="RevokedAt"/> rather than deleted; who could see what, and until when, stays
/// answerable. Second, adding a co-owner "กำหนดสิทธิ์ดูประวัติ" sets history access separately, so
/// a relative can book visits without reading the medical record.
/// </para>
/// </summary>
public sealed class PetOwnership : Entity
{
    private PetOwnership()
    {
    }

    public string PetId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>True for the person the record was opened under; co-owners are added afterwards.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Whether this carer may read the pet's clinical history, not just book for it.</summary>
    public bool MayViewHistory { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static PetOwnership Create(
        string petId,
        string customerId,
        bool isPrimary = false,
        bool mayViewHistory = true) =>
        new()
        {
            Id = EntityId.New(EntityId.Ownership),
            PetId = petId,
            CustomerId = customerId,
            IsPrimary = isPrimary,
            MayViewHistory = mayViewHistory,
        };

    public void Revoke(DateTimeOffset at)
    {
        if (IsPrimary)
        {
            // Removing the last carer would orphan the record and, with it, every notification
            // and invoice that hangs off the owner. Transfer first.
            throw new DomainException("primary_owner_required", "ถอนสิทธิ์เจ้าของหลักไม่ได้ ต้องโอนเจ้าของหลักก่อน");
        }

        RevokedAt ??= at;
    }

    public void SetHistoryAccess(bool mayViewHistory) => MayViewHistory = mayViewHistory;

    /// <summary>
    /// Folds a duplicate link into this one during a customer merge.
    /// <para>
    /// When two customer records turn out to be one person and both hold a live link to the same
    /// patient, the surviving link takes the <b>wider</b> of the two on each flag. Taking the
    /// narrower would quietly remove access the person already had, which is the one outcome a merge
    /// must never produce — a merge tidies records, it does not revoke anything.
    /// </para>
    /// </summary>
    public void Absorb(PetOwnership duplicate)
    {
        IsPrimary = IsPrimary || duplicate.IsPrimary;
        MayViewHistory = MayViewHistory || duplicate.MayViewHistory;
    }

    /// <summary>
    /// Moves this link to the surviving customer record during a merge.
    /// <para>
    /// Only called for a patient the survivor does <b>not</b> already have a live link to —
    /// otherwise there is nothing to move and <see cref="Absorb"/> folds the two instead. That split
    /// is what keeps the merge from violating the unique index on <c>(pet_id, customer_id)</c>.
    /// </para>
    /// </summary>
    public void TransferTo(string customerId) => CustomerId = customerId;
}
