using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Clinics;

/// <summary>
/// The clinic-wide booking rules read by <c>GET /clinic/settings/booking</c> and written by
/// <c>PATCH /clinic/settings/booking</c> — "ช่วงเวลาจอง lead time โควตา walk-in" and
/// "slot 15/20/30 นาที · ยกเลิกล่วงหน้ากี่ชั่วโมง".
/// <para>
/// One row per instance, because one deployment is one clinic. The fixed <see cref="SingletonId"/>
/// is what makes that structural rather than a convention someone can forget: a second row cannot
/// be inserted without colliding on the primary key.
/// </para>
/// </summary>
public sealed class BookingSettings : Entity
{
    public const string SingletonId = "set_booking";

    private BookingSettings()
    {
    }

    /// <summary>Grid the booking page offers slots on. The contract names 15, 20 and 30.</summary>
    public int SlotMinutes { get; private set; } = 30;

    /// <summary>
    /// How far ahead of now a customer may book. Staff bypass this — the contract is explicit that
    /// <c>POST /staff/appointments</c> "ข้าม lead time ได้".
    /// </summary>
    public int LeadTimeMinutes { get; private set; } = 120;

    /// <summary>Hours before the appointment after which a customer may no longer reschedule it.</summary>
    public int CancellationCutoffHours { get; private set; } = 12;

    /// <summary>How far into the future the booking page shows slots at all.</summary>
    public int MaxAdvanceDays { get; private set; } = 60;

    /// <summary>Walk-in capacity reserved per hour per vet, withheld from online booking.</summary>
    public int WalkInSlotsPerHour { get; private set; }

    public static BookingSettings CreateDefault() => new() { Id = SingletonId };

    /// <summary>
    /// Applies a partial update; a null argument leaves that setting alone, which is what
    /// <c>PATCH</c> means.
    /// </summary>
    public void Update(
        int? slotMinutes = null,
        int? leadTimeMinutes = null,
        int? cancellationCutoffHours = null,
        int? maxAdvanceDays = null,
        int? walkInSlotsPerHour = null)
    {
        if (slotMinutes is { } slot)
        {
            // A grid that is not a whole number of minutes, or coarser than two hours, produces a
            // booking page nobody can use. The contract's own examples are 15/20/30.
            if (slot is < 5 or > 120 || slot % 5 != 0)
            {
                throw new DomainException("invalid_booking_settings", "ความยาว slot ต้องเป็นจำนวนเท่าของ 5 นาที ระหว่าง 5–120 นาที");
            }

            SlotMinutes = slot;
        }

        if (leadTimeMinutes is { } lead)
        {
            LeadTimeMinutes = NonNegative(lead, "lead time");
        }

        if (cancellationCutoffHours is { } cutoff)
        {
            CancellationCutoffHours = NonNegative(cutoff, "cutoff การยกเลิก");
        }

        if (maxAdvanceDays is { } advance)
        {
            if (advance is < 1 or > 365)
            {
                throw new DomainException("invalid_booking_settings", "ช่วงเวลาจองล่วงหน้าต้องอยู่ระหว่าง 1–365 วัน");
            }

            MaxAdvanceDays = advance;
        }

        if (walkInSlotsPerHour is { } walkIn)
        {
            WalkInSlotsPerHour = NonNegative(walkIn, "โควตา walk-in");
        }
    }

    private static int NonNegative(int value, string label) =>
        value >= 0
            ? value
            : throw new DomainException("invalid_booking_settings", $"{label} ต้องไม่ติดลบ");
}
