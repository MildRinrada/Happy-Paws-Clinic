using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Appointments;

/// <summary>
/// A booked visit — <c>apt_24810</c>. The aggregate root of the booking domain.
/// <para>
/// Every state change goes through a method here, and every illegal one raises
/// <see cref="StateConflictException"/> so the API answers <c>409 state_conflict</c> as the
/// contract requires. The transitions live in <see cref="AppointmentTransitions"/> rather than
/// being spelled out inline, so the machine can be read — and unit tested — in one place.
/// </para>
/// <para>
/// The one rule worth stating out loud: an appointment holds its slot only while
/// <see cref="OccupiesSlot"/> is true. Cancelling releases the time ("ปล่อย slot คืน") by moving
/// out of that set, not by deleting the row — the clinic still needs to know that the visit was
/// booked and who called it off.
/// </para>
/// </summary>
public sealed class Appointment : Entity
{
    private Appointment()
    {
    }

    public string BranchId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    public string PetId { get; private set; } = string.Empty;

    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>The assigned vet. Null for a walk-in taken before anyone is free.</summary>
    public string? StaffId { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public AppointmentStatus Status { get; private set; }

    /// <summary>What the owner reported — "อาการที่แจ้ง".</summary>
    public string? Reason { get; private set; }

    public bool IsWalkIn { get; private set; }

    /// <summary>Set when a staff member booked on the customer's behalf; null for self-service.</summary>
    public string? BookedByStaffId { get; private set; }

    /// <summary>The hold this booking consumed, kept so a disputed double-booking is traceable.</summary>
    public string? SourceHoldId { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public DateTimeOffset? CheckedInAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>Who cancelled — the contract requires recording "เหตุผลและผู้ยกเลิก".</summary>
    public string? CancelledByUserId { get; private set; }

    public string? CancellationReason { get; private set; }

    /// <summary>
    /// <c>PUT /staff/appointment-types</c> — which of the clinic's appointment types this is
    /// ("นัดปกติ ฉุกเฉิน ติดตามผล ผ่าตัด"). Kept as the code rather than a copy of the type's rules:
    /// the rules apply at booking and check-in, and the code is what reports group by.
    /// </summary>
    public string TypeCode { get; private set; } = AppointmentTypeCatalog.DefaultCode;

    /// <summary>
    /// The series this visit belongs to, when it was booked as one of a course —
    /// <c>POST /staff/appointments/recurring</c>. Null for a one-off.
    /// </summary>
    public string? SeriesId { get; private set; }

    /// <summary>
    /// True when the customer cancelled inside the cancellation policy's cutoff. Counted, with
    /// no-shows, against the number of times the policy forgives.
    /// </summary>
    public bool IsLateCancellation { get; private set; }

    /// <summary>True while this appointment still blocks its time on the vet's calendar.</summary>
    public bool OccupiesSlot => AppointmentTransitions.OccupiesSlot(Status);

    public static Appointment Book(
        string branchId,
        string customerId,
        string petId,
        string serviceId,
        string? staffId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? reason = null,
        bool isWalkIn = false,
        string? bookedByStaffId = null,
        string? sourceHoldId = null,
        string? typeCode = null,
        string? seriesId = null)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainException("invalid_appointment", "เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม");
        }

        return new Appointment
        {
            TypeCode = typeCode ?? AppointmentTypeCatalog.DefaultCode,
            SeriesId = seriesId,
            Id = EntityId.New(EntityId.Appointment),
            BranchId = branchId,
            CustomerId = customerId,
            PetId = petId,
            ServiceId = serviceId,
            StaffId = staffId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            // Bookings enter as `booked`; `draft` is reserved for the multi-step staff flows that
            // the Tier-2 recurring-series endpoint builds.
            Status = AppointmentStatus.Booked,
            Reason = Trimmed(reason),
            IsWalkIn = isWalkIn,
            BookedByStaffId = bookedByStaffId,
            SourceHoldId = sourceHoldId,
        };
    }

    /// <summary><c>POST /appointments/{id}/confirm</c> — from a reminder link or a LINE button.</summary>
    public void Confirm(DateTimeOffset now)
    {
        // Confirming twice is what a customer tapping a reminder twice looks like. It is not an
        // error; the second tap simply changes nothing.
        if (Status == AppointmentStatus.Confirmed)
        {
            return;
        }

        Transition(AppointmentStatus.Confirmed, nameof(Confirm));
        ConfirmedAt = now;
    }

    /// <summary><c>POST /appointments/{id}/check-in</c> — the step that issues a queue ticket.</summary>
    public void CheckIn(DateTimeOffset now)
    {
        Transition(AppointmentStatus.CheckedIn, nameof(CheckIn));
        CheckedInAt = now;
        ConfirmedAt ??= now;
    }

