using HappyPaws.Application.Appointments;
using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Scheduling.Dtos;

namespace HappyPaws.Application.Scheduling.RequestTimeOff;

/// <summary>
/// The second half of "ระบบเตือนนัดที่ชนและเสนอหมอแทน": the appointments someone's leave lands on,
/// and for each, who could take it.
/// <para>
/// A suggestion is someone rostered at that branch for the whole appointment, allowed to perform
/// its service, and with nothing else booked or held over it, ordered by the clinic's assignment
/// rules. It is a suggestion only. Nothing here moves an appointment; the owner was told a vet,
/// and changing it is a conversation for the front desk.
/// </para>
/// </summary>
public sealed class SubstituteFinder(
    IBookingCatalog catalog,
    IAppointmentRepository appointments,
    ISlotHoldRepository holds,
    IBranchRepository branches,
    IClock clock)
{
    public async Task<IReadOnlyList<TimeOffConflictDto>> ConflictsAsync(
        string staffId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var rules = await catalog.AssignmentRulesAsync(cancellationToken);
        var conflicts = new List<TimeOffConflictDto>();

        foreach (var branch in await branches.ListActiveAsync(cancellationToken))
        {
            var clashing = await appointments.OccupyingSlotsAsync(branch.Id, [staffId], from, to, null, cancellationToken);

            foreach (var appointment in clashing.Where(appointment => appointment.OccupiesSlot).OrderBy(appointment => appointment.StartsAt))
            {
                var service = await catalog.FindServiceAsync(appointment.ServiceId, cancellationToken);

                var rostered = await catalog.ShiftsAsync(branch.Id, appointment.StartsAt, appointment.EndsAt, null, cancellationToken);
                var candidates = rostered
                    .Where(shift => shift.StaffId != staffId
                        && shift.Covers(appointment.StartsAt, appointment.EndsAt)
                        && (service is null || service.CanBePerformedBy(shift.StaffId)))
                    .Select(shift => shift.StaffId)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                var busy = await appointments.OccupyingSlotsAsync(
                    branch.Id, candidates, appointment.StartsAt, appointment.EndsAt, null, cancellationToken);
                var held = await holds.ActiveInWindowAsync(
                    branch.Id, candidates, appointment.StartsAt, appointment.EndsAt, clock.UtcNow, null, cancellationToken);

                var free = candidates.Where(candidate =>
                    SlotCalculator.IsFree(busy, held, candidate, appointment.StartsAt, appointment.EndsAt));

                conflicts.Add(new TimeOffConflictDto(
                    appointment.Id,
                    branch.Id,
                    appointment.ServiceId,
                    appointment.StartsAt,
                    appointment.EndsAt,
                    rules.Rank(free, appointment.ServiceId, species: null)));
            }
        }

        return conflicts;
    }
}
