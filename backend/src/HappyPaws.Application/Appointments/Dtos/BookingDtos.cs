using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Queueing.Dtos;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments.Dtos;

/// <summary>
/// One opening in the <c>GET /booking/availability</c> response.
/// <para>
/// The vet is named on every slot rather than left implicit, because a customer who picks 11:00
/// without knowing which vet is on it cannot be given the same person twice — and continuity of
/// care is the thing owners actually ask for.
/// </para>
/// </summary>
public sealed record AvailableSlotDto(DateTimeOffset StartsAt, DateTimeOffset EndsAt, string StaffId)
{
    public static AvailableSlotDto From(AvailableSlot slot) =>
        new(slot.StartsAt, slot.EndsAt, slot.StaffId);
}

/// <summary>
/// The <c>GET /booking/availability</c> payload. <c>Date</c> is the clinic-local day the slots
/// belong to, echoed back so a client can key its cache on the day it actually asked for.
/// </summary>
public sealed record AvailabilityDto(
    string BranchId,
    string ServiceId,
    DateOnly Date,
    int DurationMinutes,
    IReadOnlyList<AvailableSlotDto> Slots);

/// <summary>The <c>POST /booking/holds</c> payload — a claim on a slot and when it lapses.</summary>
public sealed record SlotHoldDto(
    string Id,
    string BranchId,
    string ServiceId,
    string StaffId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset ExpiresAt)
{
    public static SlotHoldDto From(SlotHold hold) =>
        new(hold.Id, hold.BranchId, hold.ServiceId, hold.StaffId, hold.StartsAt, hold.EndsAt, hold.ExpiresAt);
}

/// <summary>
/// What <c>POST /appointments/{id}/check-in</c> returns: the appointment and the queue ticket the
/// check-in issued, so the counter can print or display the number without a second round-trip.
/// </summary>
public sealed record CheckInResultDto(AppointmentDto Appointment, QueueTicketDto Ticket);
