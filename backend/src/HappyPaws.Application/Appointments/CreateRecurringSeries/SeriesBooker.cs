using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Appointments.CreateRecurringSeries;

/// <summary>What a course of visits needs to be booked.</summary>
public sealed record SeriesRequest(
    Pet Pet,
    string ServiceId,
    IReadOnlyList<DateTimeOffset> Starts,
    AppointmentType Type,
    string BookedByStaffId,
    string? StaffId = null,
    string? BranchId = null,
    string? Reason = null);

/// <summary>
/// Books a course of visits: every one of them, or none.
/// <para>
/// Shared by <c>POST /staff/appointments/recurring</c> and <c>POST /staff/pets/{id}/treatment-plans</c>,
/// which are the same act — a set of future visits for one patient — reached from two places.
/// </para>
/// <para>
/// <b>One vet for the whole course</b> unless the caller names one: whoever is picked for the first
/// visit is asked for every later one. A chemotherapy course that changes hands each session is not
/// what anyone booking one wants, so a later date that vet cannot take is a conflict to resolve,
/// not a silent swap.
/// </para>
/// <para>
/// <b>All or nothing.</b> Every visit is checked before any is written. If some cannot be booked the
/// answer is <c>422 slot_unavailable</c> with one entry per failed date in
/// <c>details.conflicts</c>, each with the reason and the openings that day. Half a course in the
/// calendar is worse than none, because nobody notices the missing half until the day it was due.
/// </para>
/// </summary>
public sealed class SeriesBooker(
    BookingSlotResolver resolver,
    IAppointmentRepository appointments,
    BranchGuard branchGuard)
{
    /// <summary>A year of weekly visits. A course longer than that is planned in stages.</summary>
    public const int MaxVisits = 52;

    /// <summary>Must be called inside the caller's transaction; it adds but does not save.</summary>
    public async Task<(string SeriesId, IReadOnlyList<Appointment> Visits)> BookAsync(
        SeriesRequest request,
        CancellationToken cancellationToken)
    {
        var seriesId = EntityId.New(EntityId.Series);
        var staffId = request.StaffId;
        var resolved = new List<ResolvedSlot>();
        var conflicts = new List<IReadOnlyDictionary<string, object>>();

        foreach (var start in request.Starts)
        {
            try
            {
                var slot = await resolver.ResolveAsync(
                    new SlotRequest(
                        request.ServiceId,
                        staffId,
                        request.BranchId,
                        start,
                        BypassLeadTime: true,
                        RequireOnlineBookable: false,
                        Species: request.Pet.Species,
                        DurationMinutes: request.Type.DurationMinutes,
                        BypassAdvanceLimit: true),
                    cancellationToken);

                branchGuard.EnsureCanAccess(slot.Branch.Id);

                staffId ??= slot.StaffId;
                resolved.Add(slot);
            }
            catch (SlotUnavailableException unavailable)
            {
                conflicts.Add(new Dictionary<string, object>
                {
                    ["starts_at"] = start,
                    ["reason"] = unavailable.Message,
                    ["suggested"] = unavailable.Suggested,
                });
            }
        }

        if (conflicts.Count > 0)
        {
            throw SlotUnavailableException.ForSeries(conflicts);
        }

        var visits = resolved
            .Select(slot => Appointment.Book(
                slot.Branch.Id,
                request.Pet.PrimaryCustomerId,
                request.Pet.Id,
                slot.Service.Id,
                slot.StaffId,
                slot.StartsAt,
                slot.EndsAt,
                request.Reason,
                bookedByStaffId: request.BookedByStaffId,
                typeCode: request.Type.Code,
                seriesId: seriesId))
            .ToList();

        foreach (var visit in visits)
        {
            appointments.Add(visit);
        }

        return (seriesId, visits);
    }
}
