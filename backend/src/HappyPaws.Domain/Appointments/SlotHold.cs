using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Appointments;

/// <summary>
/// A slot reserved while the customer finishes the booking form —
/// <c>POST /booking/holds</c>, "จองค้างไว้ 10 นาทีระหว่างกรอกข้อมูล".
/// <para>
/// Without this, two people filling in the same form both pass the availability check and the
/// second one loses their work at submit. The hold makes the slot disappear from everyone else's
/// availability for ten minutes, which is short enough that an abandoned form does not cost the
/// clinic an afternoon.
/// </para>
/// <para>
/// Expiry is a timestamp, never a background job: a hold is dead the moment
/// <see cref="ExpiresAt"/> passes, whether or not anything has swept the table. A sweeper that
/// falls over must not be able to freeze the calendar.
/// </para>
/// </summary>
public sealed class SlotHold : Entity
{
    /// <summary>Ten minutes, per the contract.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private SlotHold()
    {
    }

    public string BranchId { get; private set; } = string.Empty;

    public string ServiceId { get; private set; } = string.Empty;

    public string StaffId { get; private set; } = string.Empty;

    /// <summary>Who is holding it, so one account cannot consume another's hold.</summary>
    public string HeldByCustomerId { get; private set; } = string.Empty;

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Set when the hold turned into an appointment. A hold is single-use.</summary>
    public DateTimeOffset? ConsumedAt { get; private set; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public static SlotHold Create(
        string branchId,
        string serviceId,
        string staffId,
        string heldByCustomerId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset now) =>
        new()
        {
            Id = EntityId.New(EntityId.Hold),
            BranchId = branchId,
            ServiceId = serviceId,
            StaffId = staffId,
            HeldByCustomerId = heldByCustomerId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            ExpiresAt = now.Add(Lifetime),
        };

    /// <summary>
    /// A hold for a visitor who has not signed in yet — <c>POST /booking/guest/start</c>
    /// (<c>docs/contract-extensions.md § 27</c>, not in the API reference).
    /// <para>
    /// There is no customer to hold it for, so the holder is a marker derived from the phone the
    /// visitor gave (<see cref="GuestHolderFor"/>). Only that number, once proved by its code, can
    /// claim the hold; no customer id ever equals the marker, so nobody can redeem it directly.
    /// </para>
    /// </summary>
    public static SlotHold CreateForGuest(
        string branchId,
        string serviceId,
        string staffId,
        string phoneDigits,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset now) =>
        Create(branchId, serviceId, staffId, GuestHolderFor(phoneDigits), startsAt, endsAt, now);

    /// <summary>
    /// The holder written on a guest hold: <c>gst_</c> and a hash of the number, so the column never
    /// holds a phone number in the clear and still fits its 40 characters.
    /// </summary>
    public static string GuestHolderFor(string phoneDigits) =>
        GuestHolderPrefix + Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"guest-hold:{phoneDigits}")))[..32];

    /// <summary>Starts every guest holder. Customer ids start <c>cus_</c>, so the two never meet.</summary>
    public const string GuestHolderPrefix = "gst_";

    /// <summary>
    /// Hands a guest hold to the customer the visitor turned out to be, once their number is proved.
    /// The hold keeps its expiry: proving a number does not buy more time on the vet's calendar.
    /// </summary>
    public void ClaimForCustomer(string phoneDigits, string customerId, DateTimeOffset now)
    {
        if (HeldByCustomerId != GuestHolderFor(phoneDigits))
        {
            throw new DomainException("hold_not_yours", "การจองค้างนี้เป็นของผู้ใช้อื่น");
        }

        if (!IsHoldingAt(now))
        {
            throw new DomainException("hold_expired", "การจองค้างหมดอายุแล้ว กรุณาเลือกเวลาใหม่");
        }

        HeldByCustomerId = customerId;
    }

    /// <summary>True while this hold still blocks the slot for everyone else.</summary>
    public bool IsHoldingAt(DateTimeOffset now) =>
        ConsumedAt is null && ReleasedAt is null && now < ExpiresAt;

    /// <summary>
    /// Marks the hold spent. Refuses a hold that belongs to someone else, has already been used,
    /// or has run out — each of which would otherwise let a booking through on an expired claim.
    /// </summary>
    public void Consume(string byCustomerId, DateTimeOffset now)
    {
        if (HeldByCustomerId != byCustomerId)
        {
            throw new DomainException("hold_not_yours", "การจองค้างนี้เป็นของผู้ใช้อื่น");
        }

        if (!IsHoldingAt(now))
        {
            throw new DomainException("hold_expired", "การจองค้างหมดอายุแล้ว กรุณาเลือกเวลาใหม่");
        }

        ConsumedAt = now;
    }

    /// <summary>Gives the slot back early — the customer navigated away or picked another time.</summary>
    public void Release(DateTimeOffset now)
    {
        if (ConsumedAt is null)
        {
            ReleasedAt ??= now;
        }
    }
}