    /// <summary>
    /// The patient went into the consulting room — driven by <c>POST /staff/consultations</c>
    /// rather than by an endpoint of its own, because opening the case <i>is</i> the event.
    /// Calling it twice is harmless: the second vet to press it is looking at the same case.
    /// </summary>
    public void BeginConsultation()
    {
        if (Status == AppointmentStatus.InConsultation)
        {
            return;
        }

        Transition(AppointmentStatus.InConsultation, nameof(BeginConsultation));
    }

    /// <summary>
    /// The visit is finished — driven by <c>POST /staff/consultations/{id}/close</c>, which signs
    /// the record and issues the bill in the same transaction.
    /// </summary>
    public void Complete()
    {
        if (Status == AppointmentStatus.Completed)
        {
            return;
        }

        Transition(AppointmentStatus.Completed, nameof(Complete));
    }

    /// <summary>
    /// <c>POST /appointments/{id}/cancel</c> — records who and why, and frees the slot.
    /// <paramref name="isLate"/> is decided by the caller from the cancellation policy, because
    /// whether a cancellation counts against the customer depends on who made it.
    /// </summary>
    public void Cancel(DateTimeOffset now, string byUserId, string? reason, bool isLate = false)
    {
        if (Status == AppointmentStatus.Cancelled)
        {
            return;
        }

        Transition(AppointmentStatus.Cancelled, nameof(Cancel));
        CancelledAt = now;
        CancelledByUserId = byUserId;
        CancellationReason = Trimmed(reason);
        IsLateCancellation = isLate;
    }

    /// <summary>
    /// <c>POST /staff/appointments/{id}/no-show</c> — "บันทึกไม่มาตามนัด".
    /// <para>
    /// Not before the appointment's start. Until then the owner may still walk in, and a no-show
    /// recorded early is a strike against a customer who did nothing wrong.
    /// </para>
    /// </summary>
    public void MarkNoShow(DateTimeOffset now)
    {
        if (!AppointmentTransitions.IsAllowed(Status, AppointmentStatus.NoShow))
        {
            throw new StateConflictException(nameof(Appointment), Status.ToString(), nameof(MarkNoShow));
        }

        if (now < StartsAt)
        {
            throw new DomainException("no_show_too_early", "บันทึกไม่มาตามนัดได้หลังถึงเวลานัดแล้วเท่านั้น");
        }

        Status = AppointmentStatus.NoShow;
        CancelledAt = now;
    }

    /// <summary>
    /// <c>POST /appointments/{id}/reschedule</c>. Whether the caller is still allowed to move it
    /// is a policy question answered in the Application layer; what the aggregate enforces is that
    /// a visit already under way cannot be moved.
    /// </summary>
    public void Reschedule(DateTimeOffset startsAt, DateTimeOffset endsAt, string? staffId)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainException("invalid_appointment", "เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม");
        }

        RequireOpen(nameof(Reschedule));

        StartsAt = startsAt;
        EndsAt = endsAt;
        StaffId = staffId ?? StaffId;

        // A moved appointment is no longer the one the customer confirmed, so the confirmation
        // does not carry over — otherwise a reminder-driven confirm silently endorses a new time.
        Status = AppointmentStatus.Booked;
        ConfirmedAt = null;
    }

    /// <summary><c>PATCH /staff/appointments/{id}</c> — "เปลี่ยนหมอ บริการ หรืออาการที่แจ้ง".</summary>
    public void ChangeDetails(string? serviceId, string? staffId, DateTimeOffset? endsAt, string? reason)
    {
        RequireOpen(nameof(ChangeDetails));

        if (serviceId is { Length: > 0 })
        {
            ServiceId = serviceId;
        }

        if (staffId is { Length: > 0 })
        {
            StaffId = staffId;
        }

        // A different service usually means a different duration, so the end moves with it.
        if (endsAt is { } end)
        {
            if (end <= StartsAt)
            {
                throw new DomainException("invalid_appointment", "เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม");
            }

            EndsAt = end;
        }

        if (reason is not null)
        {
            Reason = Trimmed(reason);
        }
    }

    private void RequireOpen(string attempted)
    {
        if (!OccupiesSlot || Status == AppointmentStatus.InConsultation)
        {
            throw new StateConflictException(nameof(Appointment), Status.ToString(), attempted);
        }
    }

    private void Transition(AppointmentStatus next, string attempted)
    {
        if (!AppointmentTransitions.IsAllowed(Status, next))
        {
            throw new StateConflictException(nameof(Appointment), Status.ToString(), attempted);
        }

        Status = next;
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
