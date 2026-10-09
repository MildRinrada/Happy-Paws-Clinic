using HappyPaws.Application.Common;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.Application.Appointments.Dtos;

/// <summary>One entry on the waiting list.</summary>
public sealed record WaitingListEntryDto(
    string Id,
    string CustomerId,
    string PetId,
    string ServiceId,
    string? BranchId,
    string? StaffId,
    DateOnly EarliestOn,
    DateOnly LatestOn,
    string? Note,
    DateTimeOffset AddedAt,
    DateTimeOffset? LastNotifiedAt,
    int NotifiedCount)
{
    public static WaitingListEntryDto From(WaitingListEntry entry) =>
        new(
            entry.Id,
            entry.CustomerId,
            entry.PetId,
            entry.ServiceId,
            entry.BranchId,
            entry.StaffId,
            entry.EarliestOn,
            entry.LatestOn,
            entry.Note,
            entry.CreatedAt,
            entry.LastNotifiedAt,
            entry.NotifiedCount);
}

/// <summary>
/// One row of <c>GET /staff/waiting-list</c> (<c>docs/contract-extensions.md § 23</c>, not in the
/// API reference): the entry, whether it is still waiting, and the two names a person reads.
/// <para>
/// <c>status</c> is worked out, not stored: <c>waiting</c> until <c>latest_on</c> has passed,
/// <c>lapsed</c> after. The entry itself has no status column (see <see cref="WaitingListEntry"/>).
/// </para>
/// </summary>
public sealed record WaitingListItemDto(
    string Id,
    string Status,
    string CustomerId,
    string PetId,
    string PetName,
    string ServiceId,
    string ServiceName,
    string? BranchId,
    string? StaffId,
    DateOnly EarliestOn,
    DateOnly LatestOn,
    string? Note,
    DateTimeOffset AddedAt,
    DateTimeOffset? LastNotifiedAt,
    int NotifiedCount)
{
    public const string Waiting = "waiting";
    public const string Lapsed = "lapsed";

    public static WaitingListItemDto From(WaitingListEntry entry, string petName, string serviceName, DateOnly today) =>
        new(
            entry.Id,
            entry.IsLapsed(today) ? Lapsed : Waiting,
            entry.CustomerId,
            entry.PetId,
            petName,
            entry.ServiceId,
            serviceName,
            entry.BranchId,
            entry.StaffId,
            entry.EarliestOn,
            entry.LatestOn,
            entry.Note,
            entry.CreatedAt,
            entry.LastNotifiedAt,
            entry.NotifiedCount);
}

/// <summary>
/// One type a customer may book themselves — <c>GET /portal/appointment-types</c>
/// (<c>docs/contract-extensions.md § 23</c>). Only the code to send back, the name to show, and the
/// length; the counter's rules (lead time, queue priority) are not the customer's to read.
/// </summary>
public sealed record OnlineAppointmentTypeDto(string Code, string Name, int? DurationMinutes)
{
    public static OnlineAppointmentTypeDto From(AppointmentType type) => new(type.Code, type.Name, type.DurationMinutes);
}

/// <summary>One appointment type and its rules.</summary>
public sealed record AppointmentTypeDto(
    string Code,
    string Name,
    int? DurationMinutes,
    bool OnlineBooking,
    int? LeadTimeMinutes,
    string QueuePriority)
{
    public static AppointmentTypeDto From(AppointmentType type) =>
        new(type.Code, type.Name, type.DurationMinutes, type.OnlineBooking, type.LeadTimeMinutes, WireEnum.Of(type.QueuePriority));
}

/// <summary>
/// <c>GET</c> and <c>PUT /staff/appointment-types</c> answer with the whole set and its version.
/// </summary>
public sealed record AppointmentTypesDto(uint Version, IReadOnlyList<AppointmentTypeDto> Types)
{
    public static AppointmentTypesDto From(AppointmentTypeCatalog catalog) =>
        new(catalog.Version, [.. catalog.Types.Select(AppointmentTypeDto.From)]);
}

/// <summary><c>GET</c> and <c>PUT /staff/policies/cancellation</c> — the policy as stored.</summary>
public sealed record CancellationPolicyDto(
    uint Version,
    int CutoffHours,
    long FeeSatang,
    int FreeAllowance,
    int WindowDays)
{
    public static CancellationPolicyDto From(CancellationPolicy policy) =>
        new(policy.Version, policy.CutoffHours, policy.FeeSatang, policy.FreeAllowance, policy.WindowDays);
}

/// <summary>
/// "นับสถิติรายลูกค้า" — a customer's record of missed and late-cancelled visits.
/// <para>
/// <c>strikes_in_window</c> counts both, within the policy's window; <c>fee_due_satang</c> is what
/// the latest strike carries under the policy, zero while the allowance lasts. Reported, not
/// charged: the counter bills it, if it does, with the customer present.
/// </para>
/// </summary>
public sealed record CustomerAttendanceDto(
    string CustomerId,
    int NoShows,
    int LateCancellations,
    int StrikesInWindow,
    int WindowDays,
    int FreeAllowance,
    long FeeDueSatang);

/// <summary><c>POST /staff/appointments/{id}/no-show</c> — the appointment, and where the customer now stands.</summary>
public sealed record NoShowResultDto(AppointmentDto Appointment, CustomerAttendanceDto Attendance);

/// <summary>
/// One problem with a time — "นัดชน หมอลา ห้องไม่ว่าง". <c>kind</c> is one of
/// <c>clinic_closed</c>, <c>staff_off_shift</c>, <c>staff_on_time_off</c>,
/// <c>staff_double_booked</c>, <c>pet_double_booked</c>.
/// </summary>
public sealed record AppointmentConflictDto(
    string Kind,
    string Message,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? AppointmentId = null,
    string? StaffId = null,
    string? ConflictingAppointmentId = null);

/// <summary><c>GET /staff/appointments/conflicts</c>. <c>clear</c> is true when nothing was found.</summary>
public sealed record ConflictReportDto(bool Clear, IReadOnlyList<AppointmentConflictDto> Conflicts);

/// <summary><c>POST /staff/appointments/recurring</c> — every visit in the course, in order.</summary>
public sealed record RecurringSeriesDto(string SeriesId, IReadOnlyList<AppointmentDto> Appointments);
